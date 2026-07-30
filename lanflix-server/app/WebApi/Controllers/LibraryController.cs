using Lanflix.Application.Common.Interfaces;
using Lanflix.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LibraryController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<LibraryController> _logger;

    public LibraryController(
        IApplicationDbContext context,
        ILogger<LibraryController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all movies in library
    /// </summary>
    [HttpGet("movies")]
    public async Task<IActionResult> GetMovies(
        [FromQuery] string? genre = null,
        [FromQuery] string? sortBy = "addedAt",
        [FromQuery] string? sortOrder = "DESC",
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting library movies: Genre={Genre}, SortBy={SortBy}, Limit={Limit}", genre, sortBy, limit);

            var query = _context.Contents
                .AsNoTracking()
                .Where(c => c.Type == ContentType.Movie)
                .AsQueryable();

            // Filter by genre if specified
            if (!string.IsNullOrEmpty(genre))
            {
                query = query.Where(c => c.Genres != null && c.Genres.Contains(genre));
            }

            // Apply sorting
            query = sortBy?.ToLower() switch
            {
                "title" => sortOrder?.ToUpper() == "ASC" 
                    ? query.OrderBy(c => c.Title) 
                    : query.OrderByDescending(c => c.Title),
                "releasedate" => sortOrder?.ToUpper() == "ASC" 
                    ? query.OrderBy(c => c.ReleaseDate) 
                    : query.OrderByDescending(c => c.ReleaseDate),
                "rating" => sortOrder?.ToUpper() == "ASC" 
                    ? query.OrderBy(c => c.Rating) 
                    : query.OrderByDescending(c => c.Rating),
                _ => sortOrder?.ToUpper() == "ASC" 
                    ? query.OrderBy(c => c.AddedAt) 
                    : query.OrderByDescending(c => c.AddedAt)
            };

            // Get total count for pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var movies = await query
                .Skip(offset)
                .Take(limit)
                .Select(c => new
                {
                    id = c.Id,
                    tmdbId = c.TmdbId,
                    title = c.Title,
                    overview = c.Overview,
                    year = c.ReleaseDate != null ? c.ReleaseDate.Value.Year : (int?)null,
                    posterUrl = !string.IsNullOrEmpty(c.PosterPath) 
                        ? (c.PosterPath.StartsWith("/") 
                            ? $"https://image.tmdb.org/t/p/w500{c.PosterPath}"
                            : (c.PosterPath.StartsWith("http") ? c.PosterPath : $"/api/image/{c.Id}/poster"))
                        : (!string.IsNullOrEmpty(c.FilePath) ? $"/api/image/{c.Id}/poster" : null),
                    backdropUrl = !string.IsNullOrEmpty(c.BackdropPath) 
                        ? (c.BackdropPath.StartsWith("/") 
                            ? $"https://image.tmdb.org/t/p/w1280{c.BackdropPath}"
                            : (c.BackdropPath.StartsWith("http") ? c.BackdropPath : $"/api/image/{c.Id}/backdrop"))
                        : (!string.IsNullOrEmpty(c.FilePath) ? $"/api/image/{c.Id}/backdrop" : null),
                    rating = c.Rating,
                    genres = c.Genres ?? new string[0],
                    filePath = c.FilePath,
                    addedAt = c.AddedAt,
                    type = "movie"
                })
                .ToListAsync(cancellationToken);

            return Ok(new
            {
                items = movies,
                total = totalCount,
                page = offset / limit + 1,
                pageSize = limit
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get library movies");
            return StatusCode(500, new { error = "Failed to get library movies", details = ex.Message });
        }
    }

    /// <summary>
    /// Get all TV series in library
    /// </summary>
    [HttpGet("series")]
    public async Task<IActionResult> GetSeries(
        [FromQuery] string? genre = null,
        [FromQuery] string? sortBy = "addedAt",
        [FromQuery] string? sortOrder = "DESC",
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting library series: Genre={Genre}, SortBy={SortBy}, Limit={Limit}", genre, sortBy, limit);

            var query = _context.Contents
                .AsNoTracking()
                .Where(c => c.Type == ContentType.Series)
                .AsQueryable();

            // Filter by genre if specified
            if (!string.IsNullOrEmpty(genre))
            {
                query = query.Where(c => c.Genres != null && c.Genres.Contains(genre));
            }

            // Apply sorting
            query = sortBy?.ToLower() switch
            {
                "title" => sortOrder?.ToUpper() == "ASC" 
                    ? query.OrderBy(c => c.Title) 
                    : query.OrderByDescending(c => c.Title),
                "releasedate" => sortOrder?.ToUpper() == "ASC" 
                    ? query.OrderBy(c => c.ReleaseDate) 
                    : query.OrderByDescending(c => c.ReleaseDate),
                "rating" => sortOrder?.ToUpper() == "ASC" 
                    ? query.OrderBy(c => c.Rating) 
                    : query.OrderByDescending(c => c.Rating),
                _ => sortOrder?.ToUpper() == "ASC" 
                    ? query.OrderBy(c => c.AddedAt) 
                    : query.OrderByDescending(c => c.AddedAt)
            };

            // Get total count for pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var series = await query
                .Skip(offset)
                .Take(limit)
                .Select(c => new
                {
                    id = c.Id,
                    tmdbId = c.TmdbId,
                    title = c.Title,
                    overview = c.Overview,
                    year = c.ReleaseDate != null ? c.ReleaseDate.Value.Year : (int?)null,
                    posterUrl = !string.IsNullOrEmpty(c.PosterPath) 
                        ? (c.PosterPath.StartsWith("/") 
                            ? $"https://image.tmdb.org/t/p/w500{c.PosterPath}"
                            : (c.PosterPath.StartsWith("http") ? c.PosterPath : $"/api/image/{c.Id}/poster"))
                        : (!string.IsNullOrEmpty(c.FilePath) ? $"/api/image/{c.Id}/poster" : null),
                    backdropUrl = !string.IsNullOrEmpty(c.BackdropPath) 
                        ? (c.BackdropPath.StartsWith("/") 
                            ? $"https://image.tmdb.org/t/p/w1280{c.BackdropPath}"
                            : (c.BackdropPath.StartsWith("http") ? c.BackdropPath : $"/api/image/{c.Id}/backdrop"))
                        : (!string.IsNullOrEmpty(c.FilePath) ? $"/api/image/{c.Id}/backdrop" : null),
                    rating = c.Rating,
                    genres = c.Genres ?? new string[0],
                    filePath = c.FilePath,
                    addedAt = c.AddedAt,
                    type = "series"
                })
                .ToListAsync(cancellationToken);

            return Ok(new
            {
                items = series,
                total = totalCount,
                page = offset / limit + 1,
                pageSize = limit
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get library series");
            return StatusCode(500, new { error = "Failed to get library series", details = ex.Message });
        }
    }

    /// <summary>
    /// Search library content
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchLibrary(
        [FromQuery] string q,
        [FromQuery] string? type = null,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Searching library: Query={Query}, Type={Type}, Limit={Limit}", q, type, limit);

            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest(new { error = "Search query is required" });
            }

            var query = _context.Contents.AsNoTracking().AsQueryable();

            // Filter by type if specified
            if (!string.IsNullOrEmpty(type))
            {
                if (type.ToLower() == "movie")
                {
                    query = query.Where(c => c.Type == ContentType.Movie);
                }
                else if (type.ToLower() == "series" || type.ToLower() == "tv")
                {
                    query = query.Where(c => c.Type == ContentType.Series);
                }
            }

            // Search in title and overview
            query = query.Where(c => 
                c.Title.Contains(q) || 
                (c.Overview != null && c.Overview.Contains(q)));

            // Apply limit and get results (ordered by relevance/added date)
            var results = await query
                .OrderByDescending(c => c.AddedAt)
                .Take(limit)
                .Select(c => new
                {
                    id = c.Id,
                    tmdbId = c.TmdbId,
                    title = c.Title,
                    overview = c.Overview,
                    year = c.ReleaseDate != null ? c.ReleaseDate.Value.Year : (int?)null,
                    posterUrl = !string.IsNullOrEmpty(c.PosterPath) 
                        ? (c.PosterPath.StartsWith("/") 
                            ? $"https://image.tmdb.org/t/p/w500{c.PosterPath}"
                            : (c.PosterPath.StartsWith("http") ? c.PosterPath : $"/api/image/{c.Id}/poster"))
                        : (!string.IsNullOrEmpty(c.FilePath) ? $"/api/image/{c.Id}/poster" : null),
                    backdropUrl = !string.IsNullOrEmpty(c.BackdropPath) 
                        ? (c.BackdropPath.StartsWith("/") 
                            ? $"https://image.tmdb.org/t/p/w1280{c.BackdropPath}"
                            : (c.BackdropPath.StartsWith("http") ? c.BackdropPath : $"/api/image/{c.Id}/backdrop"))
                        : (!string.IsNullOrEmpty(c.FilePath) ? $"/api/image/{c.Id}/backdrop" : null),
                    rating = c.Rating,
                    genres = c.Genres ?? new string[0],
                    filePath = c.FilePath,
                    addedAt = c.AddedAt,
                    type = c.Type == ContentType.Movie ? "movie" : "series"
                })
                .ToListAsync(cancellationToken);

            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search library");
            return StatusCode(500, new { error = "Failed to search library", details = ex.Message });
        }
    }

    /// <summary>
    /// Get recently added content
    /// </summary>
    [HttpGet("recent")]
    public async Task<IActionResult> GetRecentlyAdded(
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting recently added content: Limit={Limit}", limit);

            var recentContent = await _context.Contents
                .AsNoTracking()
                .OrderByDescending(c => c.AddedAt)
                .Take(limit)
                .Select(c => new
                {
                    id = c.Id,
                    tmdbId = c.TmdbId,
                    title = c.Title,
                    overview = c.Overview,
                    year = c.ReleaseDate != null ? c.ReleaseDate.Value.Year : (int?)null,
                    posterUrl = !string.IsNullOrEmpty(c.PosterPath) 
                        ? (c.PosterPath.StartsWith("/") 
                            ? $"https://image.tmdb.org/t/p/w500{c.PosterPath}"
                            : (c.PosterPath.StartsWith("http") ? c.PosterPath : $"/api/image/{c.Id}/poster"))
                        : (!string.IsNullOrEmpty(c.FilePath) ? $"/api/image/{c.Id}/poster" : null),
                    backdropUrl = !string.IsNullOrEmpty(c.BackdropPath) 
                        ? (c.BackdropPath.StartsWith("/") 
                            ? $"https://image.tmdb.org/t/p/w1280{c.BackdropPath}"
                            : (c.BackdropPath.StartsWith("http") ? c.BackdropPath : $"/api/image/{c.Id}/backdrop"))
                        : (!string.IsNullOrEmpty(c.FilePath) ? $"/api/image/{c.Id}/backdrop" : null),
                    rating = c.Rating,
                    genres = c.Genres ?? new string[0],
                    filePath = c.FilePath,
                    addedAt = c.AddedAt,
                    type = c.Type == ContentType.Movie ? "movie" : "series"
                })
                .ToListAsync(cancellationToken);

            return Ok(new { items = recentContent });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recently added content");
            return StatusCode(500, new { error = "Failed to get recently added content", details = ex.Message });
        }
    }

    /// <summary>
    /// Get specific library item details with metadata
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetLibraryItem(
        [FromRoute] int id,
        [FromQuery] int? profileId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting library item: Id={Id}, ProfileId={ProfileId}", id, profileId);

            var content = await _context.Contents
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

            if (content == null)
            {
                return NotFound(new { error = "Content not found" });
            }

            // Load additional metadata from metadata.json if available
            var enhancedMetadata = await LoadMetadataFromFile(content.FilePath);

            // For series, fetch episodes once and reuse for seasons calculation
            object[]? episodes = null;
            object[]? seasons = null;
            if (content.Type == ContentType.Series)
            {
                episodes = await GetSeriesEpisodes(content.Id, cancellationToken);
                seasons = GetSeasonsFromEpisodes(episodes);
            }

            // Get watch progress if profileId is provided
            object? watchProgress = null;
            if (profileId.HasValue)
            {
                var history = await _context.WatchHistories
                    .AsNoTracking()
                    .Where(wh => wh.ProfileId == profileId.Value && wh.ContentId == id && wh.EpisodeId == null)
                    .OrderByDescending(wh => wh.LastWatchedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                if (history != null)
                {
                    // Convert ticks to seconds (1 tick = 100 nanoseconds, so 10,000,000 ticks = 1 second)
                    var progressSeconds = (int)(history.PositionTicks / 10_000_000);
                    var durationSeconds = enhancedMetadata?.Runtime != null ? enhancedMetadata.Runtime * 60 : null;

                    watchProgress = new
                    {
                        progressSeconds = progressSeconds,
                        durationSeconds = durationSeconds,
                        watchedPercentage = history.WatchedPercentage,
                        completed = history.IsCompleted,
                        lastWatchedAt = history.LastWatchedAt
                    };
                }
            }

            var result = new
            {
                id = content.Id,
                tmdbId = content.TmdbId,
                title = content.Title,
                overview = content.Overview,
                
                // Use metadata.json data if available, fallback to database
                releaseDate = enhancedMetadata?.ReleaseDate ?? content.ReleaseDate?.ToString("yyyy-MM-dd"),
                year = enhancedMetadata?.Year ?? content.ReleaseDate?.Year,
                voteAverage = enhancedMetadata?.VoteAverage ?? content.Rating,
                runtime = enhancedMetadata?.Runtime,
                
                posterUrl = !string.IsNullOrEmpty(content.PosterPath) 
                    ? (content.PosterPath.StartsWith("/") 
                        ? $"https://image.tmdb.org/t/p/w500{content.PosterPath}"
                        : (content.PosterPath.StartsWith("http") ? content.PosterPath : $"/api/image/{content.Id}/poster"))
                    : (!string.IsNullOrEmpty(content.FilePath) ? $"/api/image/{content.Id}/poster" : null),
                backdropUrl = !string.IsNullOrEmpty(content.BackdropPath) 
                    ? (content.BackdropPath.StartsWith("/") 
                        ? $"https://image.tmdb.org/t/p/w1280{content.BackdropPath}"
                        : (content.BackdropPath.StartsWith("http") ? content.BackdropPath : $"/api/image/{content.Id}/backdrop"))
                    : (!string.IsNullOrEmpty(content.FilePath) ? $"/api/image/{content.Id}/backdrop" : null),
                rating = enhancedMetadata?.VoteAverage ?? content.Rating,
                genres = enhancedMetadata?.Genres ?? content.Genres ?? new string[0],
                filePath = content.FilePath,
                addedAt = content.AddedAt,
                type = content.Type == ContentType.Movie ? "movie" : "series",
                
                // Additional metadata fields
                tagline = enhancedMetadata?.Tagline,
                status = enhancedMetadata?.Status,
                originalLanguage = enhancedMetadata?.OriginalLanguage,
                productionCompanies = enhancedMetadata?.ProductionCompanies,
                
                // For series, include episode and season information
                episodes = episodes,
                seasons = seasons,
                numberOfSeasons = seasons?.Length,
                numberOfEpisodes = episodes?.Length,

                // Watch progress for this profile
                watchProgress = watchProgress
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get library item: {Id}", id);
            return StatusCode(500, new { error = "Failed to get library item", details = ex.Message });
        }
    }

    /// <summary>
    /// Load enhanced metadata from metadata.json file
    /// </summary>
    private async Task<EnhancedMetadata?> LoadMetadataFromFile(string filePath)
    {
        try
        {
            var mediaFolderPath = Directory.Exists(filePath) 
                ? filePath 
                : Path.GetDirectoryName(filePath);

            if (string.IsNullOrEmpty(mediaFolderPath))
            {
                return null;
            }

            var metadataPath = Path.Combine(mediaFolderPath, "metadata.json");
            if (!System.IO.File.Exists(metadataPath))
            {
                _logger.LogDebug("No metadata.json found at: {MetadataPath}", metadataPath);
                return null;
            }

            var metadataJson = await System.IO.File.ReadAllTextAsync(metadataPath);
            var metadata = System.Text.Json.JsonSerializer.Deserialize<EnhancedMetadata>(metadataJson, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger.LogDebug("Loaded metadata from: {MetadataPath}", metadataPath);
            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load metadata from file: {FilePath}", filePath);
            return null;
        }
    }

    /// <summary>
    /// Get episodes for a series with complete TMDB metadata
    /// </summary>
    private async Task<object[]> GetSeriesEpisodes(int seriesId, CancellationToken cancellationToken)
    {
        try
        {
            // Get the series to get its TMDB ID
            var series = await _context.Contents
                .FirstOrDefaultAsync(c => c.Id == seriesId && c.Type == ContentType.Series, cancellationToken);

            if (series == null)
            {
                return new object[0];
            }

            // Get episodes from database (these are the ones we have files for or metadata stored)
            var dbEpisodes = await _context.Episodes
                .Where(e => e.ContentId == seriesId)
                .OrderBy(e => e.SeasonNumber)
                .ThenBy(e => e.EpisodeNumber)
                .ToListAsync(cancellationToken);

            // Create a map of existing episodes for quick lookup
            var dbEpisodeMap = dbEpisodes.ToDictionary(e => $"{e.SeasonNumber}x{e.EpisodeNumber}", e => e);

            // Always fetch complete episode metadata from TMDB (like old backend)
            var allEpisodes = new List<object>();

            try
            {
                // Get TMDB client from DI
                var tmdbClient = HttpContext.RequestServices.GetRequiredService<ITmdbClient>();
                var tvDetails = await tmdbClient.GetTvSeriesDetailsAsync(series.TmdbId, cancellationToken);

                foreach (var season in tvDetails.Seasons.Where(s => s.SeasonNumber > 0)) // Skip specials
                {
                    try
                    {
                        var seasonDetails = await tmdbClient.GetSeasonDetailsAsync(series.TmdbId, season.SeasonNumber, cancellationToken);

                        foreach (var episode in seasonDetails.Episodes)
                        {
                            var episodeKey = $"{episode.SeasonNumber}x{episode.EpisodeNumber}";
                            var dbEpisode = dbEpisodeMap.GetValueOrDefault(episodeKey);

                            var episodeData = new
                            {
                                id = dbEpisode?.Id ?? 0,
                                tmdbId = episode.Id,
                                seasonNumber = episode.SeasonNumber,
                                episodeNumber = episode.EpisodeNumber,
                                title = episode.Name,
                                overview = episode.Overview,
                                airDate = episode.AirDate,
                                stillPath = episode.StillPath,
                                stillUrl = !string.IsNullOrEmpty(episode.StillPath) 
                                    ? (episode.StillPath.StartsWith("/")
                                        ? $"https://image.tmdb.org/t/p/w300{episode.StillPath}"
                                        : (episode.StillPath.StartsWith("http") ? episode.StillPath : $"/api/image/{seriesId}/season/{episode.SeasonNumber}/episode/{episode.EpisodeNumber}/still"))
                                    : (!string.IsNullOrEmpty(dbEpisode?.FilePath) ? $"/api/image/{seriesId}/season/{episode.SeasonNumber}/episode/{episode.EpisodeNumber}/still" : null),
                                filePath = dbEpisode?.FilePath,
                                hasFile = !string.IsNullOrEmpty(dbEpisode?.FilePath),
                                available = !string.IsNullOrEmpty(dbEpisode?.FilePath), // Episode is available if we have a file
                                watched = false, // TODO: Add watch progress if profileId is provided
                                addedAt = dbEpisode?.AddedAt
                            };

                            allEpisodes.Add(episodeData);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to fetch season {Season} details for series {TmdbId}", season.SeasonNumber, series.TmdbId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch TMDB metadata for series {TmdbId}, falling back to database episodes only", series.TmdbId);
                
                // Fallback to database episodes only if TMDB fetch fails
                allEpisodes = dbEpisodes.Select(e => new
                {
                    id = e.Id,
                    tmdbId = e.TmdbId,
                    seasonNumber = e.SeasonNumber,
                    episodeNumber = e.EpisodeNumber,
                    title = e.Title,
                    overview = e.Overview,
                    airDate = e.AirDate,
                    stillPath = e.StillPath,
                    stillUrl = !string.IsNullOrEmpty(e.StillPath) 
                        ? (e.StillPath.StartsWith("/")
                            ? $"https://image.tmdb.org/t/p/w300{e.StillPath}"
                            : (e.StillPath.StartsWith("http") ? e.StillPath : $"/api/image/{seriesId}/season/{e.SeasonNumber}/episode/{e.EpisodeNumber}/still"))
                        : (!string.IsNullOrEmpty(e.FilePath) ? $"/api/image/{seriesId}/season/{e.SeasonNumber}/episode/{e.EpisodeNumber}/still" : null),
                    filePath = e.FilePath,
                    hasFile = !string.IsNullOrEmpty(e.FilePath),
                    available = !string.IsNullOrEmpty(e.FilePath),
                    watched = false,
                    addedAt = e.AddedAt
                }).Cast<object>().ToList();
            }

            return allEpisodes.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get episodes for series: {SeriesId}", seriesId);
            return new object[0];
        }
    }

    /// <summary>
    /// Get seasons for a series with episode counts
    /// </summary>
    private async Task<object[]> GetSeriesSeasons(int seriesId, CancellationToken cancellationToken)
    {
        try
        {
            // Get all episodes for the series
            var episodes = await GetSeriesEpisodes(seriesId, cancellationToken);
            return GetSeasonsFromEpisodes(episodes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get seasons for series: {SeriesId}", seriesId);
            return new object[0];
        }
    }

    /// <summary>
    /// Helper method to create seasons from episodes array (avoids duplicate TMDB calls)
    /// </summary>
    private object[] GetSeasonsFromEpisodes(object[] episodes)
    {
        try
        {
            // Group episodes by season
            var seasons = episodes
                .Cast<dynamic>()
                .GroupBy(e => (int)e.seasonNumber)
                .Select(g => new
                {
                    seasonNumber = g.Key,
                    episodeCount = g.Count(),
                    episodes = g.ToList()
                })
                .OrderBy(s => s.seasonNumber)
                .Cast<object>()
                .ToArray();

            return seasons;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to group episodes into seasons");
            return new object[0];
        }
    }

    /// <summary>
    /// Debug endpoint to check episode file paths
    /// </summary>
    [HttpGet("debug/episodes/{seriesId}")]
    public async Task<IActionResult> DebugEpisodes(
        [FromRoute] int seriesId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var episodes = await _context.Episodes
                .Where(e => e.ContentId == seriesId)
                .Select(e => new
                {
                    e.Id,
                    e.SeasonNumber,
                    e.EpisodeNumber,
                    e.Title,
                    e.FilePath,
                    FileExists = !string.IsNullOrEmpty(e.FilePath) && System.IO.File.Exists(e.FilePath),
                    IsDirectory = !string.IsNullOrEmpty(e.FilePath) && Directory.Exists(e.FilePath)
                })
                .ToListAsync(cancellationToken);

            return Ok(new { seriesId, episodes });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to debug episodes for series: {SeriesId}", seriesId);
            return StatusCode(500, new { error = "Failed to debug episodes", details = ex.Message });
        }
    }

    /// <summary>
    /// Remove item from library
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> RemoveFromLibrary(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Removing item from library: Id={Id}", id);

            var content = await _context.Contents
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

            if (content == null)
            {
                return NotFound(new { error = "Content not found" });
            }

            // Remove related records
            var watchHistories = await _context.WatchHistories
                .Where(w => w.ContentId == id)
                .ToListAsync(cancellationToken);
            _context.WatchHistories.RemoveRange(watchHistories);

            // Remove the content
            _context.Contents.Remove(content);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Removed content from library: {Title} (Id={Id})", content.Title, id);
            return Ok(new { message = "Content removed from library" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove content from library: {Id}", id);
            return StatusCode(500, new { error = "Failed to remove content from library", details = ex.Message });
        }
    }
}

/// <summary>
/// Enhanced metadata structure from metadata.json files
/// </summary>
public class EnhancedMetadata
{
    public string? Title { get; set; }
    public string? Overview { get; set; }
    public string? ReleaseDate { get; set; }
    public int? Year { get; set; }
    public double? VoteAverage { get; set; }
    public int? Runtime { get; set; }
    public string[]? Genres { get; set; }
    public string? Tagline { get; set; }
    public string? Status { get; set; }
    public string? OriginalLanguage { get; set; }
    public string[]? ProductionCompanies { get; set; }
    public string? PosterPath { get; set; }
    public string? BackdropPath { get; set; }
}