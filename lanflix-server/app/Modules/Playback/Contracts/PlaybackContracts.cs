namespace Lanflix.Modules.Playback;

public sealed record PlaybackSource(
    int Id, string Kind, string Title, string FilePath, string MimeType, long FileSize,
    int? SeasonNumber, int? EpisodeNumber, double? IntroStartSeconds,
    double? IntroEndSeconds, double? CreditsStartSeconds);

public sealed record PlaybackInfoDto(
    int Id, string Kind, string Title, string StreamUrl, string MimeType, long FileSize,
    int? SeasonNumber, int? EpisodeNumber, double? IntroStartSeconds,
    double? IntroEndSeconds, double? CreditsStartSeconds, PlaybackProgressDto? Progress);

public sealed record PlaybackProgressDto(
    string MediaKind, int MediaId, long PositionMilliseconds, long DurationMilliseconds,
    double Percentage, bool Completed, DateTime UpdatedAtUtc);

public sealed record UpdatePlaybackProgressRequest(
    long PositionMilliseconds, long DurationMilliseconds, bool Completed = false);
public sealed record PlaybackDownloadManifestDto(
    int Id, string Kind, string Title, long FileSize, string MimeType, string Sha256,
    string DownloadUrl, DateTime LastModifiedUtc);

public interface IPlaybackSourceCatalog
{
    Task<PlaybackSource?> FindAsync(string kind, int id, CancellationToken cancellationToken);
}

public sealed record AdaptivePlaybackDelivery(
    Stream Stream, string ContentType, long? ContentLength, bool SupportsRanges,
    long? RangeStart, long? RangeEnd, string Mode);

public interface IAdaptivePlaybackService
{
    Task<AdaptivePlaybackDelivery> OpenAsync(
        PlaybackSource source, string clientType, double? startSeconds, string? rangeHeader, CancellationToken cancellationToken);
}
