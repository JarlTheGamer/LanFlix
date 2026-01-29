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
        ILogger<LibraryService> logger)
    {
        _context = context;
        _settingsService = settingsService;
        _metadataService = metadataService;
        _tmdbClient = tmdbClient;
        _mediaAnalyzer = mediaAnalyzer;
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

            // Preload movies to optimize performance (avoid N+1 queries)
            var allMovies = await _context.Contents
                .Where(c => c.Type == ContentType.Movie)
                .ToListAsync(cancellationToken);
            
            // Build in-memory lookups
            // Windows file system is case-insensitive
            var moviesByPath = new Dictionary<string, Content>(StringComparer.OrdinalIgnoreCase);
            var moviesByTmdb = new Dictionary<int, Content>();

            foreach (var m in allMovies)
            {
                if (!string.IsNullOrEmpty(m.FilePath))
                    moviesByPath[m.FilePath] = m;
                
                if (!moviesByTmdb.ContainsKey(m.TmdbId))
                    moviesByTmdb[m.TmdbId] = m;
            }

            var directories = Directory.GetDirectories(moviesPath);

            foreach (var movieFolder in directories)
            {
                try
                {
                    await ScanMovieFolderAsync(movieFolder, moviesByPath, moviesByTmdb, stats, cancellationToken);
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

            // Preload series to optimize performance (avoid N+1 queries)
            var allSeries = await _context.Contents
                .Where(c => c.Type == ContentType.Series)
                .ToListAsync(cancellationToken);
            
            var seriesByPath = new Dictionary<string, Content>(StringComparer.OrdinalIgnoreCase);
            var seriesByTmdb = new Dictionary<int, Content>();

            foreach (var s in allSeries)
            {
                if (!string.IsNullOrEmpty(s.FilePath))
                    seriesByPath[s.FilePath] = s;
                
                if (!seriesByTmdb.ContainsKey(s.TmdbId))
                    seriesByTmdb[s.TmdbId] = s;
            }

            var directories = Directory.GetDirectories(seriesPath);

            foreach (var seriesFolder in directories)
            {
                try
                {
                    await ScanSingleSeriesFolderAsync(seriesFolder, seriesByPath, seriesByTmdb, stats, cancellationToken);
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
            _logger.LogInformation("Cleaning up missing content (Optimized)");

            // Get all content from database (projection to reduce memory)
            var allContent = await _context.Contents
                .Select(c => new { c.Id, c.Type, c.FilePath, c.Title })
                .ToListAsync(cancellationToken);

            var episodes = await _context.Episodes
                 .Select(e => new { e.Id, e.FilePath, e.SeasonNumber, e.EpisodeNumber })
                 .ToListAsync(cancellationToken);

            var missingContentIds = new System.Collections.Concurrent.ConcurrentBag<int>();
            var missingEpisodeIds = new System.Collections.Concurrent.ConcurrentBag<int>();

            // Process content in parallel
            await Parallel.ForEachAsync(allContent, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = cancellationToken }, async (content, ct) => 
            {
                var shouldRemove = false;
                if (string.IsNullOrEmpty(content.FilePath))
                {
                    shouldRemove = true;
                }
                else
                {
                    var exists = content.Type == ContentType.Movie ? File.Exists(content.FilePath) : Directory.Exists(content.FilePath);
                    if (!exists) shouldRemove = true;
                }

                if (shouldRemove)
                {
                    missingContentIds.Add(content.Id);
                }
                
                await Task.CompletedTask;
            });

             // Process episodes in parallel
            await Parallel.ForEachAsync(episodes, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = cancellationToken }, async (episode, ct) => 
            {
                if (!string.IsNullOrEmpty(episode.FilePath) && !File.Exists(episode.FilePath))
                {
                    missingEpisodeIds.Add(episode.Id);
                }
                await Task.CompletedTask;
            });

            // Remove contents
            if (!missingContentIds.IsEmpty)
            {
                var idsToRemove = missingContentIds.ToList();
                _logger.LogInformation("Removing {Count} missing content items", idsToRemove.Count);
                
                // Fetch full entities for proper removal (cascade delete handling if not database-side)
                var contentsToRemove = await _context.Contents
                    .Where(c => idsToRemove.Contains(c.Id))
                    .Include(c => c.WatchHistories)
                    .ToListAsync(cancellationToken);

                _context.Contents.RemoveRange(contentsToRemove);
                stats.Removed += contentsToRemove.Count;
            }

            // Remove episodes
            if (!missingEpisodeIds.IsEmpty)
            {
                var idsToRemove = missingEpisodeIds.ToList();
                _logger.LogInformation("Removing {Count} missing episodes", idsToRemove.Count);
                
                var episodesToRemove = await _context.Episodes
                    .Where(e => idsToRemove.Contains(e.Id))
                    .ToListAsync(cancellationToken);

                _context.Episodes.RemoveRange(episodesToRemove);
                stats.Removed += episodesToRemove.Count;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var error = $"Failed to cleanup missing content: {ex.Message}";
            _logger.LogError(ex, error);
            stats.Errors.Add(error);
        }
    }

    private async Task ScanMovieFolderAsync(string movieFolder, Dictionary<string, Content> moviesByPath, Dictionary<int, Content> moviesByTmdb, LibraryScanResult stats, CancellationToken cancellationToken)
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

        // Check if movie already exists in memory cache (avoids DB query)
        moviesByPath.TryGetValue(videoFile, out var existing);

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
                // Check if we need to add/update content in database using memory cache
                moviesByTmdb.TryGetValue(tmdbId, out var existingByTmdb);

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
                                // Update dictionary
                                moviesByPath[fullFilePath] = existingByTmdb;
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
                    await AddMovieToLibraryAsync(tmdbId, videoFile, movieFolder, moviesByTmdb, moviesByPath, stats, cancellationToken);
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
                        await AddMovieToLibraryAsync(movieMatch.Id, videoFile, movieFolder, moviesByTmdb, moviesByPath, stats, cancellationToken);
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



    private async Task ScanSingleSeriesFolderAsync(string seriesFolder, Dictionary<string, Content> seriesByPath, Dictionary<int, Content> seriesByTmdb, LibraryScanResult stats, CancellationToken cancellationToken)
    {
        var folderName = Path.GetFileName(seriesFolder);
        
        // Check if series already exists in database by file path (using cache)
        seriesByPath.TryGetValue(seriesFolder, out var existingByPath);

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
        
        // If no metadata, try to search TMDB by folder name
        if (metadata == null)
        {
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
                    await AddSeriesToLibraryAsync(seriesMatch.Id, seriesFolder, seriesByTmdb, seriesByPath, stats, cancellationToken);
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
        // TODO: Handle metadata case if implemented similar to Movies
    }

    /// <summary>
    /// Add movie to library (following old backend pattern)
    /// </summary>
    private async Task AddMovieToLibraryAsync(int tmdbId, string filePath, string movieFolder, Dictionary<int, Content> moviesByTmdb, Dictionary<string, Content> moviesByPath, LibraryScanResult stats, CancellationToken cancellationToken)
    {
        try
        {
            // Check if already exists using dictionary
            moviesByTmdb.TryGetValue(tmdbId, out var existing);

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

            // Update dictionaries
            moviesByTmdb[tmdbId] = content;
            moviesByPath[filePath] = content;

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
    /// <summary>
    /// Add series to library (following old backend pattern)
    /// </summary>
    private async Task AddSeriesToLibraryAsync(int tmdbId, string seriesFolder, Dictionary<int, Content> seriesByTmdb, Dictionary<string, Content> seriesByPath, LibraryScanResult stats, CancellationToken cancellationToken)
    {
        try
        {
            // Use the same approach as the old backend - simple check and upsert pattern
            _logger.LogInformation("TMDB TV series search completed: {SeriesName}, Results: 1", Path.GetFileName(seriesFolder));
            _logger.LogInformation("Found TMDB match for series {SeriesName}: {TmdbId}", Path.GetFileName(seriesFolder), tmdbId);
            
            // Check if already exists by TMDB ID and type (using dict)
            seriesByTmdb.TryGetValue(tmdbId, out var existing);

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
                    
                    // Update cache
                    if (seriesByPath.ContainsKey(seriesFolder)) seriesByPath.Remove(seriesFolder);
                    seriesByPath[seriesFolder] = existing;
                    
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

            // Update dictionaries
            seriesByTmdb[tmdbId] = content;
            seriesByPath[seriesFolder] = content;

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

            // Preload existing episodes for this series to avoid N+1 lookups
            var existingEpisodes = await _context.Episodes
                .Where(e => e.ContentId == contentId)
                .ToListAsync(cancellationToken);
            
            var episodesMap = existingEpisodes
                .GroupBy(e => (e.SeasonNumber, e.EpisodeNumber))
                .ToDictionary(g => g.Key, g => g.First());

            // Scan each season folder
            var seasonFolders = Directory.GetDirectories(seriesFolder)
                .Where(d => Path.GetFileName(d).StartsWith("Season ", StringComparison.OrdinalIgnoreCase) ||
                           System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(d), @"^S\d+", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                .ToList();

            foreach (var seasonFolder in seasonFolders)
            {
                await ScanSeasonFolderAsync(contentId, seasonFolder, seriesDetails, episodesMap, cancellationToken);
            }

            // Also check for episodes directly in the series folder (flat structure)
            var episodeFiles = Directory.GetFiles(seriesFolder)
                .Where(f => _videoExtensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (episodeFiles.Any())
            {
                await ScanFlatSeriesStructureAsync(contentId, seriesFolder, episodeFiles, seriesDetails, episodesMap, cancellationToken);
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
    private async Task ScanSeasonFolderAsync(int contentId, string seasonFolder, TmdbTvSeriesDetails seriesDetails, Dictionary<(int, int), Episode> episodesMap, CancellationToken cancellationToken)
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
                await ScanEpisodeFileAsync(contentId, episodeFile, seasonNumber, seasonDetails, seasonFolder, episodesMap, cancellationToken);
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
    private async Task ScanFlatSeriesStructureAsync(int contentId, string seriesFolder, List<string> episodeFiles, TmdbTvSeriesDetails seriesDetails, Dictionary<(int, int), Episode> episodesMap, CancellationToken cancellationToken)
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

                    await ScanEpisodeFileAsync(contentId, episodeFile, seasonNumber, seasonDetails, seriesFolder, episodesMap, cancellationToken);
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
    private async Task ScanEpisodeFileAsync(int contentId, string episodeFile, int seasonNumber, TmdbSeasonDetails? seasonDetails, string seasonFolder, Dictionary<(int, int), Episode> episodesMap, CancellationToken cancellationToken)
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

            // Check if episode already exists using map
            episodesMap.TryGetValue((seasonNumber, episodeNumber), out var existingEpisode);

            if (existingEpisode != null)
            {
                // Update file path if it changed
                if (existingEpisode.FilePath != episodeFile)
                {
                    existingEpisode.FilePath = episodeFile;
                    await _context.SaveChangesAsync(cancellationToken);
                    _logger.LogDebug("Updated episode file path: S{Season}E{Episode}", seasonNumber, episodeNumber);
                    
                    // Update map
                    episodesMap[(seasonNumber, episodeNumber)] = existingEpisode;
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