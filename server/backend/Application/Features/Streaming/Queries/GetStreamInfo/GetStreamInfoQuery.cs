using Lanflix.Application.Common.Behaviors;
using Lanflix.Application.Common.DTOs;
using MediatR;

namespace Lanflix.Application.Features.Streaming.Queries.GetStreamInfo;

public class GetStreamInfoQuery : IRequest<StreamSessionDto>, ICacheableQuery
{
    public string SessionId { get; set; } = string.Empty;

    public string CacheKey => $"session:{SessionId}:info";
    
    // Short expiration for session info (30 seconds)
    public TimeSpan? CacheExpiration => TimeSpan.FromSeconds(30);
}
