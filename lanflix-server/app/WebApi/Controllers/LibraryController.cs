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
                    posterUrl = !string.IsNullOrEmpty(c.PosterPath) ? $"https://image.tmdb.org/t/p/w500{c.PosterPath}" : null,
                    backdropUrl = !string.IsNullOrEmpty(c.BackdropPath) ? $"https://image.tmdb.org/t/p/w1280{c.BackdropPath}" : null,
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
                    posterUrl = !string.IsNullOrEmpty(c.PosterPath) ? $"https://image.tmdb.org/t/p/w500{c.PosterPath}" : null,
                    backdropUrl = !string.IsNullOrEmpty(c.BackdropPath) ? $"https://image.tmdb.org/t/p/w1280{c.BackdropPath}" : null,
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
                .OrderByDescending(c => c.AddedAt)
                .Take(limit)
                .Select(c => new
                {
                    id = c.Id,
                    tmdbId = c.TmdbId,
                    title = c.Title,
                    overview = c.Overview,
                    year = c.ReleaseDate != null ? c.ReleaseDate.Value.Year : (int?)null,
                    posterUrl = !string.IsNullOrEmpty(c.PosterPath) ? $"https://image.tmdb.org/t/p/w500{c.PosterPath}" : null,
                    backdropUrl = !string.IsNullOrEmpty(c.BackdropPath) ? $"https://image.tmdb.org/t/p/w1280{c.BackdropPath}" : null,
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
    /// Get specific library item details
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
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

            if (content == null)
            {
                return NotFound(new { error = "Content not found" });
            }

            var result = new
            {
                id = content.Id,
                tmdbId = content.TmdbId,
                title = content.Title,
                overview = content.Overview,
                year = content.ReleaseDate?.Year,
                posterUrl = !string.IsNullOrEmpty(content.PosterPath) ? $"https://image.tmdb.org/t/p/w500{content.PosterPath}" : null,
                backdropUrl = !string.IsNullOrEmpty(content.BackdropPath) ? $"https://image.tmdb.org/t/p/w1280{content.BackdropPath}" : null,
                rating = content.Rating,
                genres = content.Genres ?? new string[0],
                filePath = content.FilePath,
                addedAt = content.AddedAt,
                type = content.Type == ContentType.Movie ? "movie" : "series"
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