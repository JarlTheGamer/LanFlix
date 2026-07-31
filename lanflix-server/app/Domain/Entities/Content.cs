using Lanflix.Domain.Common;
using Lanflix.Domain.Enums;
using Lanflix.Domain.Interfaces;
using Lanflix.Domain.ValueObjects;

namespace Lanflix.Domain.Entities;

/// <summary>
/// Represents a media content item (movie or TV series)
/// </summary>
public class Content : BaseEntity, IAuditableEntity, ISoftDelete
{
    /// <summary>
    /// The Movie Database (TMDB) identifier
    /// </summary>
    public int TmdbId { get; set; }

    /// <summary>
    /// Type of content (Movie or Series)
    /// </summary>
    public ContentType Type { get; set; }

    /// <summary>
    /// Title of the content
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Original title in the original language
    /// </summary>
    public string? OriginalTitle { get; set; }

    /// <summary>
    /// Overview/description of the content
    /// </summary>
    public string? Overview { get; set; }

    /// <summary>
    /// Full file path to the media file
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Media information including video, audio, and subtitle streams
    /// </summary>
    public MediaInfo? MediaInfo { get; set; }

    /// <summary>
    /// Release date of the content
    /// </summary>
    public DateTime? ReleaseDate { get; set; }

    /// <summary>
    /// Path to the poster image
    /// </summary>
    public string? PosterPath { get; set; }

    /// <summary>
    /// Path to the backdrop image
    /// </summary>
    public string? BackdropPath { get; set; }

    /// <summary>
    /// Rating score (e.g., from TMDB)
    /// </summary>
    public double? Rating { get; set; }

    /// <summary>
    /// Array of genre names
    /// </summary>
    public string[]? Genres { get; set; }

    /// <summary>
    /// Timestamp when the content was added to the library
    /// </summary>
    public DateTime AddedAt { get; set; }

    /// <summary>
    /// TMDb Collection / Box Set ID
    /// </summary>
    public int? CollectionId { get; set; }

    /// <summary>
    /// TMDb Collection / Box Set Name
    /// </summary>
    public string? CollectionName { get; set; }

    /// <summary>
    /// Indicates whether the content has been soft deleted
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Timestamp when the content was soft deleted
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    // Navigation properties

    /// <summary>
    /// Collection of episodes (for Series content type)
    /// </summary>
    public ICollection<Episode> Episodes { get; set; } = new List<Episode>();

    /// <summary>
    /// Collection of watch history records for this content
    /// </summary>
    public ICollection<WatchHistory> WatchHistories { get; set; } = new List<WatchHistory>();
}
