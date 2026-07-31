using Lanflix.Domain.Common;
using Lanflix.Domain.Interfaces;
using Lanflix.Domain.ValueObjects;

namespace Lanflix.Domain.Entities;

/// <summary>
/// Represents a user profile with personalized settings
/// </summary>
public class Profile : BaseEntity, IAuditableEntity
{
    /// <summary>
    /// Profile name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Path to the profile avatar image
    /// </summary>
    public string? AvatarPath { get; set; }

    /// <summary>
    /// Indicates whether this is a kids profile with content restrictions
    /// </summary>
    public bool IsKidsProfile { get; set; }

    /// <summary>
    /// User preferences for streaming and playback
    /// </summary>
    public UserPreferences Preferences { get; set; } = new();

    /// <summary>
    /// PIN code for profile access (optional)
    /// </summary>
    public string? PinCode { get; set; }

    /// <summary>
    /// Indicates whether this is the default/primary profile
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Indicates whether this is a temporary/restricted guest profile
    /// </summary>
    public bool IsGuest { get; set; }

    /// <summary>
    /// Permission to download offline media
    /// </summary>
    public bool CanDownload { get; set; } = true;

    /// <summary>
    /// Permission to access and manage server settings
    /// </summary>
    public bool CanManageSettings { get; set; } = true;

    // Navigation properties

    /// <summary>
    /// Collection of watch history records for this profile
    /// </summary>
    public ICollection<WatchHistory> WatchHistories { get; set; } = new List<WatchHistory>();

    /// <summary>
    /// Collection of watchlist items for this profile
    /// </summary>
    public ICollection<Watchlist> Watchlists { get; set; } = new List<Watchlist>();

    /// <summary>
    /// Collection of active stream sessions for this profile
    /// </summary>
    public ICollection<StreamSession> StreamSessions { get; set; } = new List<StreamSession>();
}
