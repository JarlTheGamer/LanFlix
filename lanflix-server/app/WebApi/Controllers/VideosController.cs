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

    public VideosController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("movies/{fileName}")]
    public async Task<IActionResult> ServeMovie([FromRoute] string fileName)
    {
        // Extract ID from filename (format: movie-name-123.mp4)
        var match = Regex.Match(fileName, @"-(\d+)\.[^.]+$");
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out var movieId))
            return NotFound();

        var movie = await _context.Contents
            .Where(c => c.Id == movieId && c.Type == ContentType.Movie)
            .Select(c => c.FilePath)
            .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(movie) || !System.IO.File.Exists(movie))
            return NotFound();

        // Serve the video file with range processing enabled
        return PhysicalFile(movie, "video/mp4", enableRangeProcessing: true);
    }

    [HttpGet("series/{seriesName}/{fileName}")]
    public async Task<IActionResult> ServeEpisode([FromRoute] string seriesName, [FromRoute] string fileName)
    {
        // Extract ID from filename (format: S01E01-episode-title-456.mp4)
        var match = Regex.Match(fileName, @"-(\d+)\.[^.]+$");
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out var episodeId))
            return NotFound();

        var episode = await _context.Episodes
            .Where(e => e.Id == episodeId)
            .Select(e => e.FilePath)
            .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(episode) || !System.IO.File.Exists(episode))
            return NotFound();

        // Serve the video file with range processing enabled
        return PhysicalFile(episode, "video/mp4", enableRangeProcessing: true);
    }
}