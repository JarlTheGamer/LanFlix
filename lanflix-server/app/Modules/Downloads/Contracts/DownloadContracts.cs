namespace Lanflix.Modules.Downloads;

public sealed record ServerDownloadDto(
    string Id, string Provider, string Title, string MediaKind, string Status,
    double ProgressPercentage, long TotalBytes, long DownloadedBytes,
    long? BytesPerSecond, TimeSpan? EstimatedTimeRemaining, string? Quality,
    string? ErrorMessage);

public sealed record DownloadQueueDto(
    IReadOnlyList<ServerDownloadDto> Items,
    IReadOnlyList<string> UnavailableProviders);

public sealed record CancelDownloadRequest(bool RemoveFromClient = true, bool Blocklist = false);

public interface IDownloadQueue
{
    Task<DownloadQueueDto> GetAsync(CancellationToken cancellationToken);
    Task<bool> CancelAsync(string provider, int queueId, CancelDownloadRequest request, CancellationToken cancellationToken);
}
