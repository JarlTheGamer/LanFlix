using Lanflix.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Reflection;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/app")]
    public class AppUpdateController : ControllerBase
    {
        private readonly ILogger<AppUpdateController> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly IReleaseMetadataService _releaseMetadataService;

        public AppUpdateController(
            ILogger<AppUpdateController> logger,
            IWebHostEnvironment environment,
            IReleaseMetadataService releaseMetadataService)
        {
            _logger = logger;
            _environment = environment;
            _releaseMetadataService = releaseMetadataService;
        }

        [HttpGet("update-check")]
        public async Task<IActionResult> CheckForUpdate([FromQuery] int currentVersion, [FromQuery] string platform = "android", CancellationToken cancellationToken = default)
        {
            try
            {
                var latestRelease = await _releaseMetadataService.GetLatestAppReleaseAsync(currentVersion, cancellationToken);
                
                if (latestRelease != null)
                {
                    _logger.LogInformation("Update check for version {CurrentVersion}. Latest version code: {LatestVersionCode}", currentVersion, latestRelease.VersionCode);
                    
                    if (latestRelease.VersionCode > currentVersion)
                    {
                        return Ok(new
                        {
                            hasUpdate = true,
                            versionName = latestRelease.VersionName,
                            versionCode = latestRelease.VersionCode,
                            downloadUrl = latestRelease.DownloadUrl,
                            releaseNotes = latestRelease.ReleaseNotes,
                            mandatory = latestRelease.Mandatory,
                            fileSize = latestRelease.FileSize,
                            checksum = latestRelease.Checksum
                        });
                    }
                }

                return Ok(new { hasUpdate = false });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for app updates");
                return StatusCode(500, new { error = "Failed to check for app updates" });
            }
        }

        [HttpGet("download/{fileName}")]
        public IActionResult DownloadApk(string fileName)
        {
            try
            {
                var apkPath = Path.Combine(_environment.ContentRootPath, "releases", fileName);
                
                if (!System.IO.File.Exists(apkPath))
                {
                    return NotFound(new { error = "APK file not found" });
                }

                var fileBytes = System.IO.File.ReadAllBytes(apkPath);
                var contentType = "application/vnd.android.package-archive";
                
                _logger.LogInformation("Serving APK download: {FileName}", fileName);
                
                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading APK: {FileName}", fileName);
                return StatusCode(500, new { error = "Failed to download APK" });
            }
        }

        [HttpGet("version")]
        public IActionResult GetCurrentVersion()
        {
            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                return Ok(new
                {
                    serverVersion = version?.ToString() ?? "1.0.0",
                    apiVersion = "1.0",
                    supportedAppVersions = new[] { "3.8.0", "3.9.0", "4.0.0" }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting version info");
                return StatusCode(500, new { error = "Failed to get version info" });
            }
        }
    }
}