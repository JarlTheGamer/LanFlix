using Lanflix.Application.Common.Interfaces;
using Lanflix.Modules.Downloads;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Adapters.Downloads;

internal sealed class ExternalDownloadQueue(
    IRadarrClient radarr,
    ISonarrClient sonarr,
    ILogger<ExternalDownloadQueue> logger) : IDownloadQueue
{
    public async Task<DownloadQueueDto> GetAsync(CancellationToken cancellationToken)
    {
        var items = new List<ServerDownloadDto>();
        var unavailable = new List<string>();

        try
        {
            var queue = await radarr.GetQueueAsync(1, 100, cancellationToken);
            items.AddRange(queue.Records.Select(item => new ServerDownloadDto(
                $"radarr:{item.Id}", "radarr", item.Title, "movie", NormalizeStatus(item.Status),
                Progress(item.Size, item.Sizeleft), item.Size, Math.Max(0, item.Size - item.Sizeleft),
                item.DownloadRate, ParseDuration(item.Timeleft), item.Quality?.Quality?.Name, item.ErrorMessage)));
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException)
        {
            unavailable.Add("radarr");
            logger.LogWarning(exception, "Radarr download queue is unavailable");
        }

        try
        {
            var queue = await sonarr.GetQueueAsync(1, 100, cancellationToken);
            items.AddRange(queue.Records.Select(item => new ServerDownloadDto(
                $"sonarr:{item.Id}", "sonarr", EpisodeTitle(item), "episode", NormalizeStatus(item.Status),
                Progress(item.Size, item.Sizeleft), item.Size, Math.Max(0, item.Size - item.Sizeleft),
                item.DownloadRate, ParseDuration(item.Timeleft), item.Quality?.Quality?.Name, item.ErrorMessage)));
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException)
        {
            unavailable.Add("sonarr");
            logger.LogWarning(exception, "Sonarr download queue is unavailable");
        }

        return new DownloadQueueDto(items, unavailable);
    }

    public async Task<bool> CancelAsync(
        string provider, int queueId, CancelDownloadRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (provider.Equals("radarr", StringComparison.OrdinalIgnoreCase))
                await radarr.RemoveQueueItemAsync(queueId, request.RemoveFromClient, request.Blocklist, cancellationToken);
            else if (provider.Equals("sonarr", StringComparison.OrdinalIgnoreCase))
                await sonarr.RemoveQueueItemAsync(queueId, request.RemoveFromClient, request.Blocklist, cancellationToken);
            else
                return false;
            return true;
        }
        catch (HttpRequestException exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    private static double Progress(long size, long remaining) => size <= 0
        ? 0
        : Math.Clamp((size - remaining) * 100d / size, 0, 100);

    private static TimeSpan? ParseDuration(string? value) => TimeSpan.TryParse(value, out var duration) ? duration : null;

    private static string NormalizeStatus(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "downloading" => "downloading",
        "paused" => "paused",
        "queued" => "queued",
        "completed" => "completed",
        "failed" => "failed",
        "warning" => "warning",
        _ => "unknown"
    };

    private static string EpisodeTitle(Lanflix.Application.Common.Models.SonarrQueueItem item)
    {
        var series = item.Series?.Title ?? item.Title;
        return item.Episode is null ? series : $"{series} · S{item.Episode.SeasonNumber:D2}E{item.Episode.EpisodeNumber:D2} · {item.Episode.Title}";
    }
}
