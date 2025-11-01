using Lanflix.Domain.Enums;

namespace Lanflix.Application.Common.Models;

/// <summary>
/// Result of a streaming operation
/// </summary>
public class StreamResult
{
    /// <summary>
    /// The data stream to send to the client
    /// </summary>
    public required Stream DataStream { get; init; }

    /// <summary>
    /// Content type (MIME type) of the stream
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// Content length in bytes (null if unknown/streaming)
    /// </summary>
    public long? ContentLength { get; init; }

    /// <summary>
    /// Streaming mode used
    /// </summary>
    public required StreamingMode Mode { get; init; }

    /// <summary>
    /// Whether this stream supports HTTP range requests
    /// </summary>
    public bool SupportsRangeRequests { get; init; }

    /// <summary>
    /// Start byte position for range requests
    /// </summary>
    public long? RangeStart { get; init; }

    /// <summary>
    /// End byte position for range requests
    /// </summary>
    public long? RangeEnd { get; init; }

    /// <summary>
    /// Optional cleanup action to execute when streaming completes
    /// </summary>
    public Action? CleanupAction { get; init; }
}
