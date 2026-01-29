namespace Lanflix.Application.Common.DTOs;

public class WatchHistoryDto
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public int ContentId { get; set; }
    public int? EpisodeId { get; set; }
    public long PositionTicks { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime LastWatchedAt { get; set; }
    public double WatchedPercentage { get; set; }
    public ContentDto? Content { get; set; }
}
