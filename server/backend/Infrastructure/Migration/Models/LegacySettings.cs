namespace Lanflix.Infrastructure.Migration.Models;

/// <summary>
/// Represents a Settings record from the legacy Node.js backend database
/// </summary>
public class LegacySettings
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
