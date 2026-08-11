using Lanflix.Domain.ValueObjects;

namespace Lanflix.Application.Common.Models;

/// <summary>
/// Request for streaming media content with enhanced transcoding profile support
/// </summary>
public class StreamRequest
{
    /// <summary>
    /// Unique session identifier
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// Path to the media file
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Media information
    /// </summary>
    public required MediaInfo MediaInfo { get; init; }

    /// <summary>
    /// User preferences
    /// </summary>
    public UserPreferences? UserPreferences { get; init; }

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
    /// HTTP range header value (for range requests)
    /// </summary>
    public string? RangeHeader { get; init; }

    /// <summary>
    /// If set, limits the output to this many seconds (used for HLS segment transcoding).
    /// </summary>
    public double? SegmentDuration { get; init; }

    /// <summary>
    /// If set, overrides the output container format (e.g. "mpegts" for HLS).
    /// </summary>
    public string? ForceOutputFormat { get; init; }
}
