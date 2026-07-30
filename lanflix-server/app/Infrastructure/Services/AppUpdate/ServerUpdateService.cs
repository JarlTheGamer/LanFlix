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
        try
        {
            var versionJsonPath = Path.Combine(AppContext.BaseDirectory, "version.json");
            if (!File.Exists(versionJsonPath))
            {
                versionJsonPath = Path.Combine(Directory.GetCurrentDirectory(), "version.json");
            }

            if (File.Exists(versionJsonPath))
            {
                var json = File.ReadAllText(versionJsonPath);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("serverVersion", out var verProp))
                {
                    var verStr = verProp.GetString();
                    if (!string.IsNullOrEmpty(verStr)) return verStr;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading version.json for GetCurrentVersion");
        }

        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "0.0.0";
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
            UpdateProgress("Applying", 85, "Swapping updated files in-process...");

            // Use the actual on-disk executable directory, NOT AppContext.BaseDirectory
            // (AppContext.BaseDirectory points to .NET's single-file temp extraction folder)
            var exeLocation = Environment.ProcessPath
                ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
                ?? Path.Combine(AppContext.BaseDirectory, OperatingSystem.IsWindows() ? "Lanflix.WebApi.exe" : "Lanflix.WebApi");
            var currentDir = Path.GetDirectoryName(exeLocation)
                ?? AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            
            _logger.LogInformation("Update install target directory: {Dir}", currentDir);

            // Perform in-process C# atomic file swap
            ApplyInProcessFileSwap(currentDir, extractPath);

            UpdateProgress("Complete", 100, "Update complete! Restarting server...");

            var exePath = Path.Combine(currentDir, OperatingSystem.IsWindows() ? "Lanflix.WebApi.exe" : "Lanflix.WebApi");

            if (File.Exists(exePath))
            {
                _logger.LogInformation("Relaunching process: {ExePath}", exePath);
                var startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = currentDir,
                    UseShellExecute = true
                };
                Process.Start(startInfo);
            }

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

    private void ApplyInProcessFileSwap(string targetDir, string sourceDir)
    {
        var protectedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "lanflix.db",
            "lanflix.db-shm",
            "lanflix.db-wal",
            "appsettings.json",
            "appsettings.Development.json"
        };

        foreach (var sourceFilePath in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, sourceFilePath);
            var fileName = Path.GetFileName(relativePath);

            if (protectedFiles.Contains(fileName))
            {
                continue; // Always preserve user database & configuration
            }

            var targetFilePath = Path.Combine(targetDir, relativePath);
            var targetDirectory = Path.GetDirectoryName(targetFilePath);

            if (!string.IsNullOrEmpty(targetDirectory) && !Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            try
            {
                File.Copy(sourceFilePath, targetFilePath, true);
            }
            catch (IOException)
            {
                // Locked file (e.g. running DLL or EXE) - Windows permits renaming locked files!
                var tempOldPath = targetFilePath + ".old";
                if (File.Exists(tempOldPath))
                {
                    try { File.Delete(tempOldPath); } catch { }
                }

                File.Move(targetFilePath, tempOldPath);
                File.Copy(sourceFilePath, targetFilePath, true);
            }
        }

        // Clean up any leftover .old files from this or previous updates
        foreach (var oldFile in Directory.GetFiles(targetDir, "*.old", SearchOption.AllDirectories))
        {
            try { File.Delete(oldFile); } catch { /* ignore locked files */ }
        }
    }
}
