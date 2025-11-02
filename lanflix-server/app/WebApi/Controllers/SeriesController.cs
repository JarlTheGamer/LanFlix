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

            // TODO: Add episodes and watch progress if profileId is provided
            // var episodes = await GetSeriesEpisodes(id);
            // var watchProgress = profileId.HasValue ? await GetWatchProgress(id, profileId.Value) : null;

            return Ok(series);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get series: {Id}", id);
            return StatusCode(500, new { error = "Failed to get series", details = ex.Message });
        }
    }
}