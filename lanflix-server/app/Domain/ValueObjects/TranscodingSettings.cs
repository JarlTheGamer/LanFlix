namespace Lanflix.Domain.ValueObjects;

/// <summary>
/// Server-side transcoding settings for optimal media delivery
/// </summary>
public record TranscodingSettings
{
    /// <summary>
    /// Whether hardware acceleration is enabled
    /// </summary>
    public bool EnableHardwareAcceleration { get; init; } = true;

    /// <summary>
    /// Preferred hardware acceleration method (None for auto-detect)
    /// </summary>
    public HwAccelMethod PreferredHwAccelMethod { get; init; } = HwAccelMethod.None;

    /// <summary>
    /// Number of threads to use for software encoding (0 for auto)
    /// </summary>
    public int ThreadCount { get; init; } = 0;

    /// <summary>
    /// Whether to enable tone mapping for HDR content
    /// </summary>
    public bool EnableToneMapping { get; init; } = true;

    /// <summary>
    /// Tone mapping algorithm to use
    /// </summary>
    public ToneMappingAlgorithm ToneMappingAlgorithm { get; init; } = ToneMappingAlgorithm.Hable;

    /// <summary>
    /// Whether to allow fallback to software encoding if hardware fails
    /// </summary>
    public bool AllowSoftwareFallback { get; init; } = true;

    /// <summary>
    /// Maximum number of concurrent transcoding sessions
    /// </summary>
    public int MaxConcurrentTranscodes { get; init; } = 1;

    /// <summary>
    /// Whether to enable low-power encoding modes when available
    /// </summary>
    public bool EnableLowPowerEncoding { get; init; } = false;

    /// <summary>
    /// Encoding preset for quality vs speed tradeoff
    /// </summary>
    public EncodingPreset EncodingPreset { get; init; } = EncodingPreset.Medium;

    /// <summary>
    /// Whether to enable B-frames for better compression
    /// </summary>
    public bool EnableBFrames { get; init; } = true;

    /// <summary>
    /// Target quality level (CRF for x264/x265, 0-51, lower is better)
    /// </summary>
    public int? TargetQuality { get; init; }

    /// <summary>
    /// Whether to enable adaptive bitrate streaming
    /// </summary>
    public bool EnableAdaptiveBitrate { get; init; } = true;

    /// <summary>
    /// Segment duration for HLS/DASH streaming (in seconds)
    /// </summary>
    public int SegmentDuration { get; init; } = 6;

    /// <summary>
    /// Number of segments to keep in playlist
    /// </summary>
    public int PlaylistLength { get; init; } = 6;

    /// <summary>
    /// Whether to delete transcoded segments after streaming
    /// </summary>
    public bool DeleteSegmentsAfterStreaming { get; init; } = true;

    /// <summary>
    /// Temporary directory for transcoding files
    /// </summary>
    public string? TempDirectory { get; init; }

    /// <summary>
    /// FFmpeg path (null for auto-detect)
    /// </summary>
    public string? FFmpegPath { get; init; }

    /// <summary>
    /// FFprobe path (null for auto-detect)
    /// </summary>
    public string? FFprobePath { get; init; }

    /// <summary>
    /// Whether to use seeking optimizations
    /// </summary>
    public bool EnableSeekingOptimizations { get; init; } = true;

    /// <summary>
    /// Keyframe interval for seeking optimization (in frames)
    /// </summary>
    public int SeekingKeyframeInterval { get; init; } = 30;

    /// <summary>
    /// Whether to use MPEG-TS container for better seeking support
    /// </summary>
    public bool PreferMpegTsForSeeking { get; init; } = true;
}

/// <summary>
/// Tone mapping algorithms for HDR to SDR conversion
/// </summary>
public enum ToneMappingAlgorithm
{
    None,
    Clip,
    Linear,
    Gamma,
    Reinhard,
    Hable,
    Mobius
}

/// <summary>
/// Encoding presets for quality vs speed tradeoff
/// </summary>
public enum EncodingPreset
{
    UltraFast,
    SuperFast,
    VeryFast,
    Faster,
    Fast,
    Medium,
    Slow,
    Slower,
    VerySlow
}

/// <summary>
/// Transcoding decision result with optimal playback method selection
/// </summary>
public record TranscodingDecision
{
    /// <summary>
    /// The playback method determined by the server
    /// </summary>
    public PlaybackMethod PlaybackMethod { get; init; }

    /// <summary>
    /// Reason for the transcoding decision
    /// </summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>
    /// Selected transcoding profile
    /// </summary>
    public TranscodingProfile? SelectedProfile { get; init; }

    /// <summary>
    /// Target video codec (if transcoding video)
    /// </summary>
    public string? TargetVideoCodec { get; init; }

    /// <summary>
    /// Target audio codec (if transcoding audio)
    /// </summary>
    public string? TargetAudioCodec { get; init; }

    /// <summary>
    /// Target container format
    /// </summary>
    public string? TargetContainer { get; init; }

    /// <summary>
    /// Target video bitrate
    /// </summary>
    public long? TargetVideoBitrate { get; init; }

    /// <summary>
    /// Target audio bitrate
    /// </summary>
    public long? TargetAudioBitrate { get; init; }

    /// <summary>
    /// Target resolution width
    /// </summary>
    public int? TargetWidth { get; init; }

    /// <summary>
    /// Target resolution height
    /// </summary>
    public int? TargetHeight { get; init; }

    /// <summary>
    /// Target frame rate
    /// </summary>
    public double? TargetFrameRate { get; init; }

    /// <summary>
    /// Hardware acceleration method to use
    /// </summary>
    public HwAccelMethod HwAccelMethod { get; init; } = HwAccelMethod.None;

    /// <summary>
    /// Whether tone mapping will be applied
    /// </summary>
    public bool RequiresToneMapping { get; init; }

    /// <summary>
    /// Estimated transcoding complexity (1-10, higher is more complex)
    /// </summary>
    public int TranscodingComplexity { get; init; } = 1;
}

/// <summary>
/// Playback methods for media streaming
/// </summary>
public enum PlaybackMethod
{
    /// <summary>
    /// Direct Play - No transcoding, file served as-is
    /// </summary>
    DirectPlay,

    /// <summary>
    /// Remux - Container change only, codecs preserved
    /// </summary>
    Remux,

    /// <summary>
    /// Direct Stream - Audio transcoded, video copied
    /// </summary>
    DirectStream,

    /// <summary>
    /// Transcode - Video transcoded (audio may be copied or transcoded)
    /// </summary>
    Transcode
}