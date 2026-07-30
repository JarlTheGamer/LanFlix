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
        var manifest = await FetchAppManifestAsync(cancellationToken);
        if (manifest != null)
            return manifest;

        // Fallback to local manifest
        return await ReadLocalAppManifestAsync(cancellationToken);
    }

    public async Task<ServerUpdateInfo?> GetLatestServerReleaseAsync(string currentVersion, CancellationToken cancellationToken = default)
    {
        var manifest = await FetchServerManifestAsync(cancellationToken);
        if (manifest != null)
        {
            manifest.CurrentVersion = currentVersion;
            manifest.IsUpdateAvailable = CompareVersions(manifest.Version, currentVersion) > 0;
            return manifest;
        }

        // Fallback to local server manifest
        return await ReadLocalServerManifestAsync(currentVersion, cancellationToken);
    }

    private async Task<ServerUpdateInfo?> FetchServerManifestAsync(CancellationToken cancellationToken)
    {
        try
        {
            var manifestUrl = $"https://raw.githubusercontent.com/{_githubRepo}/main/releases/server-manifest.json";
            var request = new HttpRequestMessage(HttpMethod.Get, manifestUrl);
            request.Headers.UserAgent.ParseAdd("Lanflix-Server");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var manifest = await response.Content.ReadFromJsonAsync<ServerUpdateInfo>(
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                    cancellationToken);
                _logger.LogDebug("Fetched server manifest from GitHub: {Url}", manifestUrl);
                return manifest;
            }
            _logger.LogWarning("Failed to fetch server manifest. Status: {Status}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch server manifest from {Repo}", _githubRepo);
        }
        return null;
    }

    private async Task<AppReleaseMetadata?> FetchAppManifestAsync(CancellationToken cancellationToken)
    {
        try
        {
            var manifestUrl = $"https://raw.githubusercontent.com/{_githubRepo}/main/releases/app-manifest.json";
            var request = new HttpRequestMessage(HttpMethod.Get, manifestUrl);
            request.Headers.UserAgent.ParseAdd("Lanflix-Server");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<AppReleaseMetadata>(
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                    cancellationToken);
            }
            _logger.LogWarning("Failed to fetch app manifest. Status: {Status}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch app manifest from {Repo}", _githubRepo);
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
