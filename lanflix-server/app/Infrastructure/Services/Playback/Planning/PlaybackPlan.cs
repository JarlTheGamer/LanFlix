using Lanflix.Domain.ValueObjects;

namespace Lanflix.Infrastructure.Services.Playback.Planning;

internal enum PlannedPlaybackMethod
{
    DirectPlay,
    Remux,
    DirectStream,
    Transcode
}

internal sealed record PlaybackPlan(
    PlannedPlaybackMethod Method,
    string Reason,
    MediaInfo Media,
    string OutputVideoCodec,
    string OutputAudioCodec,
    int Width,
    int Height,
    long VideoBitrate,
    long AudioBitrate,
    int? AudioStreamIndex,
    bool ToneMap,
    HwAccelMethod HardwareAcceleration)
{
    public bool TranscodesVideo => Method == PlannedPlaybackMethod.Transcode;
    public bool TranscodesAudio => Method is PlannedPlaybackMethod.DirectStream or PlannedPlaybackMethod.Transcode;
}

internal sealed record PlaybackCapabilityProfile(
    string Id,
    IReadOnlySet<string> Containers,
    IReadOnlySet<string> VideoCodecs,
    IReadOnlySet<string> AudioCodecs,
    int MaxWidth,
    int MaxHeight,
    long MaxBitrate,
    int MaxAudioChannels,
    string? PreferredAudioLanguage,
    bool SupportsHdr,
    IReadOnlySet<string> HdrFormats,
    bool ForceTranscode);
