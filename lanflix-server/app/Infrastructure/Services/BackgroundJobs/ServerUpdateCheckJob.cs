using Lanflix.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.BackgroundJobs;

public class ServerUpdateCheckJob : BackgroundService
{
    private readonly IServerUpdateService _updateService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ServerUpdateCheckJob> _logger;
    private readonly TimeSpan _checkInterval;

    public ServerUpdateCheckJob(
        IServerUpdateService updateService,
        IConfiguration configuration,
        ILogger<ServerUpdateCheckJob> logger)
    {
        _updateService = updateService;
        _configuration = configuration;
        _logger = logger;
        
        // Check for updates every 6 hours by default
        var intervalHours = configuration.GetValue<int>("Lanflix:ServerUpdates:CheckIntervalHours", 6);
        _checkInterval = TimeSpan.FromHours(intervalHours);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Server update check service started. Check interval: {Interval}", _checkInterval);

        // Wait 1 minute after startup before first check
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndApplyUpdatesAsync(stoppingToken);
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in server update check service");
                // Continue running despite errors
            }
        }

        _logger.LogInformation("Server update check service stopped");
    }

    private async Task CheckAndApplyUpdatesAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Running server update check");

            var updateInfo = await _updateService.CheckForUpdatesAsync(cancellationToken);

            if (updateInfo == null)
            {
                _logger.LogInformation("No server updates available");
                return;
            }

            _logger.LogInformation(
                "Server update available: {Version} (current: {CurrentVersion})",
                updateInfo.Version,
                updateInfo.CurrentVersion);

            var autoUpdate = _configuration.GetValue<bool>("Lanflix:ServerUpdates:EnableAutoUpdate", false);

            if (autoUpdate)
            {
                _logger.LogInformation("Auto-update is enabled. Downloading and applying update...");
                
                var success = await _updateService.DownloadAndApplyUpdateAsync(
                    updateInfo.DownloadUrl,
                    cancellationToken);

                if (success)
                {
                    _logger.LogInformation("Update applied successfully. Server will restart.");
                }
                else
                {
                    _logger.LogError("Failed to apply update");
                }
            }
            else
            {
                _logger.LogInformation(
                    "Auto-update is disabled. Update available but not applied. " +
                    "Enable auto-update in settings or manually update via the admin panel.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for server updates");
        }
    }
}
