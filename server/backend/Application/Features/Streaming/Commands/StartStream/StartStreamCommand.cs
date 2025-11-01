using Lanflix.Application.Common.DTOs;
using Lanflix.Domain.ValueObjects;
using MediatR;

namespace Lanflix.Application.Features.Streaming.Commands.StartStream;

public class StartStreamCommand : IRequest<StreamSessionDto>
{
    public int ContentId { get; set; }
    public int ProfileId { get; set; }
    public int? EpisodeId { get; set; }
    public ClientCapabilities ClientCapabilities { get; set; } = new();
    public long? StartPositionTicks { get; set; }
}
