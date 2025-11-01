using System.Diagnostics;
using Lanflix.Infrastructure.Migration.Models;
using Lanflix.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Migration;

/// <summary>
/// Orchestrates the migration process from legacy backend to new backend
/// </summary>
public class MigrationOrchestrator
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<MigrationOrchestrator> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public MigrationOrchestrator(
        ApplicationDbContext dbContext,
        ILogger<MigrationOrchestrator> logger,
        ILoggerFactory loggerFactory)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <summary>
    /// Executes the migration process
    /// </summary>
    public async Task<MigrationResult> ExecuteMigrationAsync(
        MigrationOptions options,
        IProgress<MigrationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new MigrationResult
        {
            StartedAt = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting migration process");

            // Phase 1: Initialize
            ReportProgress(progress, MigrationPhase.Initializing, "Initializing migration", 0, 0);
            
            if (options.CreateBackup && !options.DryRun)
            {
                await CreateDatabaseBackupAsync(cancellationToken);
            }

            // Phase 2: Validate legacy database
            ReportProgress(progress, MigrationPhase.ValidatingLegacyDatabase, "Validating legacy database", 0, 0);
            
            var reader = new LegacyDatabaseReader(options.LegacyDatabasePath, 
                _loggerFactory.CreateLogger<LegacyDatabaseReader>());
            
            if (!reader.ValidateDatabaseAccessibility())
            {
                throw new InvalidOperationException("Legacy database validation failed");
            }

            // Phase 3: Read legacy data
            ReportProgress(progress, MigrationPhase.ReadingLegacyData, "Reading data from legacy database", 0, 0);
            
            var legacyData = await reader.ReadAllDataAsync(cancellationToken);
            
            result.Statistics.ContentRecordsRead = legacyData.Contents.Count;
            result.Statistics.ProfileRecordsRead = legacyData.Profiles.Count;
            result.Statistics.WatchHistoryRecordsRead = legacyData.WatchHistories.Count;
            result.Statistics.EpisodeRecordsRead = legacyData.Episodes.Count;
            result.Statistics.SettingsRecordsRead = legacyData.Settings.Count;

            _logger.LogInformation("Read {Total} total records from legacy database", 
                result.Statistics.TotalRecordsRead);

            // Phase 4: Transform data
            ReportProgress(progress, MigrationPhase.TransformingData, "Transforming data to new schema", 0, 
                result.Statistics.TotalRecordsRead);
            
            var transformer = new DataTransformer(_loggerFactory.CreateLogger<DataTransformer>());
            var transformedData = await TransformDataAsync(legacyData, transformer, progress, result, cancellationToken);

            // Phase 5: Validate transformed data
            ReportProgress(progress, MigrationPhase.ValidatingTransformedData, "Validating transformed data", 0, 
                transformedData.Contents.Count + transformedData.Profiles.Count);
            
            ValidateTransformedData(transformedData, transformer, result);

            if (!options.ContinueOnError && result.Errors.Any())
            {
                throw new InvalidOperationException($"Data validation failed with {result.Errors.Count} errors");
            }

            // Phase 6: Write to new database (skip if dry run)
            if (!options.DryRun)
            {
                ReportProgress(progress, MigrationPhase.WritingToNewDatabase, "Writing data to new database", 0, 
                    result.Statistics.TotalRecordsRead);
                
                await WriteToNewDatabaseAsync(transformedData, options, progress, result, cancellationToken);

                // Phase 7: Verify data integrity
                ReportProgress(progress, MigrationPhase.VerifyingDataIntegrity, "Verifying data integrity", 0, 0);
                
                await VerifyDataIntegrityAsync(result, cancellationToken);
            }
            else
            {
                _logger.LogInformation("Dry run mode - skipping database write");
                result.Warnings.Add("Dry run mode - no data was written to the database");
            }

            // Phase 8: Generate report
            ReportProgress(progress, MigrationPhase.GeneratingReport, "Generating migration report", 0, 0);
            
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            result.CompletedAt = DateTime.UtcNow;
            result.Success = true;

            ReportProgress(progress, MigrationPhase.Completed, "Migration completed successfully", 
                result.Statistics.TotalRecordsMigrated, result.Statistics.TotalRecordsRead);

            _logger.LogInformation("Migration completed successfully in {Duration}", result.Duration);
            LogMigrationSummary(result);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            result.CompletedAt = DateTime.UtcNow;
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.Errors.Add($"Fatal error: {ex.Message}");

            _logger.LogError(ex, "Migration failed after {Duration}", result.Duration);
            
            ReportProgress(progress, MigrationPhase.Failed, $"Migration failed: {ex.Message}", 0, 0);

            return result;
        }
    }

    private async Task<TransformedData> TransformDataAsync(
        LegacyData legacyData,
        DataTransformer transformer,
        IProgress<MigrationProgress>? progress,
        MigrationResult result,
        CancellationToken cancellationToken)
    {
        var transformedData = new TransformedData();
        int processedItems = 0;
        int totalItems = result.Statistics.TotalRecordsRead;

        // Transform Content
        foreach (var legacyContent in legacyData.Contents)
        {
            try
            {
                var content = transformer.TransformContent(legacyContent);
                transformedData.Contents.Add(content);
                result.Statistics.ContentRecordsMigrated++;
            }
            catch (Exception ex)
            {
                result.Statistics.ContentRecordsFailed++;
                result.Errors.Add($"Failed to transform content {legacyContent.Id}: {ex.Message}");
                _logger.LogError(ex, "Failed to transform content {Id}", legacyContent.Id);
            }

            processedItems++;
            ReportProgress(progress, MigrationPhase.TransformingData, "Transforming content records", 
                processedItems, totalItems);
        }

        // Transform Profiles
        foreach (var legacyProfile in legacyData.Profiles)
        {
            try
            {
                var profile = transformer.TransformProfile(legacyProfile);
                transformedData.Profiles.Add(profile);
                result.Statistics.ProfileRecordsMigrated++;
            }
            catch (Exception ex)
            {
                result.Statistics.ProfileRecordsFailed++;
                result.Errors.Add($"Failed to transform profile {legacyProfile.Id}: {ex.Message}");
                _logger.LogError(ex, "Failed to transform profile {Id}", legacyProfile.Id);
            }

            processedItems++;
            ReportProgress(progress, MigrationPhase.TransformingData, "Transforming profile records", 
                processedItems, totalItems);
        }

        // Transform Episodes
        foreach (var legacyEpisode in legacyData.Episodes)
        {
            try
            {
                var episode = transformer.TransformEpisode(legacyEpisode);
                transformedData.Episodes.Add(episode);
                result.Statistics.EpisodeRecordsMigrated++;
            }
            catch (Exception ex)
            {
                result.Statistics.EpisodeRecordsFailed++;
                result.Errors.Add($"Failed to transform episode {legacyEpisode.Id}: {ex.Message}");
                _logger.LogError(ex, "Failed to transform episode {Id}", legacyEpisode.Id);
            }

            processedItems++;
            ReportProgress(progress, MigrationPhase.TransformingData, "Transforming episode records", 
                processedItems, totalItems);
        }

        // Transform WatchHistory
        foreach (var legacyWatchHistory in legacyData.WatchHistories)
        {
            try
            {
                var watchHistory = transformer.TransformWatchHistory(legacyWatchHistory);
                transformedData.WatchHistories.Add(watchHistory);
                result.Statistics.WatchHistoryRecordsMigrated++;
            }
            catch (Exception ex)
            {
                result.Statistics.WatchHistoryRecordsFailed++;
                result.Errors.Add($"Failed to transform watch history {legacyWatchHistory.Id}: {ex.Message}");
                _logger.LogError(ex, "Failed to transform watch history {Id}", legacyWatchHistory.Id);
            }

            processedItems++;
            ReportProgress(progress, MigrationPhase.TransformingData, "Transforming watch history records", 
                processedItems, totalItems);
        }

        // Store settings for later processing
        transformedData.Settings = legacyData.Settings;
        result.Statistics.SettingsRecordsMigrated = legacyData.Settings.Count;

        return transformedData;
    }

    private void ValidateTransformedData(
        TransformedData transformedData,
        DataTransformer transformer,
        MigrationResult result)
    {
        // Validate Content
        foreach (var content in transformedData.Contents)
        {
            if (!transformer.ValidateTransformedContent(content))
            {
                result.Warnings.Add($"Content {content.Id} failed validation");
            }
        }

        // Validate Profiles
        foreach (var profile in transformedData.Profiles)
        {
            if (!transformer.ValidateTransformedProfile(profile))
            {
                result.Warnings.Add($"Profile {profile.Id} failed validation");
            }
        }

        // Validate referential integrity
        var contentIds = transformedData.Contents.Select(c => c.Id).ToHashSet();
        var profileIds = transformedData.Profiles.Select(p => p.Id).ToHashSet();
        var episodeIds = transformedData.Episodes.Select(e => e.Id).ToHashSet();

        foreach (var episode in transformedData.Episodes)
        {
            if (!contentIds.Contains(episode.ContentId))
            {
                result.Warnings.Add($"Episode {episode.Id} references non-existent content {episode.ContentId}");
            }
        }

        foreach (var watchHistory in transformedData.WatchHistories)
        {
            if (!profileIds.Contains(watchHistory.ProfileId))
            {
                result.Warnings.Add($"WatchHistory {watchHistory.Id} references non-existent profile {watchHistory.ProfileId}");
            }
            if (!contentIds.Contains(watchHistory.ContentId))
            {
                result.Warnings.Add($"WatchHistory {watchHistory.Id} references non-existent content {watchHistory.ContentId}");
            }
            if (watchHistory.EpisodeId.HasValue && !episodeIds.Contains(watchHistory.EpisodeId.Value))
            {
                result.Warnings.Add($"WatchHistory {watchHistory.Id} references non-existent episode {watchHistory.EpisodeId}");
            }
        }
    }

    private async Task WriteToNewDatabaseAsync(
        TransformedData transformedData,
        MigrationOptions options,
        IProgress<MigrationProgress>? progress,
        MigrationResult result,
        CancellationToken cancellationToken)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            int processedItems = 0;
            int totalItems = transformedData.Contents.Count + transformedData.Profiles.Count + 
                           transformedData.Episodes.Count + transformedData.WatchHistories.Count;

            // Write Profiles first (referenced by WatchHistory)
            processedItems = await WriteBatchAsync(transformedData.Profiles, _dbContext.Profiles, options.BatchSize, 
                "profiles", processedItems, totalItems, progress, cancellationToken);

            // Write Content (referenced by Episodes and WatchHistory)
            processedItems = await WriteBatchAsync(transformedData.Contents, _dbContext.Contents, options.BatchSize, 
                "content", processedItems, totalItems, progress, cancellationToken);

            // Write Episodes (referenced by WatchHistory)
            processedItems = await WriteBatchAsync(transformedData.Episodes, _dbContext.Episodes, options.BatchSize, 
                "episodes", processedItems, totalItems, progress, cancellationToken);

            // Write WatchHistory last
            processedItems = await WriteBatchAsync(transformedData.WatchHistories, _dbContext.WatchHistories, options.BatchSize, 
                "watch history", processedItems, totalItems, progress, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            
            _logger.LogInformation("Successfully wrote all data to new database");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error writing to new database, transaction rolled back");
            throw;
        }
    }

    private async Task<int> WriteBatchAsync<T>(
        List<T> items,
        DbSet<T> dbSet,
        int batchSize,
        string entityName,
        int processedItems,
        int totalItems,
        IProgress<MigrationProgress>? progress,
        CancellationToken cancellationToken) where T : class
    {
        for (int i = 0; i < items.Count; i += batchSize)
        {
            var batch = items.Skip(i).Take(batchSize).ToList();
            await dbSet.AddRangeAsync(batch, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            processedItems += batch.Count;
            ReportProgress(progress, MigrationPhase.WritingToNewDatabase, 
                $"Writing {entityName} to database", processedItems, totalItems);

            _logger.LogDebug("Wrote batch of {Count} {Entity} records", batch.Count, entityName);
        }

        return processedItems;
    }

    private async Task VerifyDataIntegrityAsync(MigrationResult result, CancellationToken cancellationToken)
    {
        var contentCount = await _dbContext.Contents.CountAsync(cancellationToken);
        var profileCount = await _dbContext.Profiles.CountAsync(cancellationToken);
        var episodeCount = await _dbContext.Episodes.CountAsync(cancellationToken);
        var watchHistoryCount = await _dbContext.WatchHistories.CountAsync(cancellationToken);

        _logger.LogInformation("Data integrity check - Content: {Content}, Profiles: {Profiles}, Episodes: {Episodes}, WatchHistory: {WatchHistory}",
            contentCount, profileCount, episodeCount, watchHistoryCount);

        if (contentCount != result.Statistics.ContentRecordsMigrated)
        {
            result.Warnings.Add($"Content count mismatch: expected {result.Statistics.ContentRecordsMigrated}, found {contentCount}");
        }
        if (profileCount != result.Statistics.ProfileRecordsMigrated)
        {
            result.Warnings.Add($"Profile count mismatch: expected {result.Statistics.ProfileRecordsMigrated}, found {profileCount}");
        }
        if (episodeCount != result.Statistics.EpisodeRecordsMigrated)
        {
            result.Warnings.Add($"Episode count mismatch: expected {result.Statistics.EpisodeRecordsMigrated}, found {episodeCount}");
        }
        if (watchHistoryCount != result.Statistics.WatchHistoryRecordsMigrated)
        {
            result.Warnings.Add($"WatchHistory count mismatch: expected {result.Statistics.WatchHistoryRecordsMigrated}, found {watchHistoryCount}");
        }
    }

    private async Task CreateDatabaseBackupAsync(CancellationToken cancellationToken)
    {
        try
        {
            var connectionString = _dbContext.Database.GetConnectionString();
            if (connectionString?.Contains("Data Source=") == true)
            {
                var dbPath = connectionString.Split("Data Source=")[1].Split(';')[0];
                if (File.Exists(dbPath))
                {
                    var backupPath = $"{dbPath}.backup.{DateTime.UtcNow:yyyyMMddHHmmss}";
                    File.Copy(dbPath, backupPath);
                    _logger.LogInformation("Created database backup at {Path}", backupPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create database backup");
        }
    }

    private void ReportProgress(
        IProgress<MigrationProgress>? progress,
        MigrationPhase phase,
        string currentStep,
        int processedItems,
        int totalItems)
    {
        if (progress == null)
            return;

        var percentage = totalItems > 0 ? (double)processedItems / totalItems * 100 : 0;

        progress.Report(new MigrationProgress
        {
            Phase = phase,
            CurrentStep = currentStep,
            ProcessedItems = processedItems,
            TotalItems = totalItems,
            PercentageComplete = percentage
        });
    }

    private void LogMigrationSummary(MigrationResult result)
    {
        _logger.LogInformation("=== Migration Summary ===");
        _logger.LogInformation("Duration: {Duration}", result.Duration);
        _logger.LogInformation("Content: {Migrated}/{Read} migrated, {Failed} failed",
            result.Statistics.ContentRecordsMigrated, result.Statistics.ContentRecordsRead, result.Statistics.ContentRecordsFailed);
        _logger.LogInformation("Profiles: {Migrated}/{Read} migrated, {Failed} failed",
            result.Statistics.ProfileRecordsMigrated, result.Statistics.ProfileRecordsRead, result.Statistics.ProfileRecordsFailed);
        _logger.LogInformation("Episodes: {Migrated}/{Read} migrated, {Failed} failed",
            result.Statistics.EpisodeRecordsMigrated, result.Statistics.EpisodeRecordsRead, result.Statistics.EpisodeRecordsFailed);
        _logger.LogInformation("WatchHistory: {Migrated}/{Read} migrated, {Failed} failed",
            result.Statistics.WatchHistoryRecordsMigrated, result.Statistics.WatchHistoryRecordsRead, result.Statistics.WatchHistoryRecordsFailed);
        _logger.LogInformation("Settings: {Migrated}/{Read} migrated",
            result.Statistics.SettingsRecordsMigrated, result.Statistics.SettingsRecordsRead);
        _logger.LogInformation("Total: {Migrated}/{Read} migrated, {Failed} failed",
            result.Statistics.TotalRecordsMigrated, result.Statistics.TotalRecordsRead, result.Statistics.TotalRecordsFailed);
        _logger.LogInformation("Warnings: {Count}", result.Warnings.Count);
        _logger.LogInformation("Errors: {Count}", result.Errors.Count);
    }
}

/// <summary>
/// Container for transformed data ready to be written to the new database
/// </summary>
internal class TransformedData
{
    public List<Domain.Entities.Content> Contents { get; set; } = new();
    public List<Domain.Entities.Profile> Profiles { get; set; } = new();
    public List<Domain.Entities.Episode> Episodes { get; set; } = new();
    public List<Domain.Entities.WatchHistory> WatchHistories { get; set; } = new();
    public List<LegacySettings> Settings { get; set; } = new();
}
