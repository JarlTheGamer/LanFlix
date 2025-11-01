using Lanflix.Domain.Common;
using Lanflix.Domain.Enums;

namespace Lanflix.Domain.Entities;

/// <summary>
/// Represents an active streaming session
/// </summary>
public class StreamSession : BaseEntity
{
    /// <summary>
    /// Unique session identifier (GUID)
    /// </summary>
    public string SessionId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Foreign key to the Profile
    /// </summary>
    public int ProfileId { get; set; }

    /// <summary>
    /// Foreign key to the Content being streamed
    /// </summary>
    public int ContentId { get; set; }

    /// <summary>
    /// Foreign key to the Episode being streamed (null for movies)
    /// </summary>
    public int? EpisodeId { get; set; }

    /// <summary>
    /// Streaming mode being used for this session
    /// </summary>
    public StreamingMode Mode { get; set; }

    /// <summary>
    /// FFmpeg process ID (if transcoding is active)
    /// </summary>
    public string? TranscodingProcessId { get; set; }

    /// <summary>
    /// Timestamp when the session started
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Timestamp when the session ended
    /// </summary>
    public DateTime? EndedAt { get; set; }

    /// <summary>
    /// Indicates whether the session is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Client IP address
    /// </summary>
    public string? ClientIpAddress { get; set; }

    /// <summary>
    /// Client user agent string
    /// </summary>
    public string? ClientUserAgent { get; set; }

    /// <summary>
    /// Current playback position in ticks (1 tick = 100 nanoseconds)
    /// </summary>
    public long CurrentPositionTicks { get; set; }

    /// <summary>
    /// Timestamp of last activity/heartbeat
    /// </summary>
    public DateTime LastActivityAt { get; set; }

    /// <summary>
    /// Target bitrate for transcoding (if applicable)
    /// </summary>
    public long? TargetBitrate { get; set; }

    /// <summary>
    /// Target video codec for transcoding (if applicable)
    /// </summary>
    public string? TargetVideoCodec { get; set; }

    /// <summary>
    /// Target audio codec for transcoding (if applicable)
    /// </summary>
    public string? TargetAudioCodec { get; set; }

    // Navigation properties

    /// <summary>
    /// Associated profile
    /// </summary>
    public Profile Profile { get; set; } = null!;

    /// <summary>
    /// Associated content
    /// </summary>
    public Content Content { get; set; } = null!;

    /// <summary>
    /// Associated episode (if applicable)
    /// </summary>
    public Episode? Episode { get; set; }
}
