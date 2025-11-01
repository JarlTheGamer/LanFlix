using Dapper;
using Lanflix.Infrastructure.Migration.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Migration;

/// <summary>
/// Reads data from the legacy Node.js backend SQLite database using Dapper
/// </summary>
public class LegacyDatabaseReader
{
    private readonly string _legacyDbPath;
    private readonly ILogger<LegacyDatabaseReader> _logger;

    public LegacyDatabaseReader(string legacyDbPath, ILogger<LegacyDatabaseReader> logger)
    {
        _legacyDbPath = legacyDbPath ?? throw new ArgumentNullException(nameof(legacyDbPath));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates that the legacy database exists and is accessible
    /// </summary>
    public bool ValidateDatabaseAccessibility()
    {
        try
        {
            if (!File.Exists(_legacyDbPath))
            {
                _logger.LogError("Legacy database file not found at: {Path}", _legacyDbPath);
                return false;
            }

            using var connection = new SqliteConnection($"Data Source={_legacyDbPath};Mode=ReadOnly");
            connection.Open();
            
            // Verify required tables exist
            var tables = connection.Query<string>(
                "SELECT name FROM sqlite_master WHERE type='table' AND name IN ('content', 'profiles', 'watch_history', 'series_episodes', 'settings')"
            ).ToList();

            var requiredTables = new[] { "content", "profiles", "watch_history", "series_episodes", "settings" };
            var missingTables = requiredTables.Except(tables).ToList();

            if (missingTables.Any())
            {
                _logger.LogError("Missing required tables in legacy database: {Tables}", string.Join(", ", missingTables));
                return false;
            }

            _logger.LogInformation("Legacy database validation successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating legacy database accessibility");
            return false;
        }
    }

    /// <summary>
    /// Reads all data from the legacy database
    /// </summary>
    public async Task<LegacyData> ReadAllDataAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting to read data from legacy database: {Path}", _legacyDbPath);

        var legacyData = new LegacyData();

        using var connection = new SqliteConnection($"Data Source={_legacyDbPath};Mode=ReadOnly");
        await connection.OpenAsync(cancellationToken);

        // Read Content table
        legacyData.Contents = await ReadContentAsync(connection, cancellationToken);
        _logger.LogInformation("Read {Count} content records", legacyData.Contents.Count);

        // Read Profile table
        legacyData.Profiles = await ReadProfilesAsync(connection, cancellationToken);
        _logger.LogInformation("Read {Count} profile records", legacyData.Profiles.Count);

        // Read WatchHistory table
        legacyData.WatchHistories = await ReadWatchHistoryAsync(connection, cancellationToken);
        _logger.LogInformation("Read {Count} watch history records", legacyData.WatchHistories.Count);

        // Read SeriesEpisode table
        legacyData.Episodes = await ReadSeriesEpisodesAsync(connection, cancellationToken);
        _logger.LogInformation("Read {Count} series episode records", legacyData.Episodes.Count);

        // Read Settings table
        legacyData.Settings = await ReadSettingsAsync(connection, cancellationToken);
        _logger.LogInformation("Read {Count} settings records", legacyData.Settings.Count);

        _logger.LogInformation("Completed reading all data from legacy database");
        return legacyData;
    }

    private async Task<List<LegacyContent>> ReadContentAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT 
                id AS Id,
                tmdb_id AS TmdbId,
                type AS Type,
                title AS Title,
                original_title AS OriginalTitle,
                overview AS Overview,
                release_date AS ReleaseDate,
                poster_path AS PosterPath,
                backdrop_path AS BackdropPath,
                vote_average AS VoteAverage,
                vote_count AS VoteCount,
                genres AS Genres,
                runtime AS Runtime,
                status AS Status,
                file_path AS FilePath,
                added_at AS AddedAt,
                updated_at AS UpdatedAt
            FROM content
            ORDER BY id";

        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        var results = await connection.QueryAsync<LegacyContent>(command);
        return results.ToList();
    }

    private async Task<List<LegacyProfile>> ReadProfilesAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT 
                id AS Id,
                name AS Name,
                avatar_color_primary AS AvatarColorPrimary,
                avatar_color_secondary AS AvatarColorSecondary,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM profiles
            ORDER BY id";

        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        var results = await connection.QueryAsync<LegacyProfile>(command);
        return results.ToList();
    }

    private async Task<List<LegacyWatchHistory>> ReadWatchHistoryAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT 
                id AS Id,
                profile_id AS ProfileId,
                content_id AS ContentId,
                episode_id AS EpisodeId,
                progress_seconds AS ProgressSeconds,
                duration_seconds AS DurationSeconds,
                completed AS Completed,
                last_watched_at AS LastWatchedAt
            FROM watch_history
            ORDER BY id";

        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        var results = await connection.QueryAsync<LegacyWatchHistory>(command);
        return results.ToList();
    }

    private async Task<List<LegacySeriesEpisode>> ReadSeriesEpisodesAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT 
                id AS Id,
                content_id AS ContentId,
                season_number AS SeasonNumber,
                episode_number AS EpisodeNumber,
                title AS Title,
                overview AS Overview,
                air_date AS AirDate,
                still_path AS StillPath,
                file_path AS FilePath
            FROM series_episodes
            ORDER BY content_id, season_number, episode_number";

        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        var results = await connection.QueryAsync<LegacySeriesEpisode>(command);
        return results.ToList();
    }

    private async Task<List<LegacySettings>> ReadSettingsAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT 
                key AS Key,
                value AS Value,
                updated_at AS UpdatedAt
            FROM settings
            ORDER BY key";

        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        var results = await connection.QueryAsync<LegacySettings>(command);
        return results.ToList();
    }
}
