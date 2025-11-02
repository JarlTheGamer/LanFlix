using Lanflix.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.BackgroundJobs;

/// <summary>
/// Background service that runs library scans periodically
/// </summary>
public class LibraryScanJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LibraryScanJob> _logger;
    private readonly TimeSpan _scanInterval = TimeSpan.FromHours(6); // Run every 6 hours

    public LibraryScanJob(
        IServiceProvider serviceProvider,
        ILogger<LibraryScanJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Library scan background job started");

        // Wait 5 minutes after startup before first scan
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunLibraryScan(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during scheduled library scan");
            }

            // Wait for next scan interval
            await Task.Delay(_scanInterval, stoppingToken);
        }
    }

    private async Task RunLibraryScan(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting scheduled library scan");

            using var scope = _serviceProvider.CreateScope();
            var libraryService = scope.ServiceProvider.GetRequiredService<ILibraryService>();

            var result = await libraryService.ScanLibraryAsync(cancellationToken);

            _logger.LogInformation("Scheduled library scan completed: {Added} added, {Updated} updated, {Removed} removed, {Errors} errors",
                result.Added, result.Updated, result.Removed, result.Errors.Count);

            if (result.Errors.Count > 0)
            {
                _logger.LogWarning("Library scan had {ErrorCount} errors: {Errors}",
                    result.Errors.Count, string.Join("; ", result.Errors));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scheduled library scan failed");
        }
    }
}