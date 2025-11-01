namespace Lanflix.Infrastructure.Migration.Models;

/// <summary>
/// Represents a Profile record from the legacy Node.js backend database
/// </summary>
public class LegacyProfile
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AvatarColorPrimary { get; set; } = string.Empty;
    public string AvatarColorSecondary { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
