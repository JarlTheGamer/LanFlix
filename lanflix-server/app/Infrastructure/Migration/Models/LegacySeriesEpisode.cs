namespace Lanflix.Infrastructure.Migration.Models;

/// <summary>
/// Represents a SeriesEpisode record from the legacy Node.js backend database
/// </summary>
public class LegacySeriesEpisode
{
    public int Id { get; set; }
    public int ContentId { get; set; }
    public int SeasonNumber { get; set; }
    public int EpisodeNumber { get; set; }
    public string? Title { get; set; }
    public string? Overview { get; set; }
    public DateTime? AirDate { get; set; }
    public string? StillPath { get; set; }
    public string? FilePath { get; set; }
}
