using Lanflix.Application.Common.Interfaces;
using Lanflix.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SeriesController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<SeriesController> _logger;

    public SeriesController(
        IApplicationDbContext context,
        ILogger<SeriesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all TV series in library
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSeries(
        [FromQuery] string? genre = null,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = "addedAt",
        [FromQuery] string? sortOrder = "DESC",
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        [FromQuery] int? profileId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting series: Genre={Genre}, Search={Search}, SortBy={SortBy}, Limit={Limit}", 
                genre, search, sortBy, limit);

            var query = _context.Contents
                .Where(c => c.Type == ContentType.Series)
                .AsQueryable();

            // Filter by genre if specified
            if (!string.IsNullOrEmpty(genre))
            {
                query = query.Where(c => c.Genres != null && c.Genres.Contains(genre));
            }

            // Filter by search term if specified
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => 
                    c.Title.Contains(search) || 
                    (c.Overview != null && c.Overview.Contains(search)));
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
                    type = "series",
                    title = c.Title,
                    originalTitle = c.OriginalTitle ?? c.Title,
                    overview = c.Overview,
                    releaseDate = c.ReleaseDate,
                    year = c.ReleaseDate != null ? c.ReleaseDate.Value.Year : (int?)null,
                    posterUrl = !string.IsNullOrEmpty(c.PosterPath) ? $"https://image.tmdb.org/t/p/w500{c.PosterPath}" : null,
                    backdropUrl = !string.IsNullOrEmpty(c.BackdropPath) ? $"https://image.tmdb.org/t/p/w1280{c.BackdropPath}" : null,
                    voteAverage = c.Rating,
                    voteCount = 0, // TODO: Add VoteCount field to Content entity
                    genres = c.Genres ?? new string[0],
                    runtime = c.MediaInfo != null ? (int?)c.MediaInfo.Duration.TotalMinutes : null,
                    status = "Ended", // TODO: Add Status field to Content entity
                    filePath = c.FilePath,
                    addedAt = c.AddedAt
                })
                .ToListAsync(cancellationToken);

            return Ok(new
            {
                items = series,
                total = totalCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get series");
            return StatusCode(500, new { error = "Failed to get series", details = ex.Message });
        }
    }

    /// <summary>
    /// Get specific series details
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetSeries(
        [FromRoute] int id,
        [FromQuery] int? profileId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting series: Id={Id}, ProfileId={ProfileId}", id, profileId);

            var series = await _context.Contents
                .Where(c => c.Id == id && c.Type == ContentType.Series)
                .Select(c => new
                {
                    id = c.Id,
                    tmdbId = c.TmdbId,
                    type = "series",
                    title = c.Title,
                    originalTitle = c.OriginalTitle ?? c.Title,
                    overview = c.Overview,
                    releaseDate = c.ReleaseDate,
                    year = c.ReleaseDate != null ? c.ReleaseDate.Value.Year : (int?)null,
                    posterUrl = !string.IsNullOrEmpty(c.PosterPath) ? $"https://image.tmdb.org/t/p/w500{c.PosterPath}" : null,
                    backdropUrl = !string.IsNullOrEmpty(c.BackdropPath) ? $"https://image.tmdb.org/t/p/w1280{c.BackdropPath}" : null,
                    voteAverage = c.Rating,
                    voteCount = 0, // TODO: Add VoteCount field
                    genres = c.Genres ?? new string[0],
                    runtime = c.MediaInfo != null ? (int?)c.MediaInfo.Duration.TotalMinutes : null,
                    status = "Ended", // TODO: Add Status field
                    filePath = c.FilePath,
                    addedAt = c.AddedAt
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (series == null)
            {
                return NotFound(new { error = "Series not found" });
            }

            // Get episodes from database (these are the ones we have files for or metadata stored)
            var dbEpisodes = await _context.Episodes
                .Where(e => e.ContentId == id)
                .OrderBy(e => e.SeasonNumber)
                .ThenBy(e => e.EpisodeNumber)
                .ToListAsync(cancellationToken);

            // Create a map of existing episodes for quick lookup
            var dbEpisodeMap = dbEpisodes.ToDictionary(e => $"{e.SeasonNumber}x{e.EpisodeNumber}", e => e);

            // Always fetch complete episode metadata from TMDB (like old backend)
            // This ensures we show all episodes, not just the ones we have files for
            var allEpisodes = new List<object>();
            var seasonsList = new List<object>();

            try
            {
                // Get TMDB client from DI
                var tmdbClient = HttpContext.RequestServices.GetRequiredService<ITmdbClient>();
                var tvDetails = await tmdbClient.GetTvSeriesDetailsAsync(series.tmdbId, cancellationToken);

                foreach (var season in tvDetails.Seasons.Where(s => s.SeasonNumber > 0)) // Skip specials
                {
                    try
                    {
                        var seasonDetails = await tmdbClient.GetSeasonDetailsAsync(series.tmdbId, season.SeasonNumber, cancellationToken);
                        var seasonEpisodes = new List<object>();

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
                                stillUrl = !string.IsNullOrEmpty(episode.StillPath) ? $"https://image.tmdb.org/t/p/w300{episode.StillPath}" : null,
                                filePath = dbEpisode?.FilePath,
                                hasFile = !string.IsNullOrEmpty(dbEpisode?.FilePath) && System.IO.File.Exists(dbEpisode.FilePath),
                                available = !string.IsNullOrEmpty(dbEpisode?.FilePath), // Episode is available if we have a file
                                addedAt = dbEpisode?.AddedAt
                            };

                            seasonEpisodes.Add(episodeData);
                            allEpisodes.Add(episodeData);
                        }

                        seasonsList.Add(new
                        {
                            seasonNumber = season.SeasonNumber,
                            episodeCount = seasonEpisodes.Count,
                            episodes = seasonEpisodes
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to fetch season {Season} details for series {TmdbId}", season.SeasonNumber, series.tmdbId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch TMDB metadata for series {TmdbId}, falling back to database episodes only", series.tmdbId);
                
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
                    stillUrl = !string.IsNullOrEmpty(e.StillPath) ? $"https://image.tmdb.org/t/p/w300{e.StillPath}" : null,
                    filePath = e.FilePath,
                    hasFile = !string.IsNullOrEmpty(e.FilePath) && System.IO.File.Exists(e.FilePath),
                    available = !string.IsNullOrEmpty(e.FilePath),
                    addedAt = e.AddedAt
                }).Cast<object>().ToList();

                seasonsList = allEpisodes
                    .Cast<dynamic>()
                    .GroupBy(e => e.seasonNumber)
                    .Select(g => new
                    {
                        seasonNumber = g.Key,
                        episodeCount = g.Count(),
                        episodes = g.ToList()
                    })
                    .Cast<object>()
                    .ToList();
            }

            var result = new
            {
                series.id,
                series.tmdbId,
                series.type,
                series.title,
                series.originalTitle,
                series.overview,
                series.releaseDate,
                series.year,
                series.posterUrl,
                series.backdropUrl,
                series.voteAverage,
                series.voteCount,
                series.genres,
                series.runtime,
                series.status,
                series.filePath,
                series.addedAt,
                seasons = seasonsList,
                episodes = allEpisodes, // Include all episodes for compatibility
                numberOfSeasons = seasonsList.Count,
                numberOfEpisodes = allEpisodes.Count,
                totalEpisodes = allEpisodes.Count,
                availableEpisodes = allEpisodes.Count(e => ((dynamic)e).hasFile == true)
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get series: {Id}", id);
            return StatusCode(500, new { error = "Failed to get series", details = ex.Message });
        }
    }

    /// <summary>
    /// Get episodes for a specific season of a series
    /// </summary>
    [HttpGet("{id}/seasons/{seasonNumber}/episodes")]
    public async Task<IActionResult> GetSeasonEpisodes(
        [FromRoute] int id,
        [FromRoute] int seasonNumber,
        [FromQuery] int? profileId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting season {Season} episodes for series: Id={Id}, ProfileId={ProfileId}", 
                seasonNumber, id, profileId);

            // Verify series exists
            var series = await _context.Contents
                .Where(c => c.Id == id && c.Type == ContentType.Series)
                .FirstOrDefaultAsync(cancellationToken);

            if (series == null)
            {
                return NotFound(new { error = "Series not found" });
            }

            // Get episodes for the season
            var episodes = await _context.Episodes
                .Where(e => e.ContentId == id && e.SeasonNumber == seasonNumber)
                .OrderBy(e => e.EpisodeNumber)
                .Select(e => new
                {
                    id = e.Id,
                    tmdbId = e.TmdbId,
                    seasonNumber = e.SeasonNumber,
                    episodeNumber = e.EpisodeNumber,
                    title = e.Title,
                    overview = e.Overview,
                    airDate = e.AirDate,
                    stillPath = e.StillPath,
                    stillUrl = !string.IsNullOrEmpty(e.StillPath) ? $"https://image.tmdb.org/t/p/w300{e.StillPath}" : null,
                    filePath = e.FilePath,
                    hasFile = !string.IsNullOrEmpty(e.FilePath) && System.IO.File.Exists(e.FilePath),
                    addedAt = e.AddedAt,
                    // TODO: Add watch progress if profileId is provided
                    watchProgress = (object?)null
                })
                .ToListAsync(cancellationToken);

            return Ok(new
            {
                seriesId = id,
                seasonNumber = seasonNumber,
                episodes = episodes,
                totalEpisodes = episodes.Count,
                availableEpisodes = episodes.Count(e => e.hasFile)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get season episodes: SeriesId={Id}, Season={Season}", id, seasonNumber);
            return StatusCode(500, new { error = "Failed to get season episodes", details = ex.Message });
        }
    }

    /// <summary>
    /// Get all seasons for a series with episode counts
    /// </summary>
    [HttpGet("{id}/seasons")]
    public async Task<IActionResult> GetSeriesSeasons(
        [FromRoute] int id,
        [FromQuery] int? profileId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting seasons for series: Id={Id}, ProfileId={ProfileId}", id, profileId);

            // Verify series exists
            var series = await _context.Contents
                .Where(c => c.Id == id && c.Type == ContentType.Series)
                .FirstOrDefaultAsync(cancellationToken);

            if (series == null)
            {
                return NotFound(new { error = "Series not found" });
            }

            // Get episodes grouped by season
            var seasons = await _context.Episodes
                .Where(e => e.ContentId == id)
                .GroupBy(e => e.SeasonNumber)
                .Select(g => new
                {
                    seasonNumber = g.Key,
                    episodeCount = g.Count(),
                    availableEpisodes = g.Count(e => !string.IsNullOrEmpty(e.FilePath) && System.IO.File.Exists(e.FilePath)),
                    firstEpisode = g.OrderBy(e => e.EpisodeNumber).Select(e => new
                    {
                        title = e.Title,
                        airDate = e.AirDate,
                        stillUrl = !string.IsNullOrEmpty(e.StillPath) ? $"https://image.tmdb.org/t/p/w300{e.StillPath}" : null
                    }).FirstOrDefault()
                })
                .OrderBy(s => s.seasonNumber)
                .ToListAsync(cancellationToken);

            return Ok(new
            {
                seriesId = id,
                seriesTitle = series.Title,
                seasons = seasons,
                totalSeasons = seasons.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get series seasons: SeriesId={Id}", id);
            return StatusCode(500, new { error = "Failed to get series seasons", details = ex.Message });
        }
    }
}