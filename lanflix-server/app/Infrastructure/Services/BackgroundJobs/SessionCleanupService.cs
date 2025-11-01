using Lanflix.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.BackgroundJobs;

/// <summary>
/// Background service that monitors and cleans up abandoned transcoding sessions
/// </summary>
public class SessionCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SessionCleanupService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _inactivityThreshold = TimeSpan.FromSeconds(30);

    public SessionCleanupService(
        IServiceProvider serviceProvider,
        ILogger<SessionCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Session cleanup service started");

        // Perform initial cleanup of orphaned sessions (from previous server run)
        await CleanupOrphanedSessionsOnStartupAsync(stoppingToken);

        // Start periodic cleanup loop
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
                await PerformCleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during session cleanup");
                // Continue running despite errors
            }
        }

        _logger.LogInformation("Session cleanup service stopped");
    }

    private async Task CleanupOrphanedSessionsOnStartupAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var sessionManager = scope.ServiceProvider.GetRequiredService<ITranscodingSessionManager>();

            _logger.LogInformation("Performing startup cleanup of orphaned sessions");
            var cleanedCount = await sessionManager.CleanupOrphanedSessionsAsync(cancellationToken);

            if (cleanedCount > 0)
            {
                _logger.LogWarning(
                    "Cleaned up {Count} orphaned sessions from previous server run",
                    cleanedCount);
            }
            else
            {
                _logger.LogInformation("No orphaned sessions found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup orphaned sessions on startup");
        }
    }

    private async Task PerformCleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var sessionManager = scope.ServiceProvider.GetRequiredService<ITranscodingSessionManager>();

        // Clean up abandoned sessions (no activity for threshold duration)
        var cleanedCount = await sessionManager.CleanupAbandonedSessionsAsync(
            _inactivityThreshold,
            cancellationToken);

        if (cleanedCount > 0)
        {
            _logger.LogInformation(
                "Cleaned up {Count} abandoned sessions during periodic check",
                cleanedCount);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Session cleanup service is stopping");
        await base.StopAsync(cancellationToken);
    }
}
