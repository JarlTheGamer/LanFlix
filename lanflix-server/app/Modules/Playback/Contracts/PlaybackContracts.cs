namespace Lanflix.Modules.Playback;

public sealed record PlaybackSource(
    int Id, int ContentId, string Kind, string Title, string FilePath, string MimeType, long FileSize,
    int? SeasonNumber, int? EpisodeNumber, double? IntroStartSeconds,
    double? IntroEndSeconds, double? CreditsStartSeconds, double DurationSeconds);

public sealed record PlaybackInfoDto(
    int Id, string Kind, string Title, string StreamUrl, string MimeType, long FileSize,
    int? SeasonNumber, int? EpisodeNumber, double? IntroStartSeconds,
    double? IntroEndSeconds, double? CreditsStartSeconds, PlaybackProgressDto? Progress,
    double DurationSeconds = 0, string PlaybackMode = "Unknown",
    string PlaybackReason = "", bool SupportsSeeking = true,
    bool TranscodesVideo = false, bool TranscodesAudio = false,
    IReadOnlyList<Lanflix.Modules.Subtitles.SubtitleTrackDto>? Subtitles = null);

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

public sealed record AdaptivePlaybackPlan(
    string Method, string Reason, double DurationSeconds, string ContentType,
    bool SupportsSeeking, bool TranscodesVideo, bool TranscodesAudio);

public sealed record AdaptivePlaybackManifest(string SessionId, string Content);

public sealed record AdaptivePlaybackSegment(string FilePath, string ContentType);

public sealed record PlaybackSessionDiagnosticsDto(
    string Id, string ClientType, string Method, string Reason,
    DateTime CreatedAtUtc, DateTime LastAccessUtc, int SegmentCount,
    int CachedSegments, bool FfmpegRunning);

public interface IAdaptivePlaybackService
{
    Task<AdaptivePlaybackPlan> GetPlanAsync(
        PlaybackSource source, string clientType, CancellationToken cancellationToken);

    Task<AdaptivePlaybackDelivery> OpenAsync(
        PlaybackSource source, string clientType, double? startSeconds, string? rangeHeader, CancellationToken cancellationToken);

    Task<AdaptivePlaybackManifest> GetManifestAsync(
        PlaybackSource source, string clientType, CancellationToken cancellationToken);

    Task<AdaptivePlaybackSegment?> OpenSessionSegmentAsync(
        string sessionId, int segmentIndex, CancellationToken cancellationToken);

    Task StopSessionAsync(string sessionId, CancellationToken cancellationToken);

    IReadOnlyList<PlaybackSessionDiagnosticsDto> GetSessionDiagnostics();

    /// <summary>Returns the duration in seconds of the media file, probing via ffprobe if needed.</summary>
    Task<double> ProbeDurationAsync(string filePath, CancellationToken cancellationToken);
}
