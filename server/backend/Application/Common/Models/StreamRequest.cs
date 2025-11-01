using Lanflix.Domain.ValueObjects;

namespace Lanflix.Application.Common.Models;

/// <summary>
/// Request for streaming media content
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
    /// Client capabilities
    /// </summary>
    public required ClientCapabilities ClientCapabilities { get; init; }

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
}
