using Lanflix.Application.Common.Interfaces;
using Lanflix.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace Lanflix.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StreamController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<StreamController> _logger;

    public StreamController(
        IApplicationDbContext context,
        ILogger<StreamController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get direct video URL for a movie
    /// </summary>
    [HttpGet("movie/{id}/url")]
    public async Task<IActionResult> GetMovieStreamUrl(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting stream URL for movie: Id={Id}", id);

            var movie = await _context.Contents
                .Where(c => c.Id == id && c.Type == ContentType.Movie)
                .FirstOrDefaultAsync(cancellationToken);

            if (movie == null)
            {
                return NotFound(new { error = "Movie not found" });
            }

            if (string.IsNullOrEmpty(movie.FilePath) || !System.IO.File.Exists(movie.FilePath))
            {
                return NotFound(new { error = "Movie file not found" });
            }

            // Generate direct .mp4 URL that looks like a real file
            var fileName = Path.GetFileNameWithoutExtension(movie.FilePath);
            var extension = Path.GetExtension(movie.FilePath);
            var safeFileName = SanitizeFileName(fileName);
            var mp4Url = $"{Request.Scheme}://{Request.Host}/videos/movies/{safeFileName}-{id}{extension}";
            
            return Ok(new
            {
                id = movie.Id,
                title = movie.Title,
                type = "movie",
                mp4Url = mp4Url,
                directUrl = mp4Url,
                streamUrl = mp4Url, // For compatibility
                filePath = movie.FilePath,
                fileName = Path.GetFileName(movie.FilePath),
                fileSize = new FileInfo(movie.FilePath).Length,
                mimeType = GetMimeType(movie.FilePath)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get movie stream URL: {Id}", id);
            return StatusCode(500, new { error = "Failed to get stream URL", details = ex.Message });
        }
    }

    /// <summary>
    /// Get direct video URL and markers for a TV episode
    /// </summary>
    [HttpGet("episode/{id}")]
    [HttpGet("episode/{id}/url")]
    public async Task<IActionResult> GetEpisodeStreamUrl(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting stream URL for episode: Id={Id}", id);

            var episode = await _context.Episodes
                .Include(e => e.Content)
                .Where(e => e.Id == id)
                .FirstOrDefaultAsync(cancellationToken);

            if (episode == null)
            {
                return NotFound(new { error = "Episode not found" });
            }

            if (string.IsNullOrEmpty(episode.FilePath) || !System.IO.File.Exists(episode.FilePath))
            {
                return NotFound(new { error = "Episode file not found" });
            }

            // Generate direct .mp4 URL that looks like a real file
            var seriesName = SanitizeFileName(episode.Content?.Title ?? "Unknown");
            var episodeTitle = SanitizeFileName(episode.Title ?? $"S{episode.SeasonNumber:D2}E{episode.EpisodeNumber:D2}");
            var extension = Path.GetExtension(episode.FilePath);
            var mp4Url = $"{Request.Scheme}://{Request.Host}/videos/series/{seriesName}/S{episode.SeasonNumber:D2}E{episode.EpisodeNumber:D2}-{episodeTitle}-{id}{extension}";
            
            return Ok(new
            {
                id = episode.Id,
                title = episode.Title,
                type = "episode",
                seasonNumber = episode.SeasonNumber,
                episodeNumber = episode.EpisodeNumber,
                mp4Url = mp4Url,
                directUrl = mp4Url,
                streamUrl = mp4Url, // For compatibility
                filePath = episode.FilePath,
                fileName = Path.GetFileName(episode.FilePath),
                fileSize = new FileInfo(episode.FilePath).Length,
                mimeType = GetMimeType(episode.FilePath),
                introStartTime = episode.IntroStartTime,
                introEndTime = episode.IntroEndTime,
                creditsStartTime = episode.CreditsStartTime
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get episode stream URL: {Id}", id);
            return StatusCode(500, new { error = "Failed to get stream URL", details = ex.Message });
        }
    }

    /// <summary>
    /// Stream movie file directly
    /// </summary>
    [HttpGet("movie/{id}/file")]
    public async Task<IActionResult> StreamMovie(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var movie = await _context.Contents
                .Where(c => c.Id == id && c.Type == ContentType.Movie)
                .FirstOrDefaultAsync(cancellationToken);

            if (movie == null || string.IsNullOrEmpty(movie.FilePath) || !System.IO.File.Exists(movie.FilePath))
            {
                return NotFound();
            }

            var fileInfo = new FileInfo(movie.FilePath);
            var mimeType = GetMimeType(movie.FilePath);
            
            // Support range requests for video streaming
            return PhysicalFile(movie.FilePath, mimeType, enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stream movie: {Id}", id);
            return StatusCode(500);
        }
    }

    /// <summary>
    /// Stream episode file directly
    /// </summary>
    [HttpGet("episode/{id}/file")]
    public async Task<IActionResult> StreamEpisode(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var episode = await _context.Episodes
                .Where(e => e.Id == id)
                .FirstOrDefaultAsync(cancellationToken);

            if (episode == null || string.IsNullOrEmpty(episode.FilePath) || !System.IO.File.Exists(episode.FilePath))
            {
                return NotFound();
            }

            var fileInfo = new FileInfo(episode.FilePath);
            var mimeType = GetMimeType(episode.FilePath);
            
            // Support range requests for video streaming
            return PhysicalFile(episode.FilePath, mimeType, enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stream episode: {Id}", id);
            return StatusCode(500);
        }
    }

    /// <summary>
    /// Get all available video URLs for external players
    /// </summary>
    [HttpGet("urls")]
    public async Task<IActionResult> GetAllStreamUrls(
        [FromQuery] string? type = null,
        [FromQuery] int? limit = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting all stream URLs: Type={Type}, Limit={Limit}", type, limit);

            var results = new List<object>();

            // Get movies if requested or no type specified
            if (type == null || type == "movie")
            {
                var movies = await _context.Contents
                    .Where(c => c.Type == ContentType.Movie && !string.IsNullOrEmpty(c.FilePath))
                    .Take(limit ?? 100)
                    .Select(c => new
                    {
                        id = c.Id,
                        title = c.Title,
                        type = "movie",
                        year = c.ReleaseDate != null ? c.ReleaseDate.Value.Year : (int?)null,
                        streamUrl = $"{Request.Scheme}://{Request.Host}/videos/movies/{SanitizeFileName(c.Title)}-{c.Id}.mp4",
                        urlEndpoint = $"{Request.Scheme}://{Request.Host}/api/stream/movie/{c.Id}/url",
                        filePath = c.FilePath,
                        fileName = Path.GetFileName(c.FilePath)
                    })
                    .ToListAsync(cancellationToken);

                results.AddRange(movies);
            }

            // Get episodes if requested or no type specified
            if (type == null || type == "episode" || type == "series")
            {
                var episodes = await _context.Episodes
                    .Include(e => e.Content)
                    .Where(e => !string.IsNullOrEmpty(e.FilePath))
                    .Take(limit ?? 100)
                    .Select(e => new
                    {
                        id = e.Id,
                        title = e.Title,
                        seriesTitle = e.Content.Title,
                        type = "episode",
                        seasonNumber = e.SeasonNumber,
                        episodeNumber = e.EpisodeNumber,
                        streamUrl = $"{Request.Scheme}://{Request.Host}/videos/series/{SanitizeFileName(e.Content.Title)}/S{e.SeasonNumber:D2}E{e.EpisodeNumber:D2}-{SanitizeFileName(e.Title)}-{e.Id}.mp4",
                        urlEndpoint = $"{Request.Scheme}://{Request.Host}/api/stream/episode/{e.Id}/url",
                        filePath = e.FilePath,
                        fileName = Path.GetFileName(e.FilePath)
                    })
                    .ToListAsync(cancellationToken);

                results.AddRange(episodes);
            }

            return Ok(new
            {
                items = results,
                total = results.Count,
                usage = new
                {
                    description = "Use 'streamUrl' for direct video playback in external players",
                    example = "Copy the streamUrl and paste it into VLC, MX Player, or any video player that supports HTTP streaming"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all stream URLs");
            return StatusCode(500, new { error = "Failed to get stream URLs", details = ex.Message });
        }
    }

    private static string GetMimeType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".mp4" => "video/mp4",
            ".mkv" => "video/x-matroska",
            ".avi" => "video/x-msvideo",
            ".mov" => "video/quicktime",
            ".wmv" => "video/x-ms-wmv",
            ".flv" => "video/x-flv",
            ".webm" => "video/webm",
            ".m4v" => "video/x-m4v",
            _ => "video/mp4" // Default fallback
        };
    }

    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return "video";

        // Remove invalid characters and replace with hyphens
        var invalidChars = Path.GetInvalidFileNameChars().Concat(new[] { ' ', ':', '?', '#', '[', ']', '@', '!', '$', '&', '\'', '(', ')', '*', '+', ',', ';', '=' }).ToArray();
        var sanitized = string.Join("-", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        
        // Remove multiple consecutive hyphens
        while (sanitized.Contains("--"))
            sanitized = sanitized.Replace("--", "-");
            
        return sanitized.Trim('-').ToLowerInvariant();
    }
}