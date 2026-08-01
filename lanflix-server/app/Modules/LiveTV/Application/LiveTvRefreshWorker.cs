using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lanflix.Modules.LiveTV;

public static class LiveTvRegistration
{
    public static IServiceCollection AddLiveTvModule(this IServiceCollection services)
    { services.AddHostedService<LiveTvRefreshWorker>(); return services; }
}

internal sealed class LiveTvRefreshWorker(IServiceScopeFactory scopes, ILogger<LiveTvRefreshWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(30));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await using var scope = scopes.CreateAsyncScope();
                    var db = scope.ServiceProvider.GetRequiredService<ILiveTvDbContext>();
                    var catalog = scope.ServiceProvider.GetRequiredService<ILiveTvCatalog>();
                    var due = await db.LiveTvSources.AsNoTracking().Where(x => x.Enabled && (x.LastRefreshedUtc == null || x.LastRefreshedUtc < DateTime.UtcNow.AddMinutes(-30))).Select(x => x.Id).ToArrayAsync(stoppingToken);
                    foreach (var id in due) await catalog.RefreshAsync(id, stoppingToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException) { logger.LogError(exception, "Live TV refresh iteration failed"); }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        catch (Exception exception) { logger.LogError(exception, "Live TV refresh worker stopped unexpectedly"); }
    }
}
