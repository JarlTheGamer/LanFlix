using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Common.Models;
using Lanflix.Application.Features.Streaming.Services;
using Lanflix.Infrastructure.Services.Settings;
using Lanflix.Modules.Playback;

namespace Lanflix.Infrastructure.Adapters.Playback;

/// <summary>Adapts the established FFmpeg decision engine to the account-based v2 playback contract.</summary>
internal sealed class AdaptivePlaybackService(
    EnhancedStreamingService streaming,
    IMediaAnalyzer analyzer,
    IHardwareAccelerationDetector hardware,
    TranscodingSettingsProvider settings) : IAdaptivePlaybackService
{
    public async Task<string> GetPlaybackModeAsync(
        PlaybackSource source, string clientType, CancellationToken cancellationToken)
    {
        var media = await analyzer.AnalyzeAsync(source.FilePath, cancellationToken);
        var decision = streaming.GetTranscodingDecision(
            media,
            streaming.CreateDefaultProfiles(clientType),
            await hardware.DetectAsync(),
            await settings.GetSettingsAsync());
        return decision.PlaybackMethod.ToString();
    }

    public async Task<AdaptivePlaybackDelivery> OpenAsync(
        PlaybackSource source, string clientType, double? startSeconds, string? rangeHeader, CancellationToken cancellationToken)
    {
        var media = await analyzer.AnalyzeAsync(source.FilePath);
        var result = await streaming.StreamAsync(new StreamRequest
        {
            SessionId = Guid.NewGuid().ToString("N"),
            FilePath = source.FilePath,
            MediaInfo = media,
            StartPosition = startSeconds is > 0 ? startSeconds : null,
            RangeHeader = rangeHeader
        }, streaming.CreateDefaultProfiles(clientType), await hardware.DetectAsync(), await settings.GetSettingsAsync(), cancellationToken);

        return new AdaptivePlaybackDelivery(result.DataStream, result.ContentType, result.ContentLength,
            result.SupportsRangeRequests, result.RangeStart, result.RangeEnd, result.Mode.ToString());
    }

    public async Task<AdaptivePlaybackDelivery> OpenSegmentAsync(
        PlaybackSource source, string clientType, double startSeconds, double segmentDuration, CancellationToken cancellationToken)
    {
        var media = await analyzer.AnalyzeAsync(source.FilePath);

        // Force transcoding profiles appropriate for HLS MPEG-TS output
        var profiles = streaming.CreateDefaultProfiles(clientType);
        var hw = await hardware.DetectAsync();
        var cfg = await settings.GetSettingsAsync();

        var result = await streaming.StreamAsync(new StreamRequest
        {
            SessionId = Guid.NewGuid().ToString("N"),
            FilePath = source.FilePath,
            MediaInfo = media,
            StartPosition = startSeconds > 0 ? startSeconds : null,
            SegmentDuration = segmentDuration,
            RangeHeader = null,
            ForceOutputFormat = "mpegts"
        }, profiles, hw, cfg, cancellationToken);

        return new AdaptivePlaybackDelivery(result.DataStream, "video/mp2t", result.ContentLength,
            false, null, null, result.Mode.ToString());
    }

    public async Task<double> ProbeDurationAsync(string filePath, CancellationToken cancellationToken)
    {
        var media = await analyzer.AnalyzeAsync(filePath, cancellationToken);
        return media.Duration.TotalSeconds;
    }
}
