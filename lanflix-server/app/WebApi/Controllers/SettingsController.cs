using Lanflix.Application.Common.DTOs;
using Lanflix.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.WebApi.Controllers;

[ApiController]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settingsService;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        ISettingsService settingsService,
        IApplicationDbContext context,
        ILogger<SettingsController> logger)
    {
        _settingsService = settingsService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get current server settings
    /// </summary>
    [HttpGet]
    // [Authorize(Roles = "Admin")] // Uncomment when authentication is implemented
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetSettings(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting server settings");

        var settings = await _settingsService.GetSettingsAsync(cancellationToken);

        // Also load all per-profile streaming preferences and other custom settings
        var customSettings = new Dictionary<string, object>();
        
        // Get all custom settings (like streamingPreferences_X)
        var allSettings = await _context.ServerSettings
            .AsNoTracking()
            .Where(s => s.Key.StartsWith("streamingPreferences_") || 
                       s.Key.StartsWith("custom_") ||
                       !s.Key.StartsWith("Lanflix:"))
            .ToListAsync(cancellationToken);

        foreach (var setting in allSettings)
        {
            try
            {
                // Try to deserialize JSON values
                var prefValue = System.Text.Json.JsonSerializer.Deserialize<object>(setting.Value);
                customSettings[setting.Key] = prefValue ?? setting.Value;
            }
            catch
            {
                // If not JSON, store as string
                customSettings[setting.Key] = setting.Value;
            }
        }

        // Return both structured settings and custom settings
        return Ok(new
        {
            // Custom settings (like per-profile preferences)
            settings = customSettings,
            
            // Structured server settings
            mediaPaths = settings.MediaPaths,
            transcoding = settings.Transcoding,
            streaming = settings.Streaming,
            cache = settings.Cache,
            externalApis = settings.ExternalApis
        });
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
    /// Update streaming preferences for a profile
    /// </summary>
    [HttpPut("streaming/{profileId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateStreamingPreferences(
        [FromRoute] int profileId,
        [FromBody] UpdateStreamingPreferencesRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating streaming preferences for profile {ProfileId}", profileId);

        try
        {
            // Save per-profile streaming preferences as a JSON string in ServerSettings
            var settingKey = $"streamingPreferences_{profileId}";
            var settingValue = System.Text.Json.JsonSerializer.Serialize(request.StreamingPreferences);
            
            await _settingsService.UpdateSettingAsync(settingKey, settingValue, cancellationToken);

            _logger.LogInformation("Streaming preferences updated successfully for profile {ProfileId}", profileId);
            return Ok(new { message = "Streaming preferences updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating streaming preferences for profile {ProfileId}", profileId);
            return BadRequest(new { message = "Error updating streaming preferences", error = ex.Message });
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

    /// <summary>
    /// Get database settings count for debugging
    /// </summary>
    [HttpGet("debug/count")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetSettingsCount(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting settings count for debugging");

        try
        {
            var settings = await _settingsService.GetSettingsAsync(cancellationToken);
            
            return Ok(new
            {
                message = "Settings loaded successfully",
                hasMoviesPath = !string.IsNullOrEmpty(settings.MediaPaths.Movies),
                hasSeriesPath = !string.IsNullOrEmpty(settings.MediaPaths.Series),
                hasTmdbKey = !string.IsNullOrEmpty(settings.ExternalApis.Tmdb.ApiKey),
                hasSonarrUrl = !string.IsNullOrEmpty(settings.ExternalApis.Sonarr.Url),
                hasSonarrKey = !string.IsNullOrEmpty(settings.ExternalApis.Sonarr.ApiKey),
                hasRadarrUrl = !string.IsNullOrEmpty(settings.ExternalApis.Radarr.Url),
                hasRadarrKey = !string.IsNullOrEmpty(settings.ExternalApis.Radarr.ApiKey),
                hasProwlarrUrl = !string.IsNullOrEmpty(settings.ExternalApis.Prowlarr.Url),
                hasProwlarrKey = !string.IsNullOrEmpty(settings.ExternalApis.Prowlarr.ApiKey)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting settings count");
            return BadRequest(new { message = "Error getting settings count", error = ex.Message });
        }
    }

    /// <summary>
    /// Save a custom setting (like per-profile user settings)
    /// </summary>
    [HttpPut("custom/{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> SaveCustomSetting(
        [FromRoute] string key,
        [FromBody] CustomSettingRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Saving custom setting: {Key}", key);

        try
        {
            await _settingsService.UpdateSettingAsync(key, request.Value, cancellationToken);
            return Ok(new { message = "Setting saved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving custom setting: {Key}", key);
            return BadRequest(new { message = "Error saving setting", error = ex.Message });
        }
    }
}

public class CustomSettingRequest
{
    public string Value { get; set; } = string.Empty;
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class UpdateStreamingPreferencesRequest
{
    public StreamingSettings? StreamingPreferences { get; set; }
}
