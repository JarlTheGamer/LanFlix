using Lanflix.Domain.Enums;

namespace Lanflix.Application.Common.Models;

/// <summary>
/// Result of a streaming operation
/// </summary>
public class StreamResult
{
    /// <summary>
    /// Stream of media data
    /// </summary>
    public required Stream DataStream { get; init; }

    /// <summary>
    /// Content type (MIME type)
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// Total content length in bytes (null if unknown)
    /// </summary>
    public long? ContentLength { get; init; }

    /// <summary>
    /// Streaming mode used
    /// </summary>
    public required StreamingMode Mode { get; init; }

    /// <summary>
    /// Indicates whether range requests are supported
    /// </summary>
    public bool SupportsRangeRequests { get; init; }

    /// <summary>
    /// Start byte position (for range requests)
    /// </summary>
    public long? RangeStart { get; init; }

    /// <summary>
    /// End byte position (for range requests)
    /// </summary>
    public long? RangeEnd { get; init; }

    /// <summary>
    /// FFmpeg process ID (if transcoding)
    /// </summary>
    public string? TranscodingProcessId { get; init; }

    /// <summary>
    /// Cleanup action to be called when streaming completes
    /// </summary>
    public Action? CleanupAction { get; init; }
}
