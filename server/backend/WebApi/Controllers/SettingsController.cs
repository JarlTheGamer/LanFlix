using Lanflix.Application.Common.DTOs;
using Lanflix.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lanflix.WebApi.Controllers;

[ApiController]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        ISettingsService settingsService,
        ILogger<SettingsController> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    /// <summary>
    /// Get current server settings
    /// </summary>
    [HttpGet]
    // [Authorize(Roles = "Admin")] // Uncomment when authentication is implemented
    [ProducesResponseType(typeof(ServerSettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ServerSettingsDto>> GetSettings(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting server settings");

        var settings = await _settingsService.GetSettingsAsync(cancellationToken);

        return Ok(settings);
    }

    /// <summary>
    /// Update server settings
    /// </summary>
    [HttpPut]
    // [Authorize(Roles = "Admin")] // Uncomment when authentication is implemented
    [ProducesResponseType(typeof(ServerSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ServerSettingsDto>> UpdateSettings(
        [FromBody] ServerSettingsDto settings,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating server settings");

        try
        {
            await _settingsService.UpdateSettingsAsync(settings, cancellationToken);

            // Return updated settings
            var updatedSettings = await _settingsService.GetSettingsAsync(cancellationToken);

            _logger.LogInformation("Server settings updated successfully");

            return Ok(updatedSettings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating server settings");
            return BadRequest(new { message = "Error updating settings", error = ex.Message });
        }
    }

    /// <summary>
    /// Validate settings without saving
    /// </summary>
    [HttpPost("validate")]
    // [Authorize(Roles = "Admin")] // Uncomment when authentication is implemented
    [ProducesResponseType(typeof(ValidationResult), StatusCodes.Status200OK)]
    public ActionResult<ValidationResult> ValidateSettings(
        [FromBody] ServerSettingsDto settings)
    {
        _logger.LogInformation("Validating server settings");

        var errors = new List<string>();

        // Validate media paths
        if (!string.IsNullOrEmpty(settings.MediaPaths.Movies) && !Directory.Exists(settings.MediaPaths.Movies))
        {
            errors.Add($"Movies path does not exist: {settings.MediaPaths.Movies}");
        }

        if (!string.IsNullOrEmpty(settings.MediaPaths.Series) && !Directory.Exists(settings.MediaPaths.Series))
        {
            errors.Add($"Series path does not exist: {settings.MediaPaths.Series}");
        }

        // Validate transcoding settings
        if (settings.Transcoding.MaxConcurrentTranscodes < 1 || settings.Transcoding.MaxConcurrentTranscodes > 10)
        {
            errors.Add("MaxConcurrentTranscodes must be between 1 and 10");
        }

        if (settings.Transcoding.DefaultBitrate < 1_000_000 || settings.Transcoding.DefaultBitrate > 50_000_000)
        {
            errors.Add("DefaultBitrate must be between 1Mbps and 50Mbps");
        }

        // Validate streaming settings
        if (settings.Streaming.ChunkSize < 8192 || settings.Streaming.ChunkSize > 1_048_576)
        {
            errors.Add("ChunkSize must be between 8KB and 1MB");
        }

        var result = new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };

        return Ok(result);
    }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}
