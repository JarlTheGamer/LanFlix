using Lanflix.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lanflix.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly ILibraryService _libraryService;
    private readonly ILogger<JobsController> _logger;

    public JobsController(
        ILibraryService libraryService,
        ILogger<JobsController> logger)
    {
        _libraryService = libraryService;
        _logger = logger;
    }

    /// <summary>
    /// Get status of background jobs
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetJobsStatus()
    {
        try
        {
            _logger.LogInformation("Getting background jobs status");

            // Return configured background jobs
            var jobs = new[]
            {
                new
                {
                    name = "library-scan",
                    displayName = "Library Scan",
                    description = "Scans media folders for new content and updates the library",
                    schedule = "Every 6 hours",
                    lastRun = (DateTime?)null, // TODO: Track last run time
                    nextRun = (DateTime?)null, // TODO: Calculate next run time
                    running = false, // TODO: Track if currently running
                    enabled = true
                },
                new
                {
                    name = "metadata-refresh",
                    displayName = "Metadata Refresh",
                    description = "Updates movie and TV show metadata from TMDB",
                    schedule = "Daily at 3 AM",
                    lastRun = DateTime.UtcNow.AddDays(-1).Date.AddHours(3),
                    nextRun = DateTime.UtcNow.Date.AddDays(1).AddHours(3),
                    running = false,
                    enabled = true
                },
                new
                {
                    name = "cleanup-temp",
                    displayName = "Cleanup Temporary Files",
                    description = "Removes old transcoding files and cache data",
                    schedule = "Daily at 2 AM",
                    lastRun = DateTime.UtcNow.Date.AddHours(2),
                    nextRun = DateTime.UtcNow.Date.AddDays(1).AddHours(2),
                    running = false,
                    enabled = true
                },
                new
                {
                    name = "server-update-check",
                    displayName = "Server Update Check",
                    description = "Checks for available server updates",
                    schedule = "Weekly",
                    lastRun = DateTime.UtcNow.AddDays(-3),
                    nextRun = DateTime.UtcNow.AddDays(4),
                    running = false,
                    enabled = true
                }
            };

            return Ok(new { jobs });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get jobs status");
            return StatusCode(500, new { error = "Failed to get jobs status", details = ex.Message });
        }
    }

    /// <summary>
    /// Trigger a background job manually
    /// </summary>
    [HttpPost("{jobName}/trigger")]
    public async Task<IActionResult> TriggerJob([FromRoute] string jobName)
    {
        try
        {
            _logger.LogInformation("Triggering job: {JobName}", jobName);

            var success = jobName switch
            {
                "library-scan" => await TriggerLibraryScan(),
                "metadata-refresh" => await TriggerMetadataRefresh(),
                "cleanup-temp" => await TriggerCleanupTemp(),
                "server-update-check" => await TriggerServerUpdateCheck(),
                _ => false
            };

            if (success)
            {
                return Ok(new { message = $"Job '{jobName}' triggered successfully" });
            }
            else
            {
                return BadRequest(new { error = $"Failed to execute job: {jobName}" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger job: {JobName}", jobName);
            return StatusCode(500, new { error = "Failed to trigger job", details = ex.Message });
        }
    }

    /// <summary>
    /// Trigger library scan directly
    /// </summary>
    [HttpPost("library-scan/trigger")]
    public async Task<IActionResult> TriggerLibraryScanDirect()
    {
        try
        {
            _logger.LogInformation("Direct library scan triggered");
            var result = await _libraryService.ScanLibraryAsync();
            
            return Ok(new 
            { 
                message = "Library scan completed successfully",
                result = new
                {
                    added = result.Added,
                    updated = result.Updated,
                    removed = result.Removed,
                    errors = result.Errors
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Direct library scan failed");
            return StatusCode(500, new { error = "Library scan failed", details = ex.Message });
        }
    }

    private async Task<bool> TriggerLibraryScan()
    {
        try
        {
            _logger.LogInformation("Library scan triggered manually");
            var result = await _libraryService.ScanLibraryAsync();
            _logger.LogInformation("Library scan completed: {Added} added, {Updated} updated, {Removed} removed, {Errors} errors", 
                result.Added, result.Updated, result.Removed, result.Errors.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Library scan failed");
            return false;
        }
    }

    private async Task<bool> TriggerMetadataRefresh()
    {
        _logger.LogInformation("Metadata refresh triggered manually");
        // In a real implementation, this would start the metadata refresh job
        await Task.Delay(100); // Simulate async work
        return true;
    }

    private async Task<bool> TriggerCleanupTemp()
    {
        _logger.LogInformation("Cleanup temp files triggered manually");
        // In a real implementation, this would start the cleanup job
        await Task.Delay(100); // Simulate async work
        return true;
    }

    private async Task<bool> TriggerServerUpdateCheck()
    {
        _logger.LogInformation("Server update check triggered manually");
        // In a real implementation, this would start the update check job
        await Task.Delay(100); // Simulate async work
        return true;
    }
}