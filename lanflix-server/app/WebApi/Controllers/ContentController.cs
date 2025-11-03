using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace Lanflix.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContentController : ControllerBase
{
    private readonly ITmdbClient _tmdbClient;
    private readonly IRadarrClient _radarrClient;
    private readonly ISonarrClient _sonarrClient;
    private readonly IProwlarrClient _prowlarrClient;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<ContentController> _logger;

    public ContentController(
        ITmdbClient tmdbClient,
        ISettingsService settingsService,
        IRadarrClient radarrClient,
        ISonarrClient sonarrClient,
        IProwlarrClient prowlarrClient,
        ILogger<ContentController> logger)
    {
        _tmdbClient = tmdbClient;
        _settingsService = settingsService;
        _radarrClient = radarrClient;
        _sonarrClient = sonarrClient;
        _prowlarrClient = prowlarrClient;
        _logger = logger;
    }

    /// <summary>
    /// Get trending and popular content for discovery
    /// </summary>
    [HttpGet("discover")]
    public async Task<IActionResult> GetDiscoverContent(
        [FromQuery] int? profileId,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting discover content: Page={Page}, ProfileId={ProfileId}", page, profileId);

            // Get trending and popular content from TMDB
            var trendingMoviesTask = _tmdbClient.GetTrendingAsync("movie", "week", cancellationToken);
            var trendingTvTask = _tmdbClient.GetTrendingAsync("tv", "week", cancellationToken);
            var popularMoviesTask = _tmdbClient.GetPopularMoviesAsync(page, cancellationToken);
            var popularTvTask = _tmdbClient.GetPopularTvSeriesAsync(page, cancellationToken);

            await Task.WhenAll(trendingMoviesTask, trendingTvTask, popularMoviesTask, popularTvTask);

            // Set MediaType and normalize titles for frontend
            var trendingMovies = trendingMoviesTask.Result.Results;
            foreach (var movie in trendingMovies) 
            {
                movie.MediaType = "movie";
                // Ensure title is set for movies (should already be set)
                if (string.IsNullOrEmpty(movie.Title) && !string.IsNullOrEmpty(movie.Name))
                    movie.Title = movie.Name;
            }
            
            var trendingSeries = trendingTvTask.Result.Results;
            foreach (var series in trendingSeries) 
            {
                series.MediaType = "tv";
                // Set title from name for TV series
                if (string.IsNullOrEmpty(series.Title) && !string.IsNullOrEmpty(series.Name))
                    series.Title = series.Name;
            }
            
            var popularMovies = popularMoviesTask.Result.Results;
            foreach (var movie in popularMovies) 
            {
                movie.MediaType = "movie";
                // Ensure title is set for movies (should already be set)
                if (string.IsNullOrEmpty(movie.Title) && !string.IsNullOrEmpty(movie.Name))
                    movie.Title = movie.Name;
            }
            
            var popularSeries = popularTvTask.Result.Results;
            foreach (var series in popularSeries) 
            {
                series.MediaType = "tv";
                // Set title from name for TV series
                if (string.IsNullOrEmpty(series.Title) && !string.IsNullOrEmpty(series.Name))
                    series.Title = series.Name;
            }

            var response = new
            {
                trending = new
                {
                    movies = trendingMovies,
                    series = trendingSeries
                },
                popular = new
                {
                    movies = popularMovies,
                    series = popularSeries
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get discover content");
            // Return empty results instead of error to keep UI working
            return Ok(new
            {
                trending = new { movies = new List<object>(), series = new List<object>() },
                popular = new { movies = new List<object>(), series = new List<object>() }
            });
        }
    }

    /// <summary>
    /// Get popular content (movies or TV series)
    /// </summary>
    [HttpGet("popular")]
    public async Task<IActionResult> GetPopularContent(
        [FromQuery] string type,
        [FromQuery] int page = 1,
        [FromQuery] int? profileId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting popular content: Type={Type}, Page={Page}", type, page);

            if (type == "movie")
            {
                var results = await _tmdbClient.GetPopularMoviesAsync(page, cancellationToken);
                // Set MediaType and normalize titles for frontend
                foreach (var movie in results.Results) 
                {
                    movie.MediaType = "movie";
                    // Ensure title is set for movies (should already be set)
                    if (string.IsNullOrEmpty(movie.Title) && !string.IsNullOrEmpty(movie.Name))
                        movie.Title = movie.Name;
                }
                return Ok(results.Results);
            }
            else if (type == "series" || type == "tv")
            {
                var results = await _tmdbClient.GetPopularTvSeriesAsync(page, cancellationToken);
                // Set MediaType and normalize titles for frontend
                foreach (var series in results.Results) 
                {
                    series.MediaType = "tv";
                    // Set title from name for TV series
                    if (string.IsNullOrEmpty(series.Title) && !string.IsNullOrEmpty(series.Name))
                        series.Title = series.Name;
                }
                return Ok(results.Results);
            }
            else
            {
                return BadRequest(new { error = "Invalid type. Must be 'movie' or 'series'" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get popular content: Type={Type}", type);
            return Ok(new List<object>()); // Return empty list to keep UI working
        }
    }

    /// <summary>
    /// Search TMDB for movies and TV series
    /// </summary>
    [HttpGet("discovery/search")]
    public async Task<IActionResult> SearchTMDB(
        [FromQuery] string q,
        [FromQuery] string type = "all",
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Searching TMDB: Query={Query}, Type={Type}", q, type);

            var results = new
            {
                movies = new List<TmdbSearchItem>(),
                series = new List<TmdbSearchItem>()
            };

            if (type == "all" || type == "movie")
            {
                var movieResults = await _tmdbClient.SearchMoviesAsync(q, cancellationToken);
                // Set MediaType and normalize titles for frontend
                foreach (var movie in movieResults.Results) 
                {
                    movie.MediaType = "movie";
                    // Ensure title is set for movies (should already be set)
                    if (string.IsNullOrEmpty(movie.Title) && !string.IsNullOrEmpty(movie.Name))
                        movie.Title = movie.Name;
                }
                results.movies.AddRange(movieResults.Results);
            }

            if (type == "all" || type == "tv")
            {
                var tvResults = await _tmdbClient.SearchTvSeriesAsync(q, cancellationToken);
                // Set MediaType and normalize titles for frontend
                foreach (var series in tvResults.Results) 
                {
                    series.MediaType = "tv";
                    // Set title from name for TV series
                    if (string.IsNullOrEmpty(series.Title) && !string.IsNullOrEmpty(series.Name))
                        series.Title = series.Name;
                }
                results.series.AddRange(tvResults.Results);
            }

            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search TMDB: Query={Query}", q);
            return StatusCode(500, new { error = "Failed to search content" });
        }
    }

    /// <summary>
    /// Get detailed content information from TMDB
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetContentDetails(
        [FromRoute] int id,
        [FromQuery] string? type = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting content details: Id={Id}, Type={Type}", id, type);

            if (string.IsNullOrEmpty(type) || type == "undefined")
            {
                return BadRequest(new { 
                    error = new {
                        message = "Type parameter is required. Must be 'movie' or 'tv'",
                        code = "MISSING_TYPE_PARAMETER",
                        details = new {
                            providedType = type ?? "null",
                            expectedValues = new[] { "movie", "tv", "series" },
                            hint = "The frontend should pass ?type=movie or ?type=tv in the URL"
                        }
                    }
                });
            }

            if (type == "movie")
            {
                var details = await _tmdbClient.GetMovieDetailsAsync(id, cancellationToken);
                if (details == null)
                {
                    return NotFound(new { 
                        error = new {
                            message = "Movie not found",
                            code = "MOVIE_NOT_FOUND",
                            details = new { tmdbId = id }
                        }
                    });
                }
                return Ok(details);
            }
            else if (type == "tv" || type == "series")
            {
                var details = await _tmdbClient.GetTvSeriesDetailsAsync(id, cancellationToken);
                if (details == null)
                {
                    return NotFound(new { 
                        error = new {
                            message = "TV series not found",
                            code = "SERIES_NOT_FOUND",
                            details = new { tmdbId = id }
                        }
                    });
                }
                return Ok(details);
            }
            else
            {
                return BadRequest(new { 
                    error = new {
                        message = $"Invalid content type: '{type}'. Must be 'movie' or 'tv'",
                        code = "INVALID_TYPE_PARAMETER",
                        details = new {
                            providedType = type,
                            expectedValues = new[] { "movie", "tv", "series" }
                        }
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get content details: Id={Id}, Type={Type}", id, type);
            return StatusCode(500, new { 
                error = new {
                    message = "Failed to get content details",
                    code = "INTERNAL_ERROR",
                    details = ex.Message
                }
            });
        }
    }

    /// <summary>
    /// Get episodes for a TV series season
    /// </summary>
    [HttpGet("{id}/episodes")]
    public async Task<IActionResult> GetEpisodes(
        [FromRoute] int id,
        [FromQuery] int? season,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting episodes: SeriesId={SeriesId}, Season={Season}", id, season);

            if (season.HasValue)
            {
                var seasonDetails = await _tmdbClient.GetSeasonDetailsAsync(id, season.Value, cancellationToken);
                if (seasonDetails == null)
                {
                    return NotFound(new { error = "Season not found" });
                }
                return Ok(seasonDetails);
            }
            else
            {
                // Get series details to return all seasons
                var seriesDetails = await _tmdbClient.GetTvSeriesDetailsAsync(id, cancellationToken);
                if (seriesDetails == null)
                {
                    return NotFound(new { error = "TV series not found" });
                }
                return Ok(new { seasons = seriesDetails.Seasons });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get episodes: SeriesId={SeriesId}, Season={Season}", id, season);
            return StatusCode(500, new { error = "Failed to get episodes" });
        }
    }

    /// <summary>
    /// Queue a movie download via Radarr/Sonarr
    /// </summary>
    [HttpPost("{id}/queue")]
    public async Task<IActionResult> QueueDownload(
        [FromRoute] int id,
        [FromBody] QueueDownloadRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request == null)
            {
                _logger.LogError("Queue request body is null");
                return BadRequest(new { error = "Request body is required" });
            }

            if (string.IsNullOrEmpty(request.Type))
            {
                _logger.LogError("Queue request Type is missing");
                return BadRequest(new { error = "Type is required (movie or series)" });
            }

            if (string.IsNullOrEmpty(request.Title))
            {
                _logger.LogError("Queue request Title is missing");
                return BadRequest(new { error = "Title is required" });
            }

            _logger.LogInformation("Queueing download: Id={Id}, Type={Type}, Title={Title}, ProfileId={ProfileId}", 
                id, request.Type, request.Title, request.ProfileId);

            if (request.Type == "movie")
            {
                // Get settings to use configured media paths
                var settings = await _settingsService.GetSettingsAsync(cancellationToken);
                
                // Get root folders and quality profiles
                var rootFolders = await _radarrClient.GetRootFoldersAsync(cancellationToken);
                var qualityProfiles = await _radarrClient.GetQualityProfilesAsync(cancellationToken);

                if (qualityProfiles.Count == 0)
                {
                    return BadRequest(new { error = "Radarr has no quality profiles configured. Please configure quality profiles in Radarr." });
                }

                // Determine root folder path - use configured media path or first available root folder
                string rootFolderPath;
                if (!string.IsNullOrEmpty(settings.MediaPaths.Movies))
                {
                    // Use configured media path
                    rootFolderPath = settings.MediaPaths.Movies;
                    
                    // Check if this path exists in Radarr's root folders
                    var matchingFolder = rootFolders.FirstOrDefault(f => 
                        f.Path.TrimEnd('\\', '/').Equals(rootFolderPath.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase));
                    
                    if (matchingFolder == null)
                    {
                        _logger.LogWarning("Configured movie path '{Path}' not found in Radarr root folders. Available folders: {Folders}", 
                            rootFolderPath, string.Join(", ", rootFolders.Select(f => f.Path)));
                        
                        return BadRequest(new { 
                            error = new {
                                message = $"The configured movie path '{rootFolderPath}' is not set up as a root folder in Radarr.",
                                code = "PATH_NOT_IN_RADARR",
                                details = new {
                                    configuredPath = rootFolderPath,
                                    availablePaths = rootFolders.Select(f => f.Path).ToArray(),
                                    solution = "Either add this path as a root folder in Radarr, or change the Movies path in Lanflix settings to one of the available paths."
                                }
                            }
                        });
                    }
                }
                else if (rootFolders.Count > 0)
                {
                    // Use first available root folder from Radarr
                    rootFolderPath = rootFolders[0].Path;
                    _logger.LogInformation("No movie path configured in Lanflix settings. Using Radarr's first root folder: {Path}", rootFolderPath);
                }
                else
                {
                    return BadRequest(new { 
                        error = new {
                            message = "No root folders available for movie downloads.",
                            code = "NO_ROOT_FOLDERS",
                            details = new {
                                radarrConfigured = !string.IsNullOrEmpty(settings.ExternalApis.Radarr.Url),
                                solution = "Configure root folders in Radarr under Settings > Media Management > Root Folders."
                            }
                        }
                    });
                }

                // Check if movie already exists
                var existingMovie = await _radarrClient.GetMovieByTmdbIdAsync(id, cancellationToken);
                if (existingMovie != null)
                {
                    return Ok(new { message = "Movie already exists in Radarr", movieId = existingMovie.Id });
                }

                // Add movie to Radarr
                var movie = await _radarrClient.AddMovieAsync(new AddRadarrMovieRequest
                {
                    TmdbId = id,
                    Title = request.Title,
                    Year = request.Year ?? DateTime.Now.Year,
                    QualityProfileId = qualityProfiles[0].Id,
                    RootFolderPath = rootFolderPath,
                    Monitored = true,
                    SearchForMovie = true
                }, cancellationToken);

                return Ok(new { message = "Movie queued for download", movieId = movie.Id });
            }
            else if (request.Type == "series")
            {
                // Get settings to use configured media paths
                var settings = await _settingsService.GetSettingsAsync(cancellationToken);
                
                // Get root folders and quality profiles
                var rootFolders = await _sonarrClient.GetRootFoldersAsync(cancellationToken);
                var qualityProfiles = await _sonarrClient.GetQualityProfilesAsync(cancellationToken);

                if (qualityProfiles.Count == 0)
                {
                    return BadRequest(new { error = "Sonarr has no quality profiles configured. Please configure quality profiles in Sonarr." });
                }

                // Determine root folder path - use configured media path or first available root folder
                string rootFolderPath;
                if (!string.IsNullOrEmpty(settings.MediaPaths.Series))
                {
                    // Use configured media path
                    rootFolderPath = settings.MediaPaths.Series;
                    
                    // Check if this path exists in Sonarr's root folders
                    var matchingFolder = rootFolders.FirstOrDefault(f => 
                        f.Path.TrimEnd('\\', '/').Equals(rootFolderPath.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase));
                    
                    if (matchingFolder == null)
                    {
                        _logger.LogWarning("Configured series path '{Path}' not found in Sonarr root folders. You need to add this path as a root folder in Sonarr.", rootFolderPath);
                        return BadRequest(new { 
                            error = $"The configured series path '{rootFolderPath}' is not set up as a root folder in Sonarr. Please add it in Sonarr's settings under Media Management > Root Folders.",
                            hint = "Go to Sonarr > Settings > Media Management > Root Folders and add: " + rootFolderPath
                        });
                    }
                }
                else if (rootFolders.Count > 0)
                {
                    // Use first available root folder from Sonarr
                    rootFolderPath = rootFolders[0].Path;
                    _logger.LogInformation("No series path configured in Lanflix settings. Using Sonarr's first root folder: {Path}", rootFolderPath);
                }
                else
                {
                    return BadRequest(new { 
                        error = "No root folders configured in Sonarr and no series path set in Lanflix settings. Please configure a series path in Lanflix settings or add a root folder in Sonarr.",
                        hint = "Set the Series path in Lanflix settings to match where you want Sonarr to download TV shows."
                    });
                }

                // For series, we need to search by title to get TVDB ID
                var searchResults = await _sonarrClient.SearchSeriesAsync(request.Title, cancellationToken);
                var match = searchResults.FirstOrDefault(s => s.Title.Equals(request.Title, StringComparison.OrdinalIgnoreCase));

                if (match == null)
                {
                    return NotFound(new { error = "Series not found in Sonarr database" });
                }

                // Check if series already exists
                var existingSeries = await _sonarrClient.GetSeriesByTvdbIdAsync(match.TvdbId, cancellationToken);
                if (existingSeries != null)
                {
                    return Ok(new { message = "Series already exists in Sonarr", seriesId = existingSeries.Id });
                }

                // Add series to Sonarr
                var series = await _sonarrClient.AddSeriesAsync(new AddSonarrSeriesRequest
                {
                    TvdbId = match.TvdbId,
                    Title = request.Title,
                    QualityProfileId = qualityProfiles[0].Id,
                    RootFolderPath = rootFolderPath,
                    Monitored = true,
                    SearchForMissingEpisodes = true
                }, cancellationToken);

                return Ok(new { message = "Series queued for download", seriesId = series.Id });
            }

            return BadRequest(new { error = "Invalid content type" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue download: Id={Id}, Type={Type}", id, request.Type);
            return StatusCode(500, new { error = "Failed to queue download", details = ex.Message });
        }
    }

    /// <summary>
    /// Test connection to external services
    /// </summary>
    [HttpPost("test-connection")]
    public async Task<IActionResult> TestConnection(
        [FromBody] TestConnectionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Testing connection to: {Service}", request.Service);

            var result = request.Service.ToLower() switch
            {
                "tmdb" => await _tmdbClient.SearchMoviesAsync("test", cancellationToken) != null,
                "radarr" => await _radarrClient.TestConnectionAsync(cancellationToken),
                "sonarr" => await _sonarrClient.TestConnectionAsync(cancellationToken),
                "prowlarr" => await _prowlarrClient.TestConnectionAsync(cancellationToken),
                _ => false
            };

            if (result)
            {
                return Ok(new { message = $"{request.Service} connection successful" });
            }
            else
            {
                return BadRequest(new { error = $"Failed to connect to {request.Service}" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed: Service={Service}", request.Service);
            return StatusCode(500, new { error = $"Connection test failed: {ex.Message}" });
        }
    }
}

public class QueueDownloadRequest
{
    public string Type { get; set; } = string.Empty; // "movie" or "series"
    public string Title { get; set; } = string.Empty;
    public int? Year { get; set; }
    public int ProfileId { get; set; }
}

public class TestConnectionRequest
{
    public string Service { get; set; } = string.Empty;
}
