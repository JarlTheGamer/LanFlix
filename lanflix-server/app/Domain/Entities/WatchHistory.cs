using Lanflix.Domain.Common;

namespace Lanflix.Domain.Entities;

/// <summary>
/// Represents watch history and playback position for a profile
/// </summary>
public class WatchHistory : BaseEntity
{
    /// <summary>
    /// Foreign key to the Profile
    /// </summary>
    public int ProfileId { get; set; }

    /// <summary>
    /// Foreign key to the Content
    /// </summary>
    public int ContentId { get; set; }

    /// <summary>
    /// Foreign key to the Episode (null for movies)
    /// </summary>
    public int? EpisodeId { get; set; }

    /// <summary>
    /// Playback position in ticks (1 tick = 100 nanoseconds)
    /// </summary>
    public long PositionTicks { get; set; }

    /// <summary>
    /// Indicates whether the content has been fully watched
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// Timestamp of the last watch activity
    /// </summary>
    public DateTime LastWatchedAt { get; set; }

    /// <summary>
    /// Percentage of content watched (0-100)
    /// </summary>
    public double WatchedPercentage { get; set; }

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
