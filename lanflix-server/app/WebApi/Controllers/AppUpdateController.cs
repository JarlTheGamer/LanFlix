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

        public AppUpdateController(ILogger<AppUpdateController> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        [HttpGet("update-check")]
        public IActionResult CheckForUpdate([FromQuery] int currentVersion, [FromQuery] string platform = "android")
        {
            try
            {
                // Define the latest version info
                // In production, this should come from a database or configuration
                var latestVersion = new
                {
                    versionName = "3.9.0",
                    versionCode = 39,
                    downloadUrl = $"{Request.Scheme}://{Request.Host}/api/app/download/lanflix-native-webview-v3.9.0.apk",
                    releaseNotes = "• Improved video playback performance\n• Fixed subtitle synchronization issues\n• Enhanced remote control support\n• Bug fixes and stability improvements",
                    mandatory = false,
                    fileSize = 15728640L, // 15MB in bytes
                    checksum = "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0u1v2w3x4y5z6" // SHA-256 hash
                };

                // Check if update is available
                if (latestVersion.versionCode > currentVersion)
                {
                    _logger.LogInformation($"Update available for version {currentVersion}. Latest: {latestVersion.versionCode}");
                    return Ok(latestVersion);
                }

                // No update available
                return Ok(new { hasUpdate = false });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for updates");
                return StatusCode(500, new { error = "Failed to check for updates" });
            }
        }

        [HttpGet("download/{fileName}")]
        public IActionResult DownloadApk(string fileName)
        {
            try
            {
                // Path to your APK files (adjust as needed)
                var apkPath = Path.Combine(_environment.ContentRootPath, "releases", fileName);
                
                if (!System.IO.File.Exists(apkPath))
                {
                    return NotFound(new { error = "APK file not found" });
                }

                var fileBytes = System.IO.File.ReadAllBytes(apkPath);
                var contentType = "application/vnd.android.package-archive";
                
                _logger.LogInformation($"Serving APK download: {fileName}");
                
                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading APK: {fileName}");
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
                    supportedAppVersions = new[] { "3.8.0", "3.9.0" }
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