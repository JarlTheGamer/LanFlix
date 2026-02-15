using Microsoft.AspNetCore.Mvc;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImageController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ImageController> _logger;

    public ImageController(IApplicationDbContext context, ILogger<ImageController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("{id}/poster")]
    public async Task<IActionResult> GetPoster(int id, CancellationToken cancellationToken)
    {
        var content = await _context.Contents.FindAsync(new object[] { id }, cancellationToken);
        if (content == null || string.IsNullOrEmpty(content.FilePath))
        {
            return NotFound();
        }

        var folder = Directory.Exists(content.FilePath) 
            ? content.FilePath 
            : Path.GetDirectoryName(content.FilePath);

        if (string.IsNullOrEmpty(folder))
        {
            return NotFound();
        }

        var path = Path.Combine(folder, "poster.jpg");
        if (!System.IO.File.Exists(path))
        {
            return NotFound();
        }

        return PhysicalFile(path, "image/jpeg");
    }

    [HttpGet("{id}/backdrop")]
    public async Task<IActionResult> GetBackdrop(int id, CancellationToken cancellationToken)
    {
        var content = await _context.Contents.FindAsync(new object[] { id }, cancellationToken);
        if (content == null || string.IsNullOrEmpty(content.FilePath))
        {
            return NotFound();
        }

        var folder = Directory.Exists(content.FilePath) 
            ? content.FilePath 
            : Path.GetDirectoryName(content.FilePath);

        if (string.IsNullOrEmpty(folder))
        {
            return NotFound();
        }

        var path = Path.Combine(folder, "backdrop.jpg");
        if (!System.IO.File.Exists(path))
        {
            return NotFound();
        }

        return PhysicalFile(path, "image/jpeg");
    }

    [HttpGet("{id}/season/{seasonNumber}/episode/{episodeNumber}/still")]
    public async Task<IActionResult> GetEpisodeStill(int id, int seasonNumber, int episodeNumber, CancellationToken cancellationToken)
    {
        var content = await _context.Contents.FindAsync(new object[] { id }, cancellationToken);
        if (content == null || string.IsNullOrEmpty(content.FilePath))
        {
            return NotFound();
        }

        var folder = Directory.Exists(content.FilePath) 
            ? content.FilePath 
            : Path.GetDirectoryName(content.FilePath);
        
        if (string.IsNullOrEmpty(folder))
        {
            return NotFound();
        }

        // Season folder
        var seasonFolder = Path.Combine(folder, $"Season {seasonNumber}");
        
        // Still filename: S{season}E{episode}.jpg
        var filename = $"S{seasonNumber:D2}E{episodeNumber:D2}.jpg";
        var path = Path.Combine(seasonFolder, filename);

        if (!System.IO.File.Exists(path))
        {
            return NotFound();
        }

        return PhysicalFile(path, "image/jpeg");
    }
}
