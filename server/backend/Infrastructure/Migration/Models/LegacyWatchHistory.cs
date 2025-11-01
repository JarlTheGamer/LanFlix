namespace Lanflix.Infrastructure.Migration.Models;

/// <summary>
/// Represents a WatchHistory record from the legacy Node.js backend database
/// </summary>
public class LegacyWatchHistory
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public int ContentId { get; set; }
    public int? EpisodeId { get; set; }
    public int ProgressSeconds { get; set; }
    public int? DurationSeconds { get; set; }
    public bool Completed { get; set; }
    public DateTime LastWatchedAt { get; set; }
}
