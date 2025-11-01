namespace Lanflix.Infrastructure.Migration.Models;

/// <summary>
/// Container for all legacy data read from the old database
/// </summary>
public class LegacyData
{
    public List<LegacyContent> Contents { get; set; } = new();
    public List<LegacyProfile> Profiles { get; set; } = new();
    public List<LegacyWatchHistory> WatchHistories { get; set; } = new();
    public List<LegacySeriesEpisode> Episodes { get; set; } = new();
    public List<LegacySettings> Settings { get; set; } = new();
}
