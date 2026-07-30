using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lanflix.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.AppUpdate;

public class ReleaseMetadataService : IReleaseMetadataService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ReleaseMetadataService> _logger;
    private readonly IHostEnvironment _environment;
    private readonly string _githubRepo;

    private (AppReleaseMetadata? Metadata, DateTime CachedAt)? _cachedAppRelease;
    private (ServerUpdateInfo? Metadata, DateTime CachedAt)? _cachedServerRelease;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    public ReleaseMetadataService(
        IHttpClientFactory httpClientFactory,
        ILogger<ReleaseMetadataService> logger,
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient("GitHubClient");
        _logger = logger;
        _environment = environment;
        _githubRepo = configuration["Lanflix:GitHubRepo"] ?? "JarlTheGamer/LanFlix";
    }

    public async Task<AppReleaseMetadata?> GetLatestAppReleaseAsync(int currentVersionCode, CancellationToken cancellationToken = default)
    {
        var release = await FetchFromGitHubAsync(cancellationToken);
        if (release != null)
        {
            var apkAsset = release.Assets?.FirstOrDefault(a => a.Name?.EndsWith(".apk", StringComparison.OrdinalIgnoreCase) == true);
            if (apkAsset != null && !string.IsNullOrWhiteSpace(release.TagName))
            {
                var versionName = release.TagName.TrimStart('v', 'V');
                var versionCode = ParseVersionCode(versionName);

                var metadata = new AppReleaseMetadata
                {
                    VersionName = versionName,
                    VersionCode = versionCode,
                    DownloadUrl = apkAsset.BrowserDownloadUrl ?? string.Empty,
                    ReleaseNotes = release.Body ?? "Bug fixes and improvements",
                    Mandatory = false,
                    FileSize = apkAsset.Size ?? 0L,
                    Checksum = string.Empty
                };

                _cachedAppRelease = (metadata, DateTime.UtcNow);
                return metadata;
            }
        }

        // Fallback to local manifest
        var localMetadata = await ReadLocalAppManifestAsync(cancellationToken);
        if (localMetadata != null)
        {
            _cachedAppRelease = (localMetadata, DateTime.UtcNow);
            return localMetadata;
        }

        return null;
    }

    public async Task<ServerUpdateInfo?> GetLatestServerReleaseAsync(string currentVersion, CancellationToken cancellationToken = default)
    {
        var release = await FetchFromGitHubAsync(cancellationToken);
        if (release != null)
        {
            var latestVersion = !string.IsNullOrWhiteSpace(release.TagName) ? release.TagName.TrimStart('v', 'V') : currentVersion;
            var platform = OperatingSystem.IsWindows() ? "win-x64" : (OperatingSystem.IsLinux() ? "linux-x64" : "osx-x64");
            
            var asset = release.Assets?.FirstOrDefault(a =>
                a.Name != null &&
                (a.Name.Contains("server", StringComparison.OrdinalIgnoreCase) || a.Name.Contains(platform, StringComparison.OrdinalIgnoreCase)) &&
                (a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) || a.Name.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase)))
                ?? release.Assets?.FirstOrDefault(a => a.Name != null && a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));

            var downloadUrl = asset?.BrowserDownloadUrl ?? release.ZipballUrl ?? string.Empty;

            if (!string.IsNullOrEmpty(downloadUrl))
            {
                var isNewer = CompareVersions(latestVersion, currentVersion) > 0;
                var info = new ServerUpdateInfo
                {
                    Version = latestVersion,
                    CurrentVersion = currentVersion,
                    ReleaseDate = release.PublishedAt ?? DateTime.UtcNow,
                    DownloadUrl = downloadUrl,
                    FileSize = asset?.Size ?? 0L,
                    ReleaseNotes = release.Body ?? "Server update available",
                    IsUpdateAvailable = isNewer
                };

                _cachedServerRelease = (info, DateTime.UtcNow);
                return info;
            }
        }

        // Fallback to local server manifest
        var localServerInfo = await ReadLocalServerManifestAsync(currentVersion, cancellationToken);
        if (localServerInfo != null)
        {
            _cachedServerRelease = (localServerInfo, DateTime.UtcNow);
            return localServerInfo;
        }

        return null;
    }

    private async Task<GitHubReleaseDto?> FetchFromGitHubAsync(CancellationToken cancellationToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{_githubRepo}/releases/latest");
            request.Headers.UserAgent.ParseAdd("Lanflix-Server");
            request.Headers.Accept.ParseAdd("application/vnd.github.v3+json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<GitHubReleaseDto>(cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch GitHub releases from repo {Repo}", _githubRepo);
        }

        return null;
    }

    private async Task<AppReleaseMetadata?> ReadLocalAppManifestAsync(CancellationToken cancellationToken)
    {
        try
        {
            var manifestPath = Path.Combine(_environment.ContentRootPath, "releases", "app-manifest.json");
            if (File.Exists(manifestPath))
            {
                var json = await File.ReadAllTextAsync(manifestPath, cancellationToken);
                return JsonSerializer.Deserialize<AppReleaseMetadata>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading local app manifest");
        }
        return null;
    }

    private async Task<ServerUpdateInfo?> ReadLocalServerManifestAsync(string currentVersion, CancellationToken cancellationToken)
    {
        try
        {
            var manifestPath = Path.Combine(_environment.ContentRootPath, "releases", "server-manifest.json");
            if (File.Exists(manifestPath))
            {
                var json = await File.ReadAllTextAsync(manifestPath, cancellationToken);
                var info = JsonSerializer.Deserialize<ServerUpdateInfo>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (info != null)
                {
                    info.CurrentVersion = currentVersion;
                    info.IsUpdateAvailable = CompareVersions(info.Version, currentVersion) > 0;
                    return info;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading local server manifest");
        }
        return null;
    }

    private static int ParseVersionCode(string version)
    {
        var parts = version.Split('.');
        var major = parts.Length > 0 && int.TryParse(parts[0], out var m) ? m : 1;
        var minor = parts.Length > 1 && int.TryParse(parts[1], out var n) ? n : 0;
        return (major * 10) + minor;
    }

    private static int CompareVersions(string v1, string v2)
    {
        var v1Parts = v1.Split('.').Select(p => int.TryParse(p, out var n) ? n : 0).ToArray();
        var v2Parts = v2.Split('.').Select(p => int.TryParse(p, out var n) ? n : 0).ToArray();

        for (int i = 0; i < Math.Max(v1Parts.Length, v2Parts.Length); i++)
        {
            var p1 = i < v1Parts.Length ? v1Parts[i] : 0;
            var p2 = i < v2Parts.Length ? v2Parts[i] : 0;
            if (p1 > p2) return 1;
            if (p1 < p2) return -1;
        }

        return 0;
    }

    private class GitHubReleaseDto
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("published_at")]
        public DateTime? PublishedAt { get; set; }

        [JsonPropertyName("zipball_url")]
        public string? ZipballUrl { get; set; }

        [JsonPropertyName("assets")]
        public List<GitHubAssetDto>? Assets { get; set; }
    }

    private class GitHubAssetDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("browser_download_url")]
        public string? BrowserDownloadUrl { get; set; }

        [JsonPropertyName("size")]
        public long? Size { get; set; }
    }
}
