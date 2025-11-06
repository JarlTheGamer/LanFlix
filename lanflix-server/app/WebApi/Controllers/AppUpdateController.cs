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
        private readonly HttpClient _httpClient;

        public AppUpdateController(ILogger<AppUpdateController> logger, IWebHostEnvironment environment, HttpClient httpClient)
        {
            _logger = logger;
            _environment = environment;
            _httpClient = httpClient;
        }

        [HttpGet("update-check")]
        public async Task<IActionResult> CheckForUpdate([FromQuery] int currentVersion, [FromQuery] string platform = "android")
        {
            try
            {
                // Try to get latest release from GitHub API
                var githubRelease = await GetLatestGitHubRelease();
                
                if (githubRelease != null)
                {
                    _logger.LogInformation($"Update check via GitHub API for version {currentVersion}. Latest: {githubRelease.VersionCode}");
                    
                    // Check if update is available
                    if (githubRelease.VersionCode > currentVersion)
                    {
                        return Ok(githubRelease);
                    }
                }
                else
                {
                    // Fallback to hardcoded version info
                    var latestVersion = new
                    {
                        versionName = "4.0.0",
                        versionCode = 4,
                        downloadUrl = "https://github.com/JarlTheGamer/Applications./releases/download/v4.0.0/lanflix-native-webview-v4.0.0.apk",
                        releaseNotes = "• OTA Update System: Automatic update system integrated\n• Enhanced Performance: Optimized hybrid experience\n• Hardware Acceleration: Smooth performance on all devices\n• Android TV Support: Remote control navigation\n• Auto-orientation: Works on phones and tablets",
                        mandatory = false,
                        fileSize = 2970279L, // 2.83MB in bytes
                        checksum = "b7b67ffa4c3be9a7c6fdb7526a0983b6a009112a96f29b618b206daca5edb40c" // SHA-256 hash from build
                    };

                    // Check if update is available
                    if (latestVersion.versionCode > currentVersion)
                    {
                        _logger.LogInformation($"Update available for version {currentVersion}. Latest: {latestVersion.versionCode}");
                        return Ok(latestVersion);
                    }
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

        private async Task<UpdateInfo?> GetLatestGitHubRelease()
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "Lanflix-Server");
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

                var response = await _httpClient.GetAsync("https://api.github.com/repos/JarlTheGamer/Applications./releases/latest");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"GitHub API request failed: {response.StatusCode}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var release = System.Text.Json.JsonSerializer.Deserialize<GitHubRelease>(content, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
                });

                if (release?.Assets?.Any() == true)
                {
                    var apkAsset = release.Assets.FirstOrDefault(a => a.Name?.EndsWith(".apk") == true);
                    
                    if (apkAsset != null)
                    {
                        // Extract version info from tag
                        var versionString = release.TagName?.TrimStart('v') ?? "4.0.0";
                        var versionParts = versionString.Split('.');
                        // Use major version as version code (4.0.0 = 4, 3.9.0 = 39 for backwards compatibility)
                        var versionCode = versionString == "4.0.0" ? 4 : 
                                         (int.Parse(versionParts[0]) * 10) + (versionParts.Length > 1 ? int.Parse(versionParts[1]) : 0);

                        return new UpdateInfo
                        {
                            VersionName = versionString,
                            VersionCode = versionCode,
                            DownloadUrl = apkAsset.BrowserDownloadUrl ?? "",
                            ReleaseNotes = release.Body ?? "Bug fixes and improvements",
                            Mandatory = false,
                            FileSize = apkAsset.Size ?? 0L,
                            Checksum = "" // GitHub doesn't provide checksums, would need to be stored separately
                        };
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching GitHub release");
                return null;
            }
        }

        private class GitHubRelease
        {
            public string? TagName { get; set; }
            public string? Name { get; set; }
            public string? Body { get; set; }
            public bool Draft { get; set; }
            public bool Prerelease { get; set; }
            public GitHubAsset[]? Assets { get; set; }
        }

        private class GitHubAsset
        {
            public string? Name { get; set; }
            public string? BrowserDownloadUrl { get; set; }
            public long? Size { get; set; }
        }

        private class UpdateInfo
        {
            public string VersionName { get; set; } = "";
            public int VersionCode { get; set; }
            public string DownloadUrl { get; set; } = "";
            public string ReleaseNotes { get; set; } = "";
            public bool Mandatory { get; set; }
            public long FileSize { get; set; }
            public string Checksum { get; set; } = "";
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