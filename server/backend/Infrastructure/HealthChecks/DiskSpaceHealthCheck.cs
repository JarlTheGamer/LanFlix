using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.HealthChecks;

/// <summary>
/// Health check to verify sufficient disk space is available
/// </summary>
public class DiskSpaceHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DiskSpaceHealthCheck> _logger;
    private const long MinimumFreeSpaceBytes = 5L * 1024 * 1024 * 1024; // 5 GB

    public DiskSpaceHealthCheck(
        IConfiguration configuration,
        ILogger<DiskSpaceHealthCheck> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var mediaPaths = new List<string>();
            
            // Get media paths from configuration
            var moviesPath = _configuration["Lanflix:MediaPaths:Movies"];
            var seriesPath = _configuration["Lanflix:MediaPaths:Series"];
            var tempPath = _configuration["Lanflix:Transcoding:TempPath"];

            if (!string.IsNullOrEmpty(moviesPath)) mediaPaths.Add(moviesPath);
            if (!string.IsNullOrEmpty(seriesPath)) mediaPaths.Add(seriesPath);
            if (!string.IsNullOrEmpty(tempPath)) mediaPaths.Add(tempPath);

            var diskInfoList = new List<DiskInfo>();
            var hasLowSpace = false;
            var hasCriticalSpace = false;

            foreach (var path in mediaPaths.Distinct())
            {
                try
                {
                    if (!Directory.Exists(path))
                    {
                        _logger.LogWarning("Media path does not exist: {Path}", path);
                        continue;
                    }

                    var driveInfo = new DriveInfo(Path.GetPathRoot(path) ?? path);
                    
                    if (!driveInfo.IsReady)
                    {
                        continue;
                    }

                    var freeSpaceGB = driveInfo.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                    var totalSpaceGB = driveInfo.TotalSize / (1024.0 * 1024.0 * 1024.0);
                    var usedPercentage = ((totalSpaceGB - freeSpaceGB) / totalSpaceGB) * 100;

                    var diskInfo = new DiskInfo
                    {
                        Path = path,
                        DriveName = driveInfo.Name,
                        FreeSpaceGB = freeSpaceGB,
                        TotalSpaceGB = totalSpaceGB,
                        UsedPercentage = usedPercentage
                    };

                    diskInfoList.Add(diskInfo);

                    // Check thresholds
                    if (driveInfo.AvailableFreeSpace < MinimumFreeSpaceBytes)
                    {
                        hasCriticalSpace = true;
                    }
                    else if (usedPercentage > 90)
                    {
                        hasLowSpace = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to check disk space for path: {Path}", path);
                }
            }

            if (diskInfoList.Count == 0)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    "No valid media paths configured or accessible"));
            }

            var data = new Dictionary<string, object>();
            for (int i = 0; i < diskInfoList.Count; i++)
            {
                var info = diskInfoList[i];
                data[$"disk_{i}_path"] = info.Path;
                data[$"disk_{i}_drive"] = info.DriveName;
                data[$"disk_{i}_free_gb"] = Math.Round(info.FreeSpaceGB, 2);
                data[$"disk_{i}_total_gb"] = Math.Round(info.TotalSpaceGB, 2);
                data[$"disk_{i}_used_percent"] = Math.Round(info.UsedPercentage, 2);
            }

            if (hasCriticalSpace)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "Critical: Less than 5GB free space available on one or more drives",
                    data: data));
            }

            if (hasLowSpace)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    "Warning: Disk space usage above 90% on one or more drives",
                    data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                "Sufficient disk space available",
                data: data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Disk space health check failed");
            
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Failed to check disk space",
                ex));
        }
    }

    private class DiskInfo
    {
        public required string Path { get; init; }
        public required string DriveName { get; init; }
        public double FreeSpaceGB { get; init; }
        public double TotalSpaceGB { get; init; }
        public double UsedPercentage { get; init; }
    }
}
