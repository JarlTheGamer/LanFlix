using Lanflix.Application.Common.Interfaces;
using Lanflix.Domain.ValueObjects;
using Lanflix.Infrastructure.Services.Playback.Planning;
using Lanflix.Infrastructure.Services.Playback.Ffmpeg;
using Lanflix.Infrastructure.Services.Playback;
using Lanflix.Infrastructure.Services.Playback.Sessions;
using Lanflix.Infrastructure.Services.Settings;
using Lanflix.Modules.Playback;

namespace Lanflix.Infrastructure.Adapters.Playback;

/// <summary>
/// v2 playback adapter. Planning and delivery are intentionally separate:
/// direct media is served by ASP.NET range responses; converted media is served
/// by managed, cacheable FFmpeg sessions.
/// </summary>
internal sealed class AdaptivePlaybackService(
    IMediaAnalyzer analyzer,
    IHardwareAccelerationDetector hardware,
    TranscodingSettingsProvider settings,
    PlaybackPlanner planner,
    MatroskaSeekIndexInspector matroskaSeekIndex,
    ManagedTranscodeSessionManager sessions) : IAdaptivePlaybackService
{
    public async Task<AdaptivePlaybackPlan> GetPlanAsync(
        PlaybackSource source, string clientType, CancellationToken cancellationToken)
    {
        var plan = await CreatePlanAsync(source, clientType, cancellationToken);
        if (plan.Method == PlannedPlaybackMethod.DirectPlay && GetMatroskaPatch(source, clientType) is not null)
            return new AdaptivePlaybackPlan("DirectPlay", "Virtual Matroska seek-index compatibility for Android Media3",
                plan.Media.Duration.TotalSeconds, "video/x-matroska", true, false, false);
        return ToContract(plan);
    }

    public async Task<AdaptivePlaybackDelivery> OpenAsync(
        PlaybackSource source,
        string clientType,
        double? startSeconds,
        string? rangeHeader,
        CancellationToken cancellationToken)
    {
        var plan = await CreatePlanAsync(source, clientType, cancellationToken);
        if (plan.Method != PlannedPlaybackMethod.DirectPlay)
            throw new InvalidOperationException("Converted playback must use a managed HLS session.");
        Stream stream = new FileStream(source.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            1024 * 128, FileOptions.Asynchronous | FileOptions.RandomAccess);
        var patch = GetMatroskaPatch(source, clientType);
        if (patch is not null)
            stream = new VirtualMatroskaSeekStream(stream, patch);
        return new AdaptivePlaybackDelivery(stream, source.MimeType, stream.Length, true, null, null, "DirectPlay");
    }

    public async Task<AdaptivePlaybackManifest> GetManifestAsync(
        PlaybackSource source, string clientType, CancellationToken cancellationToken)
    {
        var plan = await CreatePlanAsync(source, clientType, cancellationToken);
        if (plan.Method == PlannedPlaybackMethod.DirectPlay)
            throw new InvalidOperationException("Direct-play media does not require an HLS manifest.");
        var session = sessions.GetOrCreate(source.FilePath, clientType, plan);
        return new AdaptivePlaybackManifest(session.Id, session.Manifest);
    }

    public Task<AdaptivePlaybackRendition?> GetSessionRenditionAsync(
        string sessionId, string rendition, int? audioStreamIndex, CancellationToken cancellationToken)
    {
        var kind = ParseRendition(rendition);
        var manifest = kind is null ? null : sessions.GetRendition(sessionId, kind.Value, audioStreamIndex);
        return Task.FromResult(manifest is null ? null : new AdaptivePlaybackRendition(manifest.Content));
    }

    public async Task<AdaptivePlaybackSegment?> OpenSessionSegmentAsync(
        string sessionId, string rendition, int? audioStreamIndex, int segmentIndex, CancellationToken cancellationToken)
    {
        var kind = ParseRendition(rendition);
        if (kind is null) return null;
        var path = await sessions.GetSegmentAsync(sessionId, kind.Value, audioStreamIndex, segmentIndex, cancellationToken);
        return path is null ? null : new AdaptivePlaybackSegment(path, "video/mp2t");
    }

    public Task StopSessionAsync(string sessionId, CancellationToken cancellationToken)
        => sessions.StopAsync(sessionId);

    public IReadOnlyList<PlaybackSessionDiagnosticsDto> GetSessionDiagnostics()
        => sessions.GetDiagnostics().Select(session => new PlaybackSessionDiagnosticsDto(
            session.Id, session.ClientType, session.Method, session.Reason,
            session.CreatedAtUtc, session.LastAccessUtc, session.SegmentCount,
            session.CachedSegments, session.FfmpegRunning)).ToArray();

    public async Task<double> ProbeDurationAsync(string filePath, CancellationToken cancellationToken)
        => (await analyzer.AnalyzeAsync(filePath, cancellationToken)).Duration.TotalSeconds;

    private async Task<PlaybackPlan> CreatePlanAsync(
        PlaybackSource source, string clientType, CancellationToken cancellationToken)
    {
        var media = await analyzer.AnalyzeAsync(source.FilePath, cancellationToken);
        var detectedHardware = await hardware.DetectAsync();
        var transcodingSettings = await settings.GetSettingsAsync();
        return planner.Plan(media, clientType, detectedHardware, transcodingSettings);
    }

    private MatroskaSeekIndexPatch? GetMatroskaPatch(PlaybackSource source, string clientType) =>
        clientType.StartsWith("android-v1|", StringComparison.OrdinalIgnoreCase) &&
        Path.GetExtension(source.FilePath).Equals(".mkv", StringComparison.OrdinalIgnoreCase)
            ? matroskaSeekIndex.GetVirtualPatch(source.FilePath)
            : null;

    private static AdaptivePlaybackPlan ToContract(PlaybackPlan plan) => new(
        plan.Method.ToString(), plan.Reason, plan.Media.Duration.TotalSeconds,
        plan.Method == PlannedPlaybackMethod.DirectPlay ? "video/*" : "application/vnd.apple.mpegurl",
        true, plan.TranscodesVideo, plan.TranscodesAudio);

    private static HlsSegmentKind? ParseRendition(string rendition) => rendition.ToLowerInvariant() switch
    {
        "video" => HlsSegmentKind.Video,
        "audio" => HlsSegmentKind.Audio,
        _ => null
    };
}
