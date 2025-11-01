using Lanflix.Application.Common.DTOs;
using Lanflix.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Lanflix.WebApi.Controllers;

[ApiController]
[Route("api/app-updates")]
public class AppUpdateController : ControllerBase
{
    private readonly IAppUpdateService _appUpdateService;
    private readonly ILogger<AppUpdateController> _logger;

    public AppUpdateController(
        IAppUpdateService appUpdateService,
        ILogger<AppUpdateController> logger)
    {
        _appUpdateService = appUpdateService;
        _logger = logger;
    }

    /// <summary>
    /// Get the latest Android app version information
    /// </summary>
    [HttpGet("android/latest")]
    [ProducesResponseType(typeof(AppUpdateInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AppUpdateInfo>> GetLatestAndroidVersion(
        [FromQuery] string currentVersion,
        [FromQuery] string architecture = "arm64-v8a",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(currentVersion))
        {
            return BadRequest(new { message = "currentVersion parameter is required" });
        }

        _logger.LogInformation(
            "Checking for updates: Current={CurrentVersion}, Architecture={Architecture}",
            currentVersion, architecture);

        var updateInfo = await _appUpdateService.GetLatestVersionAsync(
            "android",
            currentVersion,
            architecture,
            cancellationToken);

        if (updateInfo == null)
        {
            _logger.LogInformation("No update available for version {Version}", currentVersion);
            return NoContent();
        }

        _logger.LogInformation(
            "Update available: {Version} (current: {CurrentVersion})",
            updateInfo.Version, currentVersion);

        return Ok(updateInfo);
    }

    /// <summary>
    /// Download a specific Android APK version
    /// </summary>
    [HttpGet("android/download/{version}/{architecture?}")]
    [EnableRateLimiting("streaming")] // Reuse streaming rate limiter for large file downloads
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadApk(
        string version,
        string architecture = "arm64-v8a",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "APK download requested: Version={Version}, Architecture={Architecture}",
            version, architecture);

        var apkPath = await _appUpdateService.GetApkPathAsync(version, architecture, cancellationToken);

        if (apkPath == null || !System.IO.File.Exists(apkPath))
        {
            _logger.LogWarning("APK not found: {Version} ({Architecture})", version, architecture);
            return NotFound(new { message = $"APK version {version} for {architecture} not found" });
        }

        var fileName = $"lanflix-{version}-{architecture}.apk";

        _logger.LogInformation("Serving APK: {FileName}", fileName);

        return PhysicalFile(
            apkPath,
            "application/vnd.android.package-archive",
            fileName,
            enableRangeProcessing: true);
    }

    /// <summary>
    /// Upload a new Android APK release (Admin only)
    /// </summary>
    [HttpPost("android/upload")]
    // [Authorize(Roles = "Admin")] // Uncomment when authentication is implemented
    [RequestSizeLimit(200_000_000)] // 200MB limit
    [ProducesResponseType(typeof(AppUpdateInfo), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AppUpdateInfo>> UploadApk(
        IFormFile apkFile,
        [FromForm] string version,
        [FromForm] int versionCode,
        [FromForm] string releaseNotes = "",
        [FromForm] bool isForceUpdate = false,
        [FromForm] string minimumSupportedVersion = "1.0.0",
        [FromForm] string architecture = "arm64-v8a",
        CancellationToken cancellationToken = default)
    {
        if (apkFile == null || apkFile.Length == 0)
        {
            return BadRequest(new { message = "APK file is required" });
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            return BadRequest(new { message = "Version is required" });
        }

        if (versionCode <= 0)
        {
            return BadRequest(new { message = "Version code must be positive" });
        }

        // Validate file extension
        if (!apkFile.FileName.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "File must be an APK" });
        }

        // Validate file size (max 200MB)
        if (apkFile.Length > 200_000_000)
        {
            return BadRequest(new { message = "APK file size exceeds 200MB limit" });
        }

        _logger.LogInformation(
            "Uploading APK: Version={Version}, VersionCode={VersionCode}, Size={Size} bytes, Architecture={Architecture}",
            version, versionCode, apkFile.Length, architecture);

        var releaseInfo = new AppReleaseInfo
        {
            Version = version,
            VersionCode = versionCode,
            ReleaseNotes = releaseNotes,
            IsForceUpdate = isForceUpdate,
            MinimumSupportedVersion = minimumSupportedVersion,
            Architecture = architecture
        };

        AppUpdateInfo updateInfo;
        try
        {
            using var stream = apkFile.OpenReadStream();
            updateInfo = await _appUpdateService.SaveReleaseAsync(stream, releaseInfo, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving APK release");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = "Error saving APK release" });
        }

        _logger.LogInformation("APK uploaded successfully: {Version}", version);

        return CreatedAtAction(
            nameof(GetLatestAndroidVersion),
            new { currentVersion = "0.0.0", architecture },
            updateInfo);
    }

    /// <summary>
    /// Get version history for Android releases
    /// </summary>
    [HttpGet("android/history")]
    [ProducesResponseType(typeof(List<AppUpdateInfo>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AppUpdateInfo>>> GetVersionHistory(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting Android version history");

        var history = await _appUpdateService.GetVersionHistoryAsync("android", cancellationToken);

        return Ok(history);
    }
}
