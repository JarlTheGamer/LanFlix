using Lanflix.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.Settings;

/// <summary>
/// Initializes default media folders on first run
/// </summary>
public class MediaFolderInitializer
{
    private readonly IConfiguration _configuration;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<MediaFolderInitializer> _logger;

    public MediaFolderInitializer(
        IConfiguration configuration,
        ISettingsService settingsService,
        ILogger<MediaFolderInitializer> logger)
    {
        _configuration = configuration;
        _settingsService = settingsService;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking media folder configuration...");

        try
        {
            // Determine default base path (current working directory)
            string basePath = GetDefaultMediaBasePath();
            _logger.LogInformation("Using base path for media folders: {BasePath}", basePath);

            // Get current settings
            var settings = await _settingsService.GetSettingsAsync(cancellationToken);

            // Check if media paths are already configured
            bool needsConfiguration = string.IsNullOrEmpty(settings.MediaPaths.Movies) ||
                                     string.IsNullOrEmpty(settings.MediaPaths.Series);

            if (!needsConfiguration)
            {
                _logger.LogInformation("Media paths already configured: Movies={Movies}, Series={Series}",
                    settings.MediaPaths.Movies, settings.MediaPaths.Series);
                
                // Still ensure the configured directories exist
                CreateDirectoryIfNotExists(settings.MediaPaths.Movies);
                CreateDirectoryIfNotExists(settings.MediaPaths.Series);
                return;
            }

            // Create default folder structure
            var moviesPath = Path.Combine(basePath, "Movies");
            var seriesPath = Path.Combine(basePath, "Series");

            // Create directories if they don't exist
            CreateDirectoryIfNotExists(moviesPath);
            CreateDirectoryIfNotExists(seriesPath);

            // Update settings with default paths
            // Note: PosterCache and BackdropCache are left empty - images are stored in media folders
            settings.MediaPaths.Movies = moviesPath;
            settings.MediaPaths.Series = seriesPath;
            settings.MediaPaths.PosterCache = string.Empty;
            settings.MediaPaths.BackdropCache = string.Empty;

            await _settingsService.UpdateSettingsAsync(settings, cancellationToken);

            _logger.LogInformation("✓ Default media folders created successfully:");
            _logger.LogInformation("  Movies: {MoviesPath}", moviesPath);
            _logger.LogInformation("  Series: {SeriesPath}", seriesPath);
            _logger.LogInformation("");
            _logger.LogInformation("📁 Images (posters/backdrops) will be stored in each media folder:");
            _logger.LogInformation("   Example: {MoviesPath}\\The Matrix (1999)\\poster.jpg", moviesPath);
            _logger.LogInformation("   Example: {MoviesPath}\\The Matrix (1999)\\backdrop.jpg", moviesPath);
            _logger.LogInformation("");
            _logger.LogInformation("📁 Configure Radarr/Sonarr to use these paths:");
            _logger.LogInformation("   Radarr Root Folder: {MoviesPath}", moviesPath);
            _logger.LogInformation("   Sonarr Root Folder: {SeriesPath}", seriesPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize media folders");
            // Don't throw - allow the app to start even if folder creation fails
        }
    }

    private string GetDefaultMediaBasePath()
    {
        // Priority order:
        // 1. Environment variable LANFLIX_MEDIA_PATH
        // 2. Current working directory (like Minecraft server - where you run it from)

        var envPath = Environment.GetEnvironmentVariable("LANFLIX_MEDIA_PATH");
        if (!string.IsNullOrEmpty(envPath))
        {
            return envPath;
        }

        // Use current working directory - where the user runs the server from
        // This is like Minecraft server - creates folders in the directory you're in
        var workingDir = Directory.GetCurrentDirectory();
        return Path.Combine(workingDir, "Media");
    }

    private void CreateDirectoryIfNotExists(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                _logger.LogInformation("Created directory: {Path}", path);

                // Create a README file in each main folder
                if (path.EndsWith("Movies") || path.EndsWith("Series"))
                {
                    var readmePath = Path.Combine(path, "README.txt");
                    var folderType = path.EndsWith("Movies") ? "movies" : "TV series";
                    var content = $@"Lanflix Media Folder - {folderType.ToUpper()}
=====================================

This folder is used by Lanflix to store and organize your {folderType}.

IMPORTANT: Configure Radarr/Sonarr to use this path
---------------------------------------------------
1. Open Radarr (for movies) or Sonarr (for TV series)
2. Go to Settings > Media Management > Root Folders
3. Add this path as a root folder: {path}

Folder Structure
----------------
{(path.EndsWith("Movies") ? 
@"Movies are organized as:
  Movie Title (Year)/
    Movie Title (Year).mkv
    poster.jpg          ← Downloaded from TMDB
    backdrop.jpg        ← Downloaded from TMDB
    metadata.json       ← Movie metadata

Example:
  The Matrix (1999)/
    The Matrix (1999).mkv
    poster.jpg
    backdrop.jpg
    metadata.json" :
@"TV series are organized as:
  Series Title/
    poster.jpg          ← Series poster from TMDB
    backdrop.jpg        ← Series backdrop from TMDB
    metadata.json       ← Series metadata
    Season 01/
      Series Title - S01E01.mkv
      Series Title - S01E02.mkv
      S01E01.jpg        ← Episode thumbnail
      S01E02.jpg        ← Episode thumbnail
    Season 02/
      ...

Example:
  Breaking Bad/
    poster.jpg
    backdrop.jpg
    metadata.json
    Season 01/
      Breaking Bad - S01E01.mkv
      S01E01.jpg")}

Images are stored locally in each media folder, not in a separate cache.
This allows you to backup/move content folders independently.

For more information, visit: https://github.com/yourusername/lanflix
";
                    File.WriteAllText(readmePath, content);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create directory: {Path}", path);
        }
    }
}
