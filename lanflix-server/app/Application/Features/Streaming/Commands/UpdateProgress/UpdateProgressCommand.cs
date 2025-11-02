namespace Lanflix.Application.Features.Streaming.Commands.UpdateProgress;

public class UpdateProgressCommand
{
    public string SessionId { get; set; } = null!;
    public long PositionTicks { get; set; }
    public bool IsCompleted { get; set; }
}