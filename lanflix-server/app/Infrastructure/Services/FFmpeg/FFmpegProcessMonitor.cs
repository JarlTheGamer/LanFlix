using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.FFmpeg;

/// <summary>
/// Background service that monitors FFmpeg processes and cleans up stale ones
/// </summary>
public class FFmpegProcessMonitor : BackgroundService
{
    private readonly ILogger<FFmpegProcessMonitor> _logger;
    private readonly FFmpegProcessPool _processPool;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _staleThreshold = TimeSpan.FromSeconds(60);

    public FFmpegProcessMonitor(
        ILogger<FFmpegProcessMonitor> logger,
        FFmpegProcessPool processPool)
    {
        _logger = logger;
        _processPool = processPool;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FFmpeg process monitor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_checkInterval, stoppingToken);

                // Perform health check
                var healthCheck = await _processPool.PerformHealthCheckAsync(stoppingToken);

                if (!healthCheck.IsHealthy)
                {
                    _logger.LogWarning(
                        "Process pool health check failed. Stale processes: {StaleCount}",
                        healthCheck.StaleProcesses.Count);

                    // Clean up stale processes
                    await _processPool.CleanupStaleProcessesAsync(_staleThreshold, stoppingToken);
                }
                else
                {
                    _logger.LogDebug(
                        "Process pool healthy. Active: {Active}/{Max}, Available: {Available}",
                        healthCheck.ActiveProcessCount,
                        healthCheck.MaxProcessCount,
                        healthCheck.AvailableSlots);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FFmpeg process monitor");
            }
        }

        _logger.LogInformation("FFmpeg process monitor stopped");
    }
}
