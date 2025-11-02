using Lanflix.Domain.ValueObjects;

namespace Lanflix.Application.Features.Streaming.Commands.StartStream;

public class StartStreamCommand
{
    public int ContentId { get; set; }
    public int ProfileId { get; set; }
    public ClientCapabilities ClientCapabilities { get; set; } = null!;
}