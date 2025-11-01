using Lanflix.Domain.Common;
using Lanflix.Domain.Interfaces;
using Lanflix.Domain.ValueObjects;

namespace Lanflix.Domain.Entities;

/// <summary>
/// Represents an episode of a TV series
/// </summary>
public class Episode : BaseEntity, IAuditableEntity, ISoftDelete
{
    /// <summary>
    /// Foreign key to the parent Content (Series)
    /// </summary>
    public int ContentId { get; set; }

    /// <summary>
    /// The Movie Database (TMDB) identifier for the episode
    /// </summary>
    public int? TmdbId { get; set; }

    /// <summary>
    /// Season number
    /// </summary>
    public int SeasonNumber { get; set; }

    /// <summary>
    /// Episode number within the season
    /// </summary>
    public int EpisodeNumber { get; set; }

    /// <summary>
    /// Title of the episode
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Overview/description of the episode
    /// </summary>
    public string? Overview { get; set; }

    /// <summary>
    /// Full file path to the episode media file
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Media information including video, audio, and subtitle streams
    /// </summary>
    public MediaInfo? MediaInfo { get; set; }

    /// <summary>
    /// Air date of the episode
    /// </summary>
    public DateTime? AirDate { get; set; }

    /// <summary>
    /// Path to the episode still/thumbnail image
    /// </summary>
    public string? StillPath { get; set; }

    /// <summary>
    /// Timestamp when the episode was added to the library
    /// </summary>
    public DateTime AddedAt { get; set; }

    /// <summary>
    /// Indicates whether the episode has been soft deleted
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Timestamp when the episode was soft deleted
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    // Navigation properties

    /// <summary>
    /// Parent content (Series)
    /// </summary>
    public Content Content { get; set; } = null!;

    /// <summary>
    /// Collection of watch history records for this episode
    /// </summary>
    public ICollection<WatchHistory> WatchHistories { get; set; } = new List<WatchHistory>();
}
