using MediatR;

namespace Lanflix.Application.Features.Streaming.Commands.UpdateProgress;

public class UpdateProgressCommand : IRequest<Unit>
{
    public string SessionId { get; set; } = string.Empty;
    public int ProfileId { get; set; }
    public int ContentId { get; set; }
    public int? EpisodeId { get; set; }
    public long PositionTicks { get; set; }
    public bool IsCompleted { get; set; }
}
