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

                    // Remove related records
                    var watchHistories = await _context.WatchHistories.Where(w => w.ContentId == content.Id).ToListAsync(cancellationToken);
                    _context.WatchHistories.RemoveRange(watchHistories);

                    // Remove the content
                    _context.Contents.Remove(content);
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
        var existing = await _context.Contents
            .FirstOrDefaultAsync(c => c.FilePath == videoFile, cancellationToken);

        if (existing != null)
        {
            _logger.LogDebug("Movie already exists in database: {Title}", existing.Title);
            return;
        }

        // Try to load metadata from folder
        var metadata = await _metadataService.LoadMetadataFromMediaFolderAsync(movieFolder, cancellationToken);
        
        // If no metadata, try to search and fetch from TMDB
        if (metadata == null)
        {
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
        }
    }

    private async Task ScanSingleSeriesFolderAsync(string seriesFolder, LibraryScanResult stats, CancellationToken cancellationToken)
    {
        var folderName = Path.GetFileName(seriesFolder);
        
        // Check if series already exists in database
        var existing = await _context.Contents
            .FirstOrDefaultAsync(c => c.FilePath == seriesFolder && c.Type == ContentType.Series, cancellationToken);

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
                _logger.LogDebug("Movie already exists in library: {Title}", existing.Title);
                return;
            }

            // Fetch metadata from TMDB
            var movieDetails = await _tmdbClient.GetMovieDetailsAsync(tmdbId, cancellationToken);

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
            // Check if already exists
            var existing = await _context.Contents
                .FirstOrDefaultAsync(c => c.TmdbId == tmdbId && c.Type == ContentType.Series, cancellationToken);

            if (existing != null)
            {
                _logger.LogDebug("Series already exists in library: {Title}", existing.Title);
                return;
            }

            // Fetch metadata from TMDB
            var seriesDetails = await _tmdbClient.GetTvSeriesDetailsAsync(tmdbId, cancellationToken);

            // Create content entry
            var content = new Content
            {
                TmdbId = tmdbId,
                Type = ContentType.Series,
                Title = seriesDetails.Name,
                Overview = seriesDetails.Overview,
                ReleaseDate = seriesDetails.FirstAirDate,
                PosterPath = seriesDetails.PosterPath,
                BackdropPath = seriesDetails.BackdropPath,
                Rating = seriesDetails.VoteAverage,
                Genres = seriesDetails.Genres?.Select(g => g.Name).ToArray(),
                FilePath = seriesFolder,
                AddedAt = DateTime.UtcNow
            };

            _context.Contents.Add(content);
            await _context.SaveChangesAsync(cancellationToken);

            // Save metadata to media folder (like old backend)
            await _metadataService.SaveMetadataToMediaFolderAsync(content.Id, seriesFolder, cancellationToken);

            stats.Added++;
            _logger.LogInformation("Added series to library: {Title} ({Year})", content.Title, content.ReleaseDate?.Year);

            // TODO: Fetch and store episode metadata like the old backend
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add series to library: {TmdbId}", tmdbId);
            throw;
        }
    }
}