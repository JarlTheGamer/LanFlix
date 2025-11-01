using Lanflix.Domain.Enums;
using Lanflix.Domain.ValueObjects;

namespace Lanflix.Application.Common.Models;

/// <summary>
/// Request for transcoding a media file
/// </summary>
public class TranscodeRequest
{
    /// <summary>
    /// Path to the input media file
    /// </summary>
    public required string InputPath { get; init; }

    /// <summary>
    /// Streaming mode to use
    /// </summary>
    public required StreamingMode Mode { get; init; }

    /// <summary>
    /// Source media information
    /// </summary>
    public required MediaInfo SourceMedia { get; init; }

    /// <summary>
    /// Target video codec (if transcoding video)
    /// </summary>
    public string? TargetVideoCodec { get; init; }

    /// <summary>
    /// Target audio codec (if transcoding audio)
    /// </summary>
    public string? TargetAudioCodec { get; init; }

    /// <summary>
    /// Target video bitrate in bits per second
    /// </summary>
    public long? TargetVideoBitrate { get; init; }

    /// <summary>
    /// Target audio bitrate in bits per second
    /// </summary>
    public long? TargetAudioBitrate { get; init; }

    /// <summary>
    /// Target resolution width (null to keep original)
    /// </summary>
    public int? TargetWidth { get; init; }

    /// <summary>
    /// Target resolution height (null to keep original)
    /// </summary>
    public int? TargetHeight { get; init; }

    /// <summary>
    /// Start position in seconds (for seeking)
    /// </summary>
    public double? StartPosition { get; init; }

    /// <summary>
    /// Selected audio stream index
    /// </summary>
    public int? AudioStreamIndex { get; init; }

    /// <summary>
    /// Selected subtitle stream index
    /// </summary>
    public int? SubtitleStreamIndex { get; init; }

    /// <summary>
    /// Hardware acceleration method to use
    /// </summary>
    public HwAccelMethod HwAccelMethod { get; init; } = HwAccelMethod.None;

    /// <summary>
    /// Output container format
    /// </summary>
    public string OutputFormat { get; init; } = "mpegts";

    /// <summary>
    /// Session ID for progress tracking
    /// </summary>
    public string? SessionId { get; init; }

    /// <summary>
    /// Total duration of the media in seconds (for progress calculation)
    /// </summary>
    public double TotalDuration { get; init; }
}
