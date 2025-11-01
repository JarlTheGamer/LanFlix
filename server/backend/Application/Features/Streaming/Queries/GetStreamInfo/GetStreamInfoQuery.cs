using Lanflix.Application.Common.DTOs;
using MediatR;

namespace Lanflix.Application.Features.Streaming.Queries.GetStreamInfo;

public class GetStreamInfoQuery : IRequest<StreamSessionDto>
{
    public string SessionId { get; set; } = string.Empty;
}
