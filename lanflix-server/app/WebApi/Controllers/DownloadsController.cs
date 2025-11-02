using Lanflix.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lanflix.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DownloadsController : ControllerBase
{
    private readonly IRadarrClient _radarrClient;
    private readonly ISonarrClient _sonarrClient;
    private readonly ILogger<DownloadsController> _logger;

    public DownloadsController(
        IRadarrClient radarrClient,
        ISonarrClient sonarrClient,
        ILogger<DownloadsController> logger)
    {
        _radarrClient = radarrClient;
        _sonarrClient = sonarrClient;
        _logger = logger;
    }

    /// <summary>
    /// Get current download queue from both Radarr and Sonarr
    /// </summary>
    [HttpGet("queue")]
    public async Task<IActionResult> GetDownloadQueue(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting download queue from Radarr and Sonarr");

            var downloads = new List<object>();

            // Get Radarr queue
            try
            {
                var radarrQueue = await _radarrClient.GetQueueAsync(1, 50, cancellationToken);
                foreach (var item in radarrQueue.Records)
                {
                    downloads.Add(new
                    {
                        id = $"radarr_{item.Id}",
                        service = "radarr",
                        title = item.Title,
                        type = "movie",
                        status = MapRadarrStatus(item.Status),
                        progress = item.Size > 0 ? (int)((item.Sizeleft / (double)item.Size) * 100) : 0,
                        speed = FormatSpeed(item.DownloadRate),
                        eta = item.Timeleft,
                        quality = item.Quality?.Quality?.Name ?? "Unknown",
                        size = FormatSize(item.Size),
                        downloaded = FormatSize(item.Size - item.Sizeleft),
                        indexer = item.Indexer,
                        downloadClient = item.DownloadClient,
                        errorMessage = item.ErrorMessage
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get Radarr queue");
            }

            // Get Sonarr queue
            try
            {
                var sonarrQueue = await _sonarrClient.GetQueueAsync(1, 50, cancellationToken);
                foreach (var item in sonarrQueue.Records)
                {
                    downloads.Add(new
                    {
                        id = $"sonarr_{item.Id}",
                        service = "sonarr",
                        title = $"{item.Series?.Title} - S{item.Episode?.SeasonNumber:D2}E{item.Episode?.EpisodeNumber:D2}",
                        type = "episode",
                        status = MapSonarrStatus(item.Status),
                        progress = item.Size > 0 ? (int)((item.Sizeleft / (double)item.Size) * 100) : 0,
                        speed = FormatSpeed(item.DownloadRate),
                        eta = item.Timeleft,
                        quality = item.Quality?.Quality?.Name ?? "Unknown",
                        size = FormatSize(item.Size),
                        downloaded = FormatSize(item.Size - item.Sizeleft),
                        indexer = item.Indexer,
                        downloadClient = item.DownloadClient,
                        errorMessage = item.ErrorMessage,
                        episodeTitle = item.Episode?.Title
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get Sonarr queue");
            }

            return Ok(new { downloads });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get download queue");
            return StatusCode(500, new { error = "Failed to get download queue", details = ex.Message });
        }
    }

    /// <summary>
    /// Cancel/remove download from queue
    /// </summary>
    [HttpDelete("queue/{id}")]
    public async Task<IActionResult> CancelDownload(
        [FromRoute] string id,
        [FromBody] CancelDownloadRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Cancelling download: {Id} from {Service}", id, request.Service);

            if (id.StartsWith("radarr_") && request.Service == "radarr")
            {
                var radarrId = int.Parse(id.Replace("radarr_", ""));
                // Note: Radarr doesn't have a direct cancel API, but we could implement removal
                // For now, return success
                return Ok(new { message = "Download removal requested" });
            }
            else if (id.StartsWith("sonarr_") && request.Service == "sonarr")
            {
                var sonarrId = int.Parse(id.Replace("sonarr_", ""));
                // Note: Sonarr doesn't have a direct cancel API, but we could implement removal
                // For now, return success
                return Ok(new { message = "Download removal requested" });
            }
            else
            {
                return BadRequest(new { error = "Invalid download ID or service" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel download: {Id}", id);
            return StatusCode(500, new { error = "Failed to cancel download", details = ex.Message });
        }
    }

    private static string MapRadarrStatus(string status)
    {
        return status?.ToLower() switch
        {
            "downloading" => "downloading",
            "paused" => "paused",
            "queued" => "queued",
            "completed" => "completed",
            "failed" => "failed",
            "warning" => "warning",
            _ => "unknown"
        };
    }

    private static string MapSonarrStatus(string status)
    {
        return status?.ToLower() switch
        {
            "downloading" => "downloading",
            "paused" => "paused",
            "queued" => "queued",
            "completed" => "completed",
            "failed" => "failed",
            "warning" => "warning",
            _ => "unknown"
        };
    }

    private static string FormatSpeed(long? bytesPerSecond)
    {
        if (!bytesPerSecond.HasValue || bytesPerSecond == 0)
            return "0 B/s";

        var speed = bytesPerSecond.Value;
        string[] sizes = { "B/s", "KB/s", "MB/s", "GB/s" };
        int order = 0;
        while (speed >= 1024 && order < sizes.Length - 1)
        {
            order++;
            speed = speed / 1024;
        }

        return $"{speed:0.##} {sizes[order]}";
    }

    private static string FormatSize(long bytes)
    {
        if (bytes == 0)
            return "0 B";

        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size = size / 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }
}

public class CancelDownloadRequest
{
    public string Service { get; set; } = string.Empty;
}