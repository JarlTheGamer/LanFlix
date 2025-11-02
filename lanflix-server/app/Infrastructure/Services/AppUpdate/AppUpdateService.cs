using System.Security.Cryptography;
using System.Text.Json;
using Lanflix.Application.Common.DTOs;
using Lanflix.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.AppUpdate;

public class AppUpdateService : IAppUpdateService
{
    private readonly string _apkStoragePath;
    private readonly string _metadataPath;
    private readonly ILogger<AppUpdateService> _logger;

    public AppUpdateService(IConfiguration configuration, ILogger<AppUpdateService> logger)
    {
        _apkStoragePath = configuration["Lanflix:AppUpdates:ApkStoragePath"] 
            ?? Path.Combine(Directory.GetCurrentDirectory(), "AppUpdates", "Android");
        _metadataPath = Path.Combine(_apkStoragePath, "metadata.json");
        _logger = logger;

        // Ensure storage directory exists
        Directory.CreateDirectory(_apkStoragePath);
    }

    public async Task<AppUpdateInfo?> GetLatestVersionAsync(
        string platform,
        string currentVersion,
        string architecture,
        CancellationToken cancellationToken = default)
    {
        if (platform.ToLower() != "android")
        {
            return null;
        }

        var metadata = await LoadMetadataAsync(cancellationToken);
        
        // Filter by architecture and get the latest version
        var latestRelease = metadata
            .Where(m => m.Architecture == architecture)
            .OrderByDescending(m => m.VersionCode)
            .FirstOrDefault();

        if (latestRelease == null)
        {
            _logger.LogWarning("No releases found for architecture {Architecture}", architecture);
            return null;
        }

        // Check if update is available
        if (CompareVersions(latestRelease.Version, currentVersion) <= 0)
        {
            _logger.LogInformation(
                "Current version {CurrentVersion} is up to date (latest: {LatestVersion})",
                currentVersion, latestRelease.Version);
            return null;
        }

        return latestRelease;
    }

    public Task<string?> GetApkPathAsync(
        string version,
        string architecture,
        CancellationToken cancellationToken = default)
    {
        var fileName = $"lanflix-{version}-{architecture}.apk";
        var filePath = Path.Combine(_apkStoragePath, fileName);

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("APK file not found: {FilePath}", filePath);
            return Task.FromResult<string?>(null);
        }

        return Task.FromResult<string?>(filePath);
    }

    public async Task<AppUpdateInfo> SaveReleaseAsync(
        Stream apkStream,
        AppReleaseInfo releaseInfo,
        CancellationToken cancellationToken = default)
    {
        var fileName = $"lanflix-{releaseInfo.Version}-{releaseInfo.Architecture}.apk";
        var filePath = Path.Combine(_apkStoragePath, fileName);

        // Save APK file
        using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
            await apkStream.CopyToAsync(fileStream, cancellationToken);
        }

        // Calculate file size and checksum
        var fileInfo = new FileInfo(filePath);
        var checksum = await CalculateSha256Async(filePath, cancellationToken);

        // Create update info
        var updateInfo = new AppUpdateInfo
        {
            Version = releaseInfo.Version,
            VersionCode = releaseInfo.VersionCode,
            ReleaseDate = DateTime.UtcNow,
            DownloadUrl = $"/api/app-updates/android/download/{releaseInfo.Version}/{releaseInfo.Architecture}",
            FileSize = fileInfo.Length,
            Sha256Checksum = checksum,
            ReleaseNotes = releaseInfo.ReleaseNotes,
            IsForceUpdate = releaseInfo.IsForceUpdate,
            MinimumSupportedVersion = releaseInfo.MinimumSupportedVersion,
            Architecture = releaseInfo.Architecture
        };

        // Update metadata
        var metadata = await LoadMetadataAsync(cancellationToken);
        
        // Remove existing entry for this version and architecture
        metadata.RemoveAll(m => m.Version == releaseInfo.Version && m.Architecture == releaseInfo.Architecture);
        
        // Add new entry
        metadata.Add(updateInfo);
        
        // Save metadata
        await SaveMetadataAsync(metadata, cancellationToken);

        _logger.LogInformation(
            "Saved APK release: {Version} ({Architecture}), Size: {Size} bytes",
            releaseInfo.Version, releaseInfo.Architecture, fileInfo.Length);

        return updateInfo;
    }

    public async Task<List<AppUpdateInfo>> GetVersionHistoryAsync(
        string platform,
        CancellationToken cancellationToken = default)
    {
        if (platform.ToLower() != "android")
        {
            return new List<AppUpdateInfo>();
        }

        var metadata = await LoadMetadataAsync(cancellationToken);
        return metadata.OrderByDescending(m => m.VersionCode).ToList();
    }

    private async Task<List<AppUpdateInfo>> LoadMetadataAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_metadataPath))
        {
            return new List<AppUpdateInfo>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_metadataPath, cancellationToken);
            return JsonSerializer.Deserialize<List<AppUpdateInfo>>(json) ?? new List<AppUpdateInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading metadata from {Path}", _metadataPath);
            return new List<AppUpdateInfo>();
        }
    }

    private async Task SaveMetadataAsync(List<AppUpdateInfo> metadata, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(_metadataPath, json, cancellationToken);
    }

    private async Task<string> CalculateSha256Async(string filePath, CancellationToken cancellationToken)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, true);
        using var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(stream, cancellationToken);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private int CompareVersions(string version1, string version2)
    {
        var v1Parts = version1.Split('.').Select(int.Parse).ToArray();
        var v2Parts = version2.Split('.').Select(int.Parse).ToArray();

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
