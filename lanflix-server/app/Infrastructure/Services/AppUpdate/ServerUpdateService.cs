using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
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
    private readonly IReleaseMetadataService _releaseMetadataService;
    private readonly string _currentVersion;
    private readonly HttpClient _httpClient;

    public ServerUpdateService(
        IConfiguration configuration,
        ILogger<ServerUpdateService> logger,
        IHostApplicationLifetime lifetime,
        IHttpClientFactory httpClientFactory,
        IReleaseMetadataService releaseMetadataService)
    {
        _configuration = configuration;
        _logger = logger;
        _lifetime = lifetime;
        _releaseMetadataService = releaseMetadataService;
        _httpClient = httpClientFactory.CreateClient();
        _currentVersion = GetCurrentVersion();
    }

    public string GetCurrentVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version?.ToString(3) ?? "1.2.6";
    }

    public async Task<ServerUpdateInfo?> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Checking for server updates. Current version: {Version}", _currentVersion);
            var updateInfo = await _releaseMetadataService.GetLatestServerReleaseAsync(_currentVersion, cancellationToken);
            
            if (updateInfo != null && updateInfo.IsUpdateAvailable)
            {
                return updateInfo;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for server updates");
            return null;
        }
    }

    private readonly UpdateProgressStatus _progress = new();

    public UpdateProgressStatus GetUpdateProgress()
    {
        lock (_progress)
        {
            return new UpdateProgressStatus
            {
                Status = _progress.Status,
                Percentage = _progress.Percentage,
                Message = _progress.Message,
                BytesDownloaded = _progress.BytesDownloaded,
                TotalBytes = _progress.TotalBytes
            };
        }
    }

    private void UpdateProgress(string status, int percentage, string message, long downloaded = 0, long total = 0)
    {
        lock (_progress)
        {
            _progress.Status = status;
            _progress.Percentage = percentage;
            _progress.Message = message;
            if (downloaded > 0) _progress.BytesDownloaded = downloaded;
            if (total > 0) _progress.TotalBytes = total;
        }
    }

    public async Task<bool> DownloadAndApplyUpdateAsync(
        string downloadUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Downloading update from: {Url}", downloadUrl);
            UpdateProgress("Downloading", 5, "Connecting to GitHub...");

            var updateDir = Path.Combine(Path.GetTempPath(), "lanflix-update");
            if (Directory.Exists(updateDir))
            {
                Directory.Delete(updateDir, true);
            }
            Directory.CreateDirectory(updateDir);

            var downloadPath = Path.Combine(updateDir, "update.zip");

            using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                response.EnsureSuccessStatusCode();
                var totalBytes = response.Content.Headers.ContentLength ?? 0L;
                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None);

                var buffer = new byte[8192];
                long totalDownloaded = 0;
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                    totalDownloaded += bytesRead;

                    var percentage = totalBytes > 0
                        ? (int)((totalDownloaded * 60) / totalBytes) + 5
                        : 30;

                    UpdateProgress("Downloading", percentage, $"Downloaded {totalDownloaded / 1024 / 1024} MB", totalDownloaded, totalBytes);
                }
            }

            _logger.LogInformation("Update downloaded successfully to {Path}", downloadPath);
            UpdateProgress("Extracting", 70, "Unpacking update archive...");

            var extractPath = Path.Combine(updateDir, "extracted");
            Directory.CreateDirectory(extractPath);
            ZipFile.ExtractToDirectory(downloadPath, extractPath, true);

            _logger.LogInformation("Update extracted successfully to {Path}", extractPath);
            UpdateProgress("Applying", 90, "Restarting Lanflix Server...");

            var currentDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var scriptPath = CreateUpdateScript(currentDir, extractPath);

            _logger.LogInformation("Starting update script at {ScriptPath}. Server restarting...", scriptPath);
            UpdateProgress("Complete", 100, "Update complete! Restarting...");

            var processInfo = new ProcessStartInfo
            {
                FileName = scriptPath,
                UseShellExecute = true,
                CreateNoWindow = false
            };

            Process.Start(processInfo);

            await Task.Delay(1000, cancellationToken);
            _lifetime.StopApplication();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying server update");
            UpdateProgress("Failed", 0, $"Update failed: {ex.Message}");
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
            Process.Start("chmod", $"+x {scriptPath}")?.WaitForExit();
        }

        return scriptPath;
    }
}
