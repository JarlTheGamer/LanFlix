using Lanflix.Modules.Discovery;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.Discovery;

public sealed class DiscoveryPrewarmService(
    IServiceScopeFactory scopeFactory,
    ILogger<DiscoveryPrewarmService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(2000, stoppingToken);
            logger.LogInformation("Pre-warming Discovery cache and title logos on server startup...");
            using var scope = scopeFactory.CreateScope();
            var discoveryProvider = scope.ServiceProvider.GetRequiredService<IDiscoveryProvider>();
            var page = await discoveryProvider.GetPageAsync(1, stoppingToken);
            
            // Pre-fetch title logos for all discovery items concurrently so no on-demand downloading occurs during browsing
            var allItems = page.TrendingMovies
                .Concat(page.TrendingSeries)
                .Concat(page.PopularMovies)
                .Concat(page.PopularSeries)
                .DistinctBy(x => $"{x.Type}:{x.TmdbId}");

            var logoTasks = allItems.Select(item => discoveryProvider.GetLogoUrlAsync(item.TmdbId, item.Type, stoppingToken));
            await Task.WhenAll(logoTasks);

            logger.LogInformation("Successfully pre-warmed Discovery cache and title logos for {MoviesCount} movies and {SeriesCount} series.",
                page.TrendingMovies.Count, page.TrendingSeries.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Discovery cache pre-warm skipped or encountered error: {Message}", ex.Message);
        }
    }
}
