using System.IO;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Domain.Entities;
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
    private readonly ILogger<LibraryService> _logger;

    private readonly string[] _videoExtensions = { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v" };
    private readonly string[] _moviesFolderNames = { "movies", "Movies", "MOVIES" };
    private readonly string[] _seriesFolderNames = { "series", "Series", "SERIES", "shows", "Shows", "SHOWS" };

    public LibraryService(
        IApplicationDbContext context,
        ISettingsService settingsService,
        IMetadataService metadataService,
        ITmdbClient tmdbClient,
        ILogger<LibraryService> logger)
    {
        _context = context;
        _settingsService = settingsService;
        _metadataService = metadataService;
        _tmdbClient = tmdbClient;
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
                    await ScanSeriesFolderAsync(seriesFolder, stats, cancellationToken);
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
            var allContent = await _context.Content.ToListAsync(cancellationToken);

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
                    var exists = content.Type == "movie" ? File.Exists(content.FilePath) : Directory.Exists(content.FilePath);
                    if (!exists)
                    {
                        shouldRemove = true;
                        reason = $"{(content.Type == "movie" ? "file" : "folder")} no longer exists: {content.FilePath}";
                    }
                }

                if (shouldRemove)
                {
                    _logger.LogInformation("Removing content {Id} ({Title}) - {Reason}", content.Id, content.Title, reason);

                    // Remove related records
                    var watchHistories = await _context.WatchHistory.Where(w => w.ContentId == content.Id).ToListAsync(cancellationToken);
                    _context.WatchHistory.RemoveRange(watchHistories);

                    // Remove the content
                    _context.Content.Remove(content);
                    stats.Removed++;
                }
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
        var existing = await _context.Content
            .FirstOrDefaultAsync(c => c.FilePath == videoFile, cancellationToken);

        if (existing != null)
        {
            _logger.LogDebug("Movie already exists in database: {Title}", existing.Title);
            return;
        }

        // Try to load metadata from folder
        var metadata = await _metadataService.LoadMetadataFromMediaFolderAsync(movieFolder, cancellationToken);
        
        // If no metadata, try to search TMDB by folder name
        if (metadata == null)
        {
            try
            {
                var searchResults = await _tmdbClient.SearchMoviesAsync(folderName, cancellationToken);
                var match = searchResults.Results.FirstOrDefault();
                
                if (match != null)
                {
                    // Save metadata to folder
                    await _metadataService.SaveMetadataToMediaFolderAsync(match.Id, "movie", movieFolder, cancellationToken);
                    
                    // Create content entry
                    var content = new Content
                    {
                        TmdbId = match.Id,
                        Title = match.Title,
                        Type = "movie",
                        FilePath = videoFile,
                        Year = match.ReleaseDate?.Year,
                        Overview = match.Overview,
                        PosterUrl = match.PosterPath,
                        BackdropUrl = match.BackdropPath,
                        VoteAverage = match.VoteAverage,
                        Runtime = 0, // Will be filled by metadata service
                        AddedAt = DateTime.UtcNow
                    };

                    _context.Content.Add(content);
                    await _context.SaveChangesAsync(cancellationToken);
                    
                    stats.Added++;
                    _logger.LogInformation("Added movie: {Title} ({Year})", content.Title, content.Year);
                }
                else
                {
                    stats.Errors.Add($"No TMDB match found for movie folder: {folderName}");
                }
            }
            catch (Exception ex)
            {
                stats.Errors.Add($"Failed to fetch metadata for movie {folderName}: {ex.Message}");
            }
        }
    }

    private async Task ScanSeriesFolderAsync(string seriesFolder, LibraryScanResult stats, CancellationToken cancellationToken)
    {
        var folderName = Path.GetFileName(seriesFolder);
        
        // Check if series already exists in database
        var existing = await _context.Content
            .FirstOrDefaultAsync(c => c.FilePath == seriesFolder && c.Type == "series", cancellationToken);

        if (existing != null)
        {
            _logger.LogDebug("Series already exists in database: {Title}", existing.Title);
            // TODO: Scan for new episodes
            return;
        }

        // Try to load metadata from folder
        var metadata = await _metadataService.LoadMetadataFromMediaFolderAsync(seriesFolder, cancellationToken);
        
        // If no metadata, try to search TMDB by folder name
        if (metadata == null)
        {
            try
            {
                var searchResults = await _tmdbClient.SearchTvSeriesAsync(folderName, cancellationToken);
                var match = searchResults.Results.FirstOrDefault();
                
                if (match != null)
                {
                    // Save metadata to folder
                    await _metadataService.SaveMetadataToMediaFolderAsync(match.Id, "tv", seriesFolder, cancellationToken);
                    
                    // Create content entry
                    var content = new Content
                    {
                        TmdbId = match.Id,
                        Title = match.Name,
                        Type = "series",
                        FilePath = seriesFolder,
                        Year = match.FirstAirDate?.Year,
                        Overview = match.Overview,
                        PosterUrl = match.PosterPath,
                        BackdropUrl = match.BackdropPath,
                        VoteAverage = match.VoteAverage,
                        AddedAt = DateTime.UtcNow
                    };

                    _context.Content.Add(content);
                    await _context.SaveChangesAsync(cancellationToken);
                    
                    stats.Added++;
                    _logger.LogInformation("Added series: {Title} ({Year})", content.Title, content.Year);
                    
                    // TODO: Scan episodes in season folders
                }
                else
                {
                    stats.Errors.Add($"No TMDB match found for series folder: {folderName}");
                }
            }
            catch (Exception ex)
            {
                stats.Errors.Add($"Failed to fetch metadata for series {folderName}: {ex.Message}");
            }
        }
    }
}