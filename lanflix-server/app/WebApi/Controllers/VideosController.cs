using Lanflix.Application.Common.Interfaces;
using Lanflix.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Lanflix.WebApi.Controllers;

[ApiController]
[Route("videos")]
public class VideosController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<VideosController> _logger;

    public VideosController(
        IApplicationDbContext context,
        ILogger<VideosController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Serve movie files with pretty URLs like /videos/movies/movie-name-123.mp4
    /// </summary>
    [HttpGet("movies/{fileName}")]
    public async Task<IActionResult> ServeMovie(
        [FromRoute] string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Extract ID from filename (format: movie-name-123.mp4)
            var match = Regex.Match(fileName, @"-(\d+)\.[^.]+$");
            if (!match.Success || !int.TryParse(match.Groups[1].Value, out var movieId))
            {
                return NotFound();
            }

            var movie = await _context.Contents
                .Where(c => c.Id == movieId && c.Type == ContentType.Movie)
                .FirstOrDefaultAsync(cancellationToken);

            if (movie == null || string.IsNullOrEmpty(movie.FilePath) || !System.IO.File.Exists(movie.FilePath))
            {
                return NotFound();
            }

            var mimeType = GetMimeType(movie.FilePath);
            var originalFileName = Path.GetFileName(movie.FilePath);
            
            // Support range requests for video streaming and set proper headers
            Response.Headers.Add("Accept-Ranges", "bytes");
            Response.Headers.Add("Content-Disposition", $"inline; filename=\"{originalFileName}\"");
            
            return PhysicalFile(movie.FilePath, mimeType, enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serve movie: {FileName}", fileName);
            return StatusCode(500);
        }
    }

    /// <summary>
    /// Serve episode files with pretty URLs like /videos/series/series-name/S01E01-episode-title-456.mp4
    /// </summary>
    [HttpGet("series/{seriesName}/{fileName}")]
    public async Task<IActionResult> ServeEpisode(
        [FromRoute] string seriesName,
        [FromRoute] string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Extract ID from filename (format: S01E01-episode-title-456.mp4)
            var match = Regex.Match(fileName, @"-(\d+)\.[^.]+$");
            if (!match.Success || !int.TryParse(match.Groups[1].Value, out var episodeId))
            {
                return NotFound();
            }

            var episode = await _context.Episodes
                .Where(e => e.Id == episodeId)
                .FirstOrDefaultAsync(cancellationToken);

            if (episode == null || string.IsNullOrEmpty(episode.FilePath) || !System.IO.File.Exists(episode.FilePath))
            {
                return NotFound();
            }

            var mimeType = GetMimeType(episode.FilePath);
            var originalFileName = Path.GetFileName(episode.FilePath);
            
            // Support range requests for video streaming and set proper headers
            Response.Headers.Add("Accept-Ranges", "bytes");
            Response.Headers.Add("Content-Disposition", $"inline; filename=\"{originalFileName}\"");
            
            return PhysicalFile(episode.FilePath, mimeType, enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serve episode: {SeriesName}/{FileName}", seriesName, fileName);
            return StatusCode(500);
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
}