using Lanflix.Domain.Common;

namespace Lanflix.Domain.Entities;

/// <summary>
/// Represents a watchlist item for a profile
/// </summary>
public class Watchlist : BaseEntity
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
    /// Timestamp when the item was added to the watchlist
    /// </summary>
    public DateTime AddedAt { get; set; }

    /// <summary>
    /// Optional notes or comments about the watchlist item
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties

    /// <summary>
    /// Associated profile
    /// </summary>
    public Profile Profile { get; set; } = null!;

    /// <summary>
    /// Associated content
    /// </summary>
    public Content Content { get; set; } = null!;
}
