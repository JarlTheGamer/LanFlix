using Lanflix.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lanflix.WebApi.Controllers;

[ApiController]
[Route("api/server-update")]
public class ServerUpdateController : ControllerBase
{
    private readonly IServerUpdateService _updateService;
    private readonly ILogger<ServerUpdateController> _logger;

    public ServerUpdateController(
        IServerUpdateService updateService,
        ILogger<ServerUpdateController> logger)
    {
        _updateService = updateService;
        _logger = logger;
    }

    /// <summary>
    /// Get current server version
    /// </summary>
    [HttpGet("version")]
    public IActionResult GetVersion()
    {
        var version = _updateService.GetCurrentVersion();
        return Ok(new { version });
    }

    /// <summary>
    /// Check for available server updates
    /// </summary>
    [HttpGet("check")]
    public async Task<IActionResult> CheckForUpdates(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Manual update check requested");

            var updateInfo = await _updateService.CheckForUpdatesAsync(cancellationToken);

            if (updateInfo == null)
            {
                return Ok(new
                {
                    updateAvailable = false,
                    currentVersion = _updateService.GetCurrentVersion(),
                    message = "Server is up to date"
                });
            }

            return Ok(new
            {
                updateAvailable = true,
                currentVersion = updateInfo.CurrentVersion,
                latestVersion = updateInfo.Version,
                releaseDate = updateInfo.ReleaseDate,
                downloadUrl = updateInfo.DownloadUrl,
                fileSize = updateInfo.FileSize,
                releaseNotes = updateInfo.ReleaseNotes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for updates");
            return StatusCode(500, new { error = "Failed to check for updates", details = ex.Message });
        }
    }

    /// <summary>
    /// Download and apply server update
    /// </summary>
    [HttpPost("apply")]
    public async Task<IActionResult> ApplyUpdate(
        [FromBody] ApplyUpdateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Manual update requested: {Url}", request.DownloadUrl);

            if (string.IsNullOrEmpty(request.DownloadUrl))
            {
                return BadRequest(new { error = "Download URL is required" });
            }

            var success = await _updateService.DownloadAndApplyUpdateAsync(
                request.DownloadUrl,
                cancellationToken);

            if (success)
            {
                return Ok(new
                {
                    message = "Update is being applied. Server will restart shortly.",
                    success = true
                });
            }
            else
            {
                return StatusCode(500, new { error = "Failed to apply update" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying update");
            return StatusCode(500, new { error = "Failed to apply update", details = ex.Message });
        }
    }
}

public class ApplyUpdateRequest
{
    public string DownloadUrl { get; set; } = string.Empty;
}
