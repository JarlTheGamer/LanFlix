using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using Lanflix.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServerUpdateInfo = Lanflix.Application.Common.Interfaces.ServerUpdateInfo;

namespace Lanflix.Infrastructure.Services.AppUpdate;

public class ServerUpdateService : IServerUpdateService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ServerUpdateService> _logger;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly string _updateCheckUrl;
    private readonly string _currentVersion;
    private readonly bool _autoUpdateEnabled;
    private readonly HttpClient _httpClient;

    public ServerUpdateService(
        IConfiguration configuration,
        ILogger<ServerUpdateService> logger,
        IHostApplicationLifetime lifetime,
        IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _lifetime = lifetime;
        _httpClient = httpClientFactory.CreateClient();
        
        _currentVersion = GetCurrentVersion();
        _autoUpdateEnabled = configuration.GetValue<bool>("Lanflix:ServerUpdates:EnableAutoUpdate", false);
        _updateCheckUrl = configuration["Lanflix:ServerUpdates:UpdateCheckUrl"] 
            ?? "https://api.github.com/repos/YOUR_REPO/releases/latest";
    }

    public string GetCurrentVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version?.ToString(3) ?? "1.0.0";
    }

    public async Task<ServerUpdateInfo?> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Checking for server updates. Current version: {Version}", _currentVersion);

            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Lanflix-Server");
            var response = await _httpClient.GetAsync(_updateCheckUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to check for updates: {StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var release = JsonSerializer.Deserialize<GitHubRelease>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (release == null)
            {
                _logger.LogWarning("Failed to parse release information");
                return null;
            }

            var latestVersion = release.TagName?.TrimStart('v') ?? release.Name?.TrimStart('v') ?? "0.0.0";

            if (CompareVersions(latestVersion, _currentVersion) <= 0)
            {
                _logger.LogInformation("Server is up to date");
                return null;
            }

            // Find the appropriate asset for the current platform
            var platform = GetPlatformIdentifier();
            var asset = release.Assets?.FirstOrDefault(a => 
                a.Name.Contains(platform, StringComparison.OrdinalIgnoreCase) &&
                (a.Name.EndsWith(".zip") || a.Name.EndsWith(".tar.gz")));

            if (asset == null)
            {
                _logger.LogWarning("No compatible update package found for platform: {Platform}", platform);
                return null;
            }

            return new ServerUpdateInfo
            {
                Version = latestVersion,
                CurrentVersion = _currentVersion,
                ReleaseDate = release.PublishedAt ?? DateTime.UtcNow,
                DownloadUrl = asset.BrowserDownloadUrl,
                FileSize = asset.Size,
                ReleaseNotes = release.Body ?? "No release notes available",
                IsUpdateAvailable = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for updates");
            return null;
        }
    }

    public async Task<bool> DownloadAndApplyUpdateAsync(
        string downloadUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Downloading update from: {Url}", downloadUrl);

            var updateDir = Path.Combine(Path.GetTempPath(), "lanflix-update");
            Directory.CreateDirectory(updateDir);

            var downloadPath = Path.Combine(updateDir, "update.zip");

            // Download the update
            using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                response.EnsureSuccessStatusCode();
                
                using var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await response.Content.CopyToAsync(fileStream, cancellationToken);
            }

            _logger.LogInformation("Update downloaded successfully");

            // Extract the update
            var extractPath = Path.Combine(updateDir, "extracted");
            Directory.CreateDirectory(extractPath);
            ZipFile.ExtractToDirectory(downloadPath, extractPath, true);

            _logger.LogInformation("Update extracted successfully");

            // Create update script
            var currentDir = AppContext.BaseDirectory;
            var scriptPath = CreateUpdateScript(currentDir, extractPath);

            _logger.LogInformation("Starting update process. Server will restart...");

            // Start the update script and exit
            var processInfo = new ProcessStartInfo
            {
                FileName = scriptPath,
                UseShellExecute = true,
                CreateNoWindow = false
            };

            Process.Start(processInfo);

            // Give the script time to start
            await Task.Delay(1000, cancellationToken);

            // Trigger graceful shutdown
            _lifetime.StopApplication();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying update");
            return false;
        }
    }

    private string CreateUpdateScript(string currentDir, string extractPath)
    {
        var isWindows = OperatingSystem.IsWindows();
        var scriptPath = Path.Combine(Path.GetTempPath(), isWindows ? "update.bat" : "update.sh");

        if (isWindows)
        {
            var script = $@"@echo off
echo Waiting for Lanflix server to stop...
timeout /t 5 /nobreak > nul

echo Backing up current version...
if exist ""{currentDir}\backup"" rmdir /s /q ""{currentDir}\backup""
mkdir ""{currentDir}\backup""
xcopy ""{currentDir}\*"" ""{currentDir}\backup\"" /E /I /Y > nul

echo Preserving database and settings...
if exist ""{currentDir}\lanflix.db"" copy ""{currentDir}\lanflix.db"" ""{currentDir}\lanflix.db.preserve"" > nul
if exist ""{currentDir}\lanflix.db-shm"" copy ""{currentDir}\lanflix.db-shm"" ""{currentDir}\lanflix.db-shm.preserve"" > nul
if exist ""{currentDir}\lanflix.db-wal"" copy ""{currentDir}\lanflix.db-wal"" ""{currentDir}\lanflix.db-wal.preserve"" > nul
if exist ""{currentDir}\appsettings.json"" copy ""{currentDir}\appsettings.json"" ""{currentDir}\appsettings.json.preserve"" > nul

echo Installing update...
xcopy ""{extractPath}\*"" ""{currentDir}\"" /E /I /Y > nul

echo Restoring database and settings...
if exist ""{currentDir}\lanflix.db.preserve"" (
    copy ""{currentDir}\lanflix.db.preserve"" ""{currentDir}\lanflix.db"" > nul
    del ""{currentDir}\lanflix.db.preserve""
)
if exist ""{currentDir}\lanflix.db-shm.preserve"" (
    copy ""{currentDir}\lanflix.db-shm.preserve"" ""{currentDir}\lanflix.db-shm"" > nul
    del ""{currentDir}\lanflix.db-shm.preserve""
)
if exist ""{currentDir}\lanflix.db-wal.preserve"" (
    copy ""{currentDir}\lanflix.db-wal.preserve"" ""{currentDir}\lanflix.db-wal"" > nul
    del ""{currentDir}\lanflix.db-wal.preserve""
)
if exist ""{currentDir}\appsettings.json.preserve"" (
    copy ""{currentDir}\appsettings.json.preserve"" ""{currentDir}\appsettings.json"" > nul
    del ""{currentDir}\appsettings.json.preserve""
)

echo Starting Lanflix server...
cd /d ""{currentDir}""
start """" ""Lanflix.WebApi.exe""

echo Update complete!
timeout /t 3 /nobreak > nul
del ""{scriptPath}""
";
            File.WriteAllText(scriptPath, script);
        }
        else
        {
            var script = $@"#!/bin/bash
echo ""Waiting for Lanflix server to stop...""
sleep 5

echo ""Backing up current version...""
rm -rf ""{currentDir}/backup""
mkdir -p ""{currentDir}/backup""
cp -r ""{currentDir}""/* ""{currentDir}/backup/"" 2>/dev/null || true

echo ""Preserving database and settings...""
[ -f ""{currentDir}/lanflix.db"" ] && cp ""{currentDir}/lanflix.db"" ""{currentDir}/lanflix.db.preserve""
[ -f ""{currentDir}/lanflix.db-shm"" ] && cp ""{currentDir}/lanflix.db-shm"" ""{currentDir}/lanflix.db-shm.preserve""
[ -f ""{currentDir}/lanflix.db-wal"" ] && cp ""{currentDir}/lanflix.db-wal"" ""{currentDir}/lanflix.db-wal.preserve""
[ -f ""{currentDir}/appsettings.json"" ] && cp ""{currentDir}/appsettings.json"" ""{currentDir}/appsettings.json.preserve""

echo ""Installing update...""
cp -r ""{extractPath}""/* ""{currentDir}/""

echo ""Restoring database and settings...""
[ -f ""{currentDir}/lanflix.db.preserve"" ] && mv ""{currentDir}/lanflix.db.preserve"" ""{currentDir}/lanflix.db""
[ -f ""{currentDir}/lanflix.db-shm.preserve"" ] && mv ""{currentDir}/lanflix.db-shm.preserve"" ""{currentDir}/lanflix.db-shm""
[ -f ""{currentDir}/lanflix.db-wal.preserve"" ] && mv ""{currentDir}/lanflix.db-wal.preserve"" ""{currentDir}/lanflix.db-wal""
[ -f ""{currentDir}/appsettings.json.preserve"" ] && mv ""{currentDir}/appsettings.json.preserve"" ""{currentDir}/appsettings.json""

echo ""Setting permissions...""
chmod +x ""{currentDir}/Lanflix.WebApi""

echo ""Starting Lanflix server...""
cd ""{currentDir}""
./Lanflix.WebApi &

echo ""Update complete!""
sleep 3
rm ""{scriptPath}""
";
            File.WriteAllText(scriptPath, script);
            
            // Make script executable on Unix
            Process.Start("chmod", $"+x {scriptPath}")?.WaitForExit();
        }

        return scriptPath;
    }

    private string GetPlatformIdentifier()
    {
        if (OperatingSystem.IsWindows())
            return "win-x64";
        if (OperatingSystem.IsLinux())
            return "linux-x64";
        if (OperatingSystem.IsMacOS())
            return "osx-x64";
        
        return "unknown";
    }

    private int CompareVersions(string version1, string version2)
    {
        var v1Parts = version1.Split('.').Select(p => int.TryParse(p, out var n) ? n : 0).ToArray();
        var v2Parts = version2.Split('.').Select(p => int.TryParse(p, out var n) ? n : 0).ToArray();

        for (int i = 0; i < Math.Max(v1Parts.Length, v2Parts.Length); i++)
        {
            var v1Part = i < v1Parts.Length ? v1Parts[i] : 0;
            var v2Part = i < v2Parts.Length ? v2Parts[i] : 0;

            if (v1Part > v2Part) return 1;
            if (v1Part < v2Part) return -1;
        }

        return 0;
    }
}

public class GitHubRelease
{
    public string? TagName { get; set; }
    public string? Name { get; set; }
    public string? Body { get; set; }
    public DateTime? PublishedAt { get; set; }
    public List<GitHubAsset>? Assets { get; set; }
}

public class GitHubAsset
{
    public string Name { get; set; } = string.Empty;
    public string BrowserDownloadUrl { get; set; } = string.Empty;
    public long Size { get; set; }
}
