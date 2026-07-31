using System.IO;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Common.Models;
using Lanflix.Domain.Entities;
using Lanflix.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.Infrastructure.Services.Library;

/// <summary>
/// Library service for scanning media folders and managing library content
/// Based on the old backend library.service.ts
/// </summary>
public class LibraryService : ILibraryService
{
    private readonly IApplicationDbContext _context;
    private readonly ISettingsService _settingsService;
    private readonly IMetadataService _metadataService;
    private readonly ITmdbClient _tmdbClient;
    private readonly IMediaAnalyzer _mediaAnalyzer;
    private readonly IIntroScanner _introScanner;
    private readonly ILogger<LibraryService> _logger;

    private readonly string[] _videoExtensions = { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v" };
    private readonly string[] _moviesFolderNames = { "movies", "Movies", "MOVIES" };
    private readonly string[] _seriesFolderNames = { "series", "Series", "SERIES", "shows", "Shows", "SHOWS" };

    public LibraryService(
        IApplicationDbContext context,
        ISettingsService settingsService,
        IMetadataService metadataService,
        ITmdbClient tmdbClient,
        IMediaAnalyzer mediaAnalyzer,
        IIntroScanner introScanner,
        ILogger<LibraryService> logger)
    {
        _context = context;
        _settingsService = settingsService;
        _metadataService = metadataService;
        _tmdbClient = tmdbClient;
        _mediaAnalyzer = mediaAnalyzer;
        _introScanner = introScanner;
        _logger = logger;
    }

    public async Task<LibraryScanResult> ScanLibraryAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting library scan");

            var settings = await _settingsService.GetSettingsAsync(cancellationToken);
            var stats = new LibraryScanResult();

            // Scan movies folder if configured
            if (!string.IsNullOrEmpty(settings.MediaPaths.Movies) && Directory.Exists(settings.MediaPaths.Movies))
            {
                await ScanMoviesFolderAsync(settings.MediaPaths.Movies, stats, cancellationToken);
            }
            else
            {
                _logger.LogWarning("Movies path not configured or doesn't exist: {Path}", settings.MediaPaths.Movies);
            }

            // Scan series folder if configured
            if (!string.IsNullOrEmpty(settings.MediaPaths.Series) && Directory.Exists(settings.MediaPaths.Series))
            {
                await ScanSeriesFolderAsync(settings.MediaPaths.Series, stats, cancellationToken);
            }
            else
            {
                _logger.LogWarning("Series path not configured or doesn't exist: {Path}", settings.MediaPaths.Series);
            }

            // Clean up missing content
            await CleanupMissingContentAsync(stats, cancellationToken);

            // Trigger background audio fingerprint intro scanner for seasons missing intro markers
            var introScanner = _introScanner;
            var logger = _logger;
            var unscannedSeasons = await _context.Episodes
                .Where(e => e.IntroStartTime == null && !string.IsNullOrEmpty(e.FilePath))
                .Select(e => new { e.ContentId, e.SeasonNumber })
                .Distinct()
                .ToListAsync(cancellationToken);

            if (unscannedSeasons.Count > 0)
            {
                _logger.LogInformation("Queueing background audio fingerprint intro scan for {Count} unscanned seasons...", unscannedSeasons.Count);
                _ = Task.Run(async () =>
                {
                    foreach (var season in unscannedSeasons)
                    {
                        try
                        {
                            await introScanner.ScanSeasonIntrosAsync(season.ContentId, season.SeasonNumber);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Background intro scan failed for Series {ContentId} Season {SeasonNumber}", season.ContentId, season.SeasonNumber);
                        }
                    }
                });
            }

            _logger.LogInformation("Library scan completed: {Added} added, {Updated} updated, {Removed} removed, {Errors} errors", 
                stats.Added, stats.Updated, stats.Removed, stats.Errors.Count);

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan library");
            throw;
        }
    }

    public async Task ScanMoviesFolderAsync(string moviesPath, LibraryScanResult stats, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Scanning movies folder: {Path}", moviesPath);

            var directories = Directory.GetDirectories(moviesPath);

            foreach (var movieFolder in directories)
            {
                try
                {
                    await ScanMovieFolderAsync(movieFolder, stats, cancellationToken);
                }
                catch (Exception ex)
                {
                    var error = $"Failed to scan movie folder {movieFolder}: {ex.Message}";
                    _logger.LogError(ex, error);
                    stats.Errors.Add(error);
                }
            }
        }
        catch (Exception ex)
        {
            var error = $"Failed to scan movies folder {moviesPath}: {ex.Message}";
            _logger.LogError(ex, error);
            stats.Errors.Add(error);
        }
    }

    public async Task ScanSeriesFolderAsync(string seriesPath, LibraryScanResult stats, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Scanning series folder: {Path}", seriesPath);

            var directories = Directory.GetDirectories(seriesPath);

            foreach (var seriesFolder in directories)
            {
                try
                {
                    await ScanSingleSeriesFolderAsync(seriesFolder, stats, cancellationToken);
                }
                catch (Exception ex)
                {
                    var error = $"Failed to scan series folder {seriesFolder}: {ex.Message}";
                    _logger.LogError(ex, error);
                    stats.Errors.Add(error);
                }
            }
        }
        catch (Exception ex)
        {
            var error = $"Failed to scan series folder {seriesPath}: {ex.Message}";
            _logger.LogError(ex, error);
            stats.Errors.Add(error);
        }
    }

    public async Task CleanupMissingContentAsync(LibraryScanResult stats, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Cleaning up missing content");

            // Get all content from database
            var allContent = await _context.Contents.ToListAsync(cancellationToken);
            var contentIdsToRemove = new List<int>();

            foreach (var content in allContent)
            {
                var shouldRemove = false;
                var reason = "";

                // Remove content with no file path
                if (string.IsNullOrEmpty(content.FilePath))
                {
                    shouldRemove = true;
                    reason = "no file path";
                }
                else
                {
                    // Check if file/folder exists
                    var exists = content.Type == ContentType.Movie ? File.Exists(content.FilePath) : Directory.Exists(content.FilePath);
                    if (!exists)
                    {
                        shouldRemove = true;
                        reason = $"{(content.Type == ContentType.Movie ? "file" : "folder")} no longer exists: {content.FilePath}";
                    }
                }

                if (shouldRemove)
                {
                    _logger.LogInformation("Removing content {Id} ({Title}) - {Reason}", content.Id, content.Title, reason);
                    contentIdsToRemove.Add(content.Id);
                    stats.Removed++;
                }
            }

            // Remove identified content using ExecuteDeleteAsync to bypass change tracker issues with owned JSON collections
            if (contentIdsToRemove.Any())
            {
                // Remove related watch histories
                await _context.WatchHistories
                    .Where(w => contentIdsToRemove.Contains(w.ContentId))
                    .ExecuteDeleteAsync(cancellationToken);
                
                // Remove related episodes
                await _context.Episodes
                     .Where(e => contentIdsToRemove.Contains(e.ContentId))
                     .ExecuteDeleteAsync(cancellationToken);
                
                // Finally remove the content
                await _context.Contents
                    .Where(c => contentIdsToRemove.Contains(c.Id))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            // Also cleanup missing episodes (iterate and find IDs, then delete)
            var episodes = await _context.Episodes.ToListAsync(cancellationToken);
            var episodeIdsToRemove = new List<int>();
            
            foreach (var episode in episodes)
            {
                if (!string.IsNullOrEmpty(episode.FilePath) && !File.Exists(episode.FilePath))
                {
                    _logger.LogInformation("Removing missing episode: S{Season}E{Episode} at {Path}", 
                        episode.SeasonNumber, episode.EpisodeNumber, episode.FilePath);
                    episodeIdsToRemove.Add(episode.Id);
                    stats.Removed++;
                }
            }
            
            if (episodeIdsToRemove.Any())
            {
                await _context.Episodes
                    .Where(e => episodeIdsToRemove.Contains(e.Id))
                    .ExecuteDeleteAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            var error = $"Failed to cleanup missing content: {ex.Message}";
            _logger.LogError(ex, error);
            stats.Errors.Add(error);
        }
    }

    private async Task ScanMovieFolderAsync(string movieFolder, LibraryScanResult stats, CancellationToken cancellationToken)
    {
        var folderName = Path.GetFileName(movieFolder);
        
        // Find video file
        var files = Directory.GetFiles(movieFolder);
        var videoFile = files.FirstOrDefault(f => 
            _videoExtensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) &&
            !f.Contains(".converting.")); // Skip incomplete conversion files

        if (videoFile == null)
        {
            _logger.LogDebug("No video file found in movie folder: {Folder}", movieFolder);
            return;
        }

        // Check if movie already exists in database
        var existing = await _context.Contents
            .FirstOrDefaultAsync(c => c.FilePath == videoFile, cancellationToken);

        if (existing != null)
        {
            // If existing content lacks MediaInfo, analyze and update it
            if (existing.MediaInfo == null)
            {
                _logger.LogWarning("Found existing movie without MediaInfo, analyzing: {Title} at {FilePath}", existing.Title, existing.FilePath);
                
                try
                {
                    // Analyze media file to get technical information
                    var analyzedMediaInfo = await _mediaAnalyzer.AnalyzeAsync(existing.FilePath, cancellationToken);
                    existing.MediaInfo = analyzedMediaInfo;
                    
                    await _context.SaveChangesAsync(cancellationToken);
                    stats.Updated++;
                    
                    _logger.LogInformation("Successfully updated movie MediaInfo: {Title}", existing.Title);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to analyze media file for existing movie: {Title} at {FilePath}", existing.Title, existing.FilePath);
                    stats.Errors.Add($"Failed to analyze: {existing.Title} - {ex.Message}");
                }
            }
            else
            {
                _logger.LogDebug("Movie already exists with MediaInfo: {Title}", existing.Title);
            }
            return;
        }

        // Try to load metadata from folder
        var metadata = await _metadataService.LoadMetadataFromMediaFolderAsync(movieFolder, cancellationToken);
        
        if (metadata != null)
        {
            // Metadata exists, parse the TMDB ID from the JSON
            var metadataJson = (System.Text.Json.JsonElement)metadata;
            if (metadataJson.TryGetProperty("tmdbId", out var tmdbIdElement) && tmdbIdElement.TryGetInt32(out var tmdbId))
            {
                // Check if we need to add/update content in database
                var existingByTmdb = await _context.Contents
                    .FirstOrDefaultAsync(c => c.TmdbId == tmdbId && c.Type == ContentType.Movie, cancellationToken);

                if (existingByTmdb != null)
                {
                    // Content exists, check if MediaInfo needs to be added
                    if (existingByTmdb.MediaInfo == null)
                    {
                        _logger.LogWarning("Found existing movie with metadata but no MediaInfo, analyzing: {Title} at {FilePath}", existingByTmdb.Title, existingByTmdb.FilePath);
                        
                        // Check if file path is relative and make it absolute
                        var fullFilePath = existingByTmdb.FilePath;
                        if (!Path.IsPathRooted(fullFilePath))
                        {
                            _logger.LogWarning("File path is relative, converting to absolute: {RelativePath}", fullFilePath);
                            // Try to construct full path using the movie folder
                            fullFilePath = Path.Combine(movieFolder, Path.GetFileName(fullFilePath));
                            _logger.LogInformation("Converted to absolute path: {AbsolutePath}", fullFilePath);
                        }
                        
                        // Verify file exists
                        if (!System.IO.File.Exists(fullFilePath))
                        {
                            _logger.LogError("Media file does not exist at path: {FilePath}", fullFilePath);
                            stats.Errors.Add($"File not found: {existingByTmdb.Title}");
                            return;
                        }
                        
                        try
                        {
                            var analyzedMediaInfo = await _mediaAnalyzer.AnalyzeAsync(fullFilePath, cancellationToken);
                            existingByTmdb.MediaInfo = analyzedMediaInfo;
                            
                            // Update the file path if it was corrected
                            if (fullFilePath != existingByTmdb.FilePath)
                            {
                                _logger.LogInformation("Updating file path in database: {OldPath} -> {NewPath}", existingByTmdb.FilePath, fullFilePath);
                                existingByTmdb.FilePath = fullFilePath;
                            }
                            
                            await _context.SaveChangesAsync(cancellationToken);
                            stats.Updated++;
                            
                            _logger.LogInformation("Successfully updated movie MediaInfo from metadata: {Title}", existingByTmdb.Title);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to analyze media file for existing movie with metadata: {Title} at {FilePath}", existingByTmdb.Title, existingByTmdb.FilePath);
                            stats.Errors.Add($"Failed to analyze: {existingByTmdb.Title} - {ex.Message}");
                        }
                    }
                    else
                    {
                        _logger.LogDebug("Movie with metadata already exists with MediaInfo: {Title}", existingByTmdb.Title);
                    }
                }
                else
                {
                    // Content doesn't exist, add it using the metadata
                    _logger.LogInformation("Found metadata for new movie, adding to library: TMDB ID {TmdbId}", tmdbId);
                    await AddMovieToLibraryAsync(tmdbId, videoFile, movieFolder, stats, cancellationToken);
                }
            }
            else
            {
                _logger.LogWarning("Metadata file exists but doesn't contain valid tmdbId: {Folder}", movieFolder);
                stats.Errors.Add($"Invalid metadata: {folderName}");
            }
        }
        else
        {
            // If no metadata, try to search and fetch from TMDB
            try
            {
                // Parse movie title and year from folder name (like the old backend)
                var match = System.Text.RegularExpressions.Regex.Match(folderName, @"^(.+?)\s*\((\d{4})\)");
                if (match.Success)
                {
                    var title = match.Groups[1].Value.Trim();
                    var year = int.Parse(match.Groups[2].Value);
                    
                    _logger.LogInformation("Parsed movie: {Title} ({Year}) from folder: {FolderName}", title, year, folderName);
                    
                    // Search for movie with the parsed title
                    var searchResults = await _tmdbClient.SearchMoviesAsync(title, cancellationToken);
                    var movieMatch = searchResults.Results.FirstOrDefault(m => 
                        m.ReleaseDate?.Year == year);
                    
                    if (movieMatch != null)
                    {
                        _logger.LogInformation("Found TMDB match for {Title} ({Year}): {TmdbId}", title, year, movieMatch.Id);
                        
                        // Add to library using the addToLibrary pattern from old backend
                        await AddMovieToLibraryAsync(movieMatch.Id, videoFile, movieFolder, stats, cancellationToken);
                    }
                    else
                    {
                        _logger.LogWarning("No TMDB match found for {Title} ({Year})", title, year);
                        stats.Errors.Add($"No TMDB match: {folderName}");
                    }
                }
                else
                {
                    _logger.LogWarning("Could not parse movie title and year from folder name: {FolderName}", folderName);
                    stats.Errors.Add($"Invalid folder name format: {folderName}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch metadata for movie {FolderName}", folderName);
                stats.Errors.Add($"Failed to fetch metadata: {folderName}");
            }
        } // End of else block for when metadata == null
    }



    private async Task ScanSingleSeriesFolderAsync(string seriesFolder, LibraryScanResult stats, CancellationToken cancellationToken)
    {
        var folderName = Path.GetFileName(seriesFolder);
        
        // Check if series already exists in database by file path
        var existingByPath = await _context.Contents
            .FirstOrDefaultAsync(c => c.FilePath == seriesFolder && c.Type == ContentType.Series, cancellationToken);

        if (existingByPath != null)
        {
            _logger.LogInformation("Series already exists in database by path: {Title} (ID: {Id}, TMDB: {TmdbId})", existingByPath.Title, existingByPath.Id, existingByPath.TmdbId);
            
            // Fetch and store episode metadata using MetadataService (like old backend)
            await _metadataService.FetchAndStoreEpisodeMetadataAsync(existingByPath.Id, existingByPath.TmdbId, seriesFolder, cancellationToken);
            
            // Scan for new episodes
            await ScanSeriesEpisodesAsync(existingByPath.Id, seriesFolder, cancellationToken);
            return;
        }

        // Try to load metadata from folder
        var metadata = await _metadataService.LoadMetadataFromMediaFolderAsync(seriesFolder, cancellationToken);
        
    // If metadata found, use it
    if (metadata != null)
    {
        var metadataJson = (System.Text.Json.JsonElement)metadata;
        if (metadataJson.TryGetProperty("tmdbId", out var tmdbIdElement) && tmdbIdElement.TryGetInt32(out var tmdbId))
        {
             // Check if already exists in database by TMDB ID
            var existingByTmdb = await _context.Contents
                .FirstOrDefaultAsync(c => c.TmdbId == tmdbId && c.Type == ContentType.Series, cancellationToken);
                
            if (existingByTmdb != null)
            {
                _logger.LogInformation("Series already exists (by TMDB): {Title}", existingByTmdb.Title);
                
                 // Update file path if different
                if (existingByTmdb.FilePath != seriesFolder)
                {
                    _logger.LogInformation("Updating existing series file path from {OldPath} to {NewPath}", existingByTmdb.FilePath, seriesFolder);
                    existingByTmdb.FilePath = seriesFolder;
                    existingByTmdb.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);
                    stats.Updated++;
                }

                // Scan for new episodes
                await ScanSeriesEpisodesAsync(existingByTmdb.Id, seriesFolder, cancellationToken);
            }
            else
            {
                 // Add to library
                _logger.LogInformation("Found metadata for new series, adding to library: TMDB ID {TmdbId}", tmdbId);
                await AddSeriesToLibraryAsync(tmdbId, seriesFolder, stats, cancellationToken);
            }
        }
    }
    else
    {
        // If no metadata, try to search TMDB by folder name
        try
        {
            _logger.LogInformation("No metadata file found for {FolderName}, attempting to fetch from TMDB", folderName);
            
            // Search for series using folder name
            var searchResults = await _tmdbClient.SearchTvSeriesAsync(folderName, cancellationToken);
            var seriesMatch = searchResults.Results.FirstOrDefault();
            
            if (seriesMatch != null)
            {
                _logger.LogInformation("Found TMDB match for series {FolderName}: {TmdbId}", folderName, seriesMatch.Id);
                
                // Add to library
                await AddSeriesToLibraryAsync(seriesMatch.Id, seriesFolder, stats, cancellationToken);
            }
            else
            {
                _logger.LogWarning("No TMDB match found for series: {FolderName}", folderName);
                stats.Errors.Add($"No TMDB match: {folderName}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch metadata for series {FolderName}", folderName);
            stats.Errors.Add($"Failed to fetch metadata: {folderName}");
        }
    }
    }



    /// <summary>
    /// Add movie to library (following old backend pattern)
    /// </summary>
    private async Task AddMovieToLibraryAsync(int tmdbId, string filePath, string movieFolder, LibraryScanResult stats, CancellationToken cancellationToken)
    {
        try
        {
            // Check if already exists
            var existing = await _context.Contents
                .FirstOrDefaultAsync(c => c.TmdbId == tmdbId && c.Type == ContentType.Movie, cancellationToken);

            if (existing != null)
            {
                // If existing content lacks MediaInfo, analyze and update it
                if (existing.MediaInfo == null)
                {
                    _logger.LogWarning("Found existing movie without MediaInfo (via TMDB), analyzing: {Title} at {FilePath}", existing.Title, existing.FilePath);
                    
                    // Analyze media file to get technical information
                    var analyzedMediaInfo = await _mediaAnalyzer.AnalyzeAsync(existing.FilePath, cancellationToken);
                    existing.MediaInfo = analyzedMediaInfo;
                    
                    await _context.SaveChangesAsync(cancellationToken);
                    stats.Updated++;
                    
                    _logger.LogInformation("Successfully updated movie MediaInfo (via TMDB): {Title}", existing.Title);
                }
                else
                {
                    _logger.LogDebug("Movie already exists with MediaInfo (via TMDB): {Title}", existing.Title);
                }
                return;
            }

            // Fetch metadata from TMDB
            var movieDetails = await _tmdbClient.GetMovieDetailsAsync(tmdbId, cancellationToken);

            // Analyze media file to get technical information
            _logger.LogInformation("Analyzing media file: {FilePath}", filePath);
            var mediaInfo = await _mediaAnalyzer.AnalyzeAsync(filePath, cancellationToken);

            // Create content entry
            var content = new Content
            {
                TmdbId = tmdbId,
                Type = ContentType.Movie,
                Title = movieDetails.Title,
                Overview = movieDetails.Overview,
                ReleaseDate = movieDetails.ReleaseDate,
                PosterPath = movieDetails.PosterPath,
                BackdropPath = movieDetails.BackdropPath,
                Rating = movieDetails.VoteAverage,
                Genres = movieDetails.Genres?.Select(g => g.Name).ToArray(),
                FilePath = filePath,
                MediaInfo = mediaInfo, // Add the analyzed media information
                AddedAt = DateTime.UtcNow
            };

            _context.Contents.Add(content);
            await _context.SaveChangesAsync(cancellationToken);

            // Save metadata to media folder (like old backend)
            await _metadataService.SaveMetadataToMediaFolderAsync(content.Id, movieFolder, cancellationToken);

            stats.Added++;
            _logger.LogInformation("Added movie to library: {Title} ({Year})", content.Title, content.ReleaseDate?.Year);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add movie to library: {TmdbId}", tmdbId);
            throw;
        }
    }

    /// <summary>
    /// Add series to library (following old backend pattern)
    /// </summary>
    private async Task AddSeriesToLibraryAsync(int tmdbId, string seriesFolder, LibraryScanResult stats, CancellationToken cancellationToken)
    {
        try
        {
            // Use the same approach as the old backend - simple check and upsert pattern
            _logger.LogInformation("TMDB TV series search completed: {SeriesName}, Results: 1", Path.GetFileName(seriesFolder));
            _logger.LogInformation("Found TMDB match for series {SeriesName}: {TmdbId}", Path.GetFileName(seriesFolder), tmdbId);
            
            // Check if already exists by TMDB ID and type (like old backend)
            var existing = await _context.Contents
                .FirstOrDefaultAsync(c => c.TmdbId == tmdbId && c.Type == ContentType.Series, cancellationToken);

            if (existing != null)
            {
                _logger.LogInformation("Series already exists in library: {Title} (ID: {Id})", existing.Title, existing.Id);
                
                // Update file path if different (like old backend)
                if (existing.FilePath != seriesFolder)
                {
                    _logger.LogInformation("Updating existing series file path from {OldPath} to {NewPath}", existing.FilePath, seriesFolder);
                    existing.FilePath = seriesFolder;
                    existing.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);
                    stats.Updated++;
                }
                
                // Always refresh episode metadata during scan (like old backend)
                await _metadataService.FetchAndStoreEpisodeMetadataAsync(existing.Id, tmdbId, seriesFolder, cancellationToken);
                await ScanSeriesEpisodesAsync(existing.Id, seriesFolder, cancellationToken);
                return;
            }

            _logger.LogInformation("No existing series found with TMDB ID {TmdbId}. Adding new series to library: Folder: {Folder}", tmdbId, seriesFolder);

            // Fetch metadata from TMDB
            var seriesDetails = await _tmdbClient.GetTvSeriesDetailsAsync(tmdbId, cancellationToken);
            _logger.LogInformation("TMDB TV series details retrieved: {TmdbId}, Name: {Name}", tmdbId, seriesDetails.Name);

            // Create content entry (like old backend)
            var content = new Content
            {
                TmdbId = tmdbId,
                Type = ContentType.Series,
                Title = seriesDetails.Name,
                OriginalTitle = seriesDetails.OriginalName,
                Overview = seriesDetails.Overview,
                ReleaseDate = seriesDetails.FirstAirDate,
                PosterPath = seriesDetails.PosterPath,
                BackdropPath = seriesDetails.BackdropPath,
                Rating = seriesDetails.VoteAverage,
                Genres = seriesDetails.Genres?.Select(g => g.Name).ToArray(),
                FilePath = seriesFolder,
                AddedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Contents.Add(content);
            
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Successfully added series to database: {Title} (TMDB: {TmdbId})", content.Title, tmdbId);

            // Save metadata to media folder (like old backend)
            await _metadataService.SaveMetadataToMediaFolderAsync(content.Id, seriesFolder, cancellationToken);

            stats.Added++;
            _logger.LogInformation("Added series to library: {Title} ({Year})", content.Title, content.ReleaseDate?.Year);

            // Fetch and store episode metadata using MetadataService (like old backend)
            await _metadataService.FetchAndStoreEpisodeMetadataAsync(content.Id, tmdbId, seriesFolder, cancellationToken);
            
            // Scan for episodes in the series folder
            await ScanSeriesEpisodesAsync(content.Id, seriesFolder, cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("UNIQUE constraint failed: Contents.TmdbId") == true)
        {
            _logger.LogError(ex, "UNIQUE constraint violation for TMDB ID {TmdbId}. This indicates the database schema needs to be updated.", tmdbId);
            stats.Errors.Add($"Database schema error for series {tmdbId}: UNIQUE constraint on TmdbId needs to be updated to include Type. Please recreate the database or run the migration helper.");
            
            // Don't throw - continue with other series
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add series to library: {TmdbId}", tmdbId);
            stats.Errors.Add($"Failed to add series {tmdbId}: {ex.Message}");
            
            // Don't throw - continue with other series to avoid stopping the entire scan
        }
    }

    /// <summary>
    /// Scan series folder for episodes and add them to the database
    /// Based on the old backend's episode scanning logic
    /// </summary>
    private async Task ScanSeriesEpisodesAsync(int contentId, string seriesFolder, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Scanning episodes for series ID {ContentId} in folder: {Folder}", contentId, seriesFolder);

            // Get series details from TMDB to know how many seasons/episodes exist
            var content = await _context.Contents.FindAsync(new object[] { contentId }, cancellationToken);
            if (content == null)
            {
                _logger.LogError("Content not found: {ContentId}", contentId);
                return;
            }

            var seriesDetails = await _tmdbClient.GetTvSeriesDetailsAsync(content.TmdbId, cancellationToken);
            if (seriesDetails == null)
            {
                _logger.LogError("Could not fetch series details from TMDB: {TmdbId}", content.TmdbId);
                return;
            }

            // Scan each season folder
            var seasonFolders = Directory.GetDirectories(seriesFolder)
                .Where(d => Path.GetFileName(d).StartsWith("Season ", StringComparison.OrdinalIgnoreCase) ||
                           System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(d), @"^S\d+", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                .ToList();

            foreach (var seasonFolder in seasonFolders)
            {
                await ScanSeasonFolderAsync(contentId, seasonFolder, seriesDetails, cancellationToken);
            }

            // Also check for episodes directly in the series folder (flat structure)
            var episodeFiles = Directory.GetFiles(seriesFolder)
                .Where(f => _videoExtensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (episodeFiles.Any())
            {
                await ScanFlatSeriesStructureAsync(contentId, seriesFolder, episodeFiles, seriesDetails, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan episodes for series {ContentId}", contentId);
        }
    }

    /// <summary>
    /// Scan a season folder for episodes
    /// </summary>
    private async Task ScanSeasonFolderAsync(int contentId, string seasonFolder, TmdbTvSeriesDetails seriesDetails, CancellationToken cancellationToken)
    {
        try
        {
            var seasonFolderName = Path.GetFileName(seasonFolder);
            
            // Parse season number from folder name
            var seasonMatch = System.Text.RegularExpressions.Regex.Match(seasonFolderName, @"(?:Season\s+)?(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!seasonMatch.Success || !int.TryParse(seasonMatch.Groups[1].Value, out var seasonNumber))
            {
                _logger.LogWarning("Could not parse season number from folder: {Folder}", seasonFolderName);
                return;
            }

            _logger.LogDebug("Scanning season {Season} in folder: {Folder}", seasonNumber, seasonFolder);

            // Get episode files
            var episodeFiles = Directory.GetFiles(seasonFolder)
                .Where(f => _videoExtensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(f => f)
                .ToList();

            // Fetch season details from TMDB to get episode metadata
            TmdbSeasonDetails? seasonDetails = null;
            try
            {
                seasonDetails = await _tmdbClient.GetSeasonDetailsAsync(seriesDetails.Id, seasonNumber, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not fetch season {Season} details from TMDB for series {TmdbId}", seasonNumber, seriesDetails.Id);
            }

            foreach (var episodeFile in episodeFiles)
            {
                await ScanEpisodeFileAsync(contentId, episodeFile, seasonNumber, seasonDetails, seasonFolder, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan season folder: {Folder}", seasonFolder);
        }
    }

    /// <summary>
    /// Scan series with flat structure (episodes directly in series folder)
    /// </summary>
    private async Task ScanFlatSeriesStructureAsync(int contentId, string seriesFolder, List<string> episodeFiles, TmdbTvSeriesDetails seriesDetails, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Scanning flat series structure with {Count} episode files", episodeFiles.Count);

            foreach (var episodeFile in episodeFiles)
            {
                // Try to parse season and episode from filename
                var fileName = Path.GetFileNameWithoutExtension(episodeFile);
                var episodeMatch = System.Text.RegularExpressions.Regex.Match(fileName, @"S(\d+)E(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                if (episodeMatch.Success && 
                    int.TryParse(episodeMatch.Groups[1].Value, out var seasonNumber) &&
                    int.TryParse(episodeMatch.Groups[2].Value, out var episodeNumber))
                {
                    // Fetch season details if needed
                    TmdbSeasonDetails? seasonDetails = null;
                    try
                    {
                        seasonDetails = await _tmdbClient.GetSeasonDetailsAsync(seriesDetails.Id, seasonNumber, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not fetch season {Season} details from TMDB for series {TmdbId}", seasonNumber, seriesDetails.Id);
                    }

                    await ScanEpisodeFileAsync(contentId, episodeFile, seasonNumber, seasonDetails, seriesFolder, cancellationToken);
                }
                else
                {
                    _logger.LogWarning("Could not parse season/episode from filename: {FileName}", fileName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan flat series structure: {Folder}", seriesFolder);
        }
    }

    /// <summary>
    /// Scan individual episode file and add to database
    /// </summary>
    private async Task ScanEpisodeFileAsync(int contentId, string episodeFile, int seasonNumber, TmdbSeasonDetails? seasonDetails, string seasonFolder, CancellationToken cancellationToken)
    {
        try
        {
            var fileName = Path.GetFileNameWithoutExtension(episodeFile);
            
            // Try to parse episode number from filename
            var episodeMatch = System.Text.RegularExpressions.Regex.Match(fileName, @"E(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!episodeMatch.Success || !int.TryParse(episodeMatch.Groups[1].Value, out var episodeNumber))
            {
                // Try alternative patterns
                episodeMatch = System.Text.RegularExpressions.Regex.Match(fileName, @"(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (!episodeMatch.Success || !int.TryParse(episodeMatch.Groups[1].Value, out episodeNumber))
                {
                    _logger.LogWarning("Could not parse episode number from filename: {FileName}", fileName);
                    return;
                }
            }

            // Check if episode already exists
            var existingEpisode = await _context.Episodes
                .FirstOrDefaultAsync(e => e.ContentId == contentId && 
                                        e.SeasonNumber == seasonNumber && 
                                        e.EpisodeNumber == episodeNumber, cancellationToken);

            if (existingEpisode != null)
            {
                // Update file path if it changed
                if (existingEpisode.FilePath != episodeFile)
                {
                    existingEpisode.FilePath = episodeFile;
                    await _context.SaveChangesAsync(cancellationToken);
                    _logger.LogDebug("Updated episode file path: S{Season}E{Episode}", seasonNumber, episodeNumber);
                }
                return;
            }

            // Get episode metadata from TMDB season details
            TmdbEpisode? episodeDetails = null;
            if (seasonDetails != null)
            {
                episodeDetails = seasonDetails.Episodes.FirstOrDefault(e => e.EpisodeNumber == episodeNumber);
            }

            // Analyze media file
            var mediaInfo = await _mediaAnalyzer.AnalyzeAsync(episodeFile, cancellationToken);

            // Create episode entry
            var episode = new Episode
            {
                ContentId = contentId,
                TmdbId = episodeDetails?.Id,
                SeasonNumber = seasonNumber,
                EpisodeNumber = episodeNumber,
                Title = episodeDetails?.Name ?? $"Episode {episodeNumber}",
                Overview = episodeDetails?.Overview,
                AirDate = episodeDetails?.AirDate,
                StillPath = episodeDetails?.StillPath,
                FilePath = episodeFile,
                MediaInfo = mediaInfo,
                AddedAt = DateTime.UtcNow
            };

            _context.Episodes.Add(episode);
            await _context.SaveChangesAsync(cancellationToken);

            // Download episode still image if available
            if (!string.IsNullOrEmpty(episodeDetails?.StillPath))
            {
                try
                {
                    await _metadataService.DownloadEpisodeStillAsync(
                        episodeDetails.StillPath, 
                        seasonFolder, 
                        seasonNumber, 
                        episodeNumber, 
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to download episode still for S{Season}E{Episode}", seasonNumber, episodeNumber);
                }
            }

            _logger.LogInformation("Added episode to library: S{Season}E{Episode} - {Title}", seasonNumber, episodeNumber, episode.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan episode file: {File}", episodeFile);
        }
    }


}