using MediatR;

namespace Lanflix.Application.Features.Streaming.Commands.StopStream;

public class StopStreamCommand : IRequest<Unit>
{
    public string SessionId { get; set; } = string.Empty;
}
