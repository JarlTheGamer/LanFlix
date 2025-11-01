using Lanflix.Domain.Enums;

namespace Lanflix.Application.Common.DTOs;

public class StreamSessionDto
{
    public string Id { get; set; } = string.Empty;
    public int ProfileId { get; set; }
    public int ContentId { get; set; }
    public StreamingMode Mode { get; set; }
    public string? TranscodingProcessId { get; set; }
    public DateTime StartedAt { get; set; }
    public bool IsActive { get; set; }
    public string StreamUrl { get; set; } = string.Empty;
}
