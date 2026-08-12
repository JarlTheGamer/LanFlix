using Lanflix.Application.Common.Interfaces;
using Lanflix.Domain.ValueObjects;
using Lanflix.Infrastructure.Services.Playback.Planning;
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
    ManagedTranscodeSessionManager sessions) : IAdaptivePlaybackService
{
    public async Task<AdaptivePlaybackPlan> GetPlanAsync(
        PlaybackSource source, string clientType, CancellationToken cancellationToken)
    {
        var plan = await CreatePlanAsync(source, clientType, cancellationToken);
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
        var stream = new FileStream(source.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            1024 * 128, FileOptions.Asynchronous | FileOptions.SequentialScan);
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

    public async Task<AdaptivePlaybackSegment?> OpenSessionSegmentAsync(
        string sessionId, int segmentIndex, CancellationToken cancellationToken)
    {
        var path = await sessions.GetSegmentAsync(sessionId, segmentIndex, cancellationToken);
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

    private static AdaptivePlaybackPlan ToContract(PlaybackPlan plan) => new(
        plan.Method.ToString(), plan.Reason, plan.Media.Duration.TotalSeconds,
        plan.Method == PlannedPlaybackMethod.DirectPlay ? "video/*" : "application/vnd.apple.mpegurl",
        true, plan.TranscodesVideo, plan.TranscodesAudio);
}
