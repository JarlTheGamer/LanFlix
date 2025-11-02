namespace Lanflix.Application.Common.DTOs;

public class StreamSessionDto
{
    public string Id { get; set; } = null!;
    public int ContentId { get; set; }
    public int ProfileId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string StreamUrl { get; set; } = null!;
}