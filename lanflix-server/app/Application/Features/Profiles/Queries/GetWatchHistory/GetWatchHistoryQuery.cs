using Lanflix.Application.Common.Behaviors;
using Lanflix.Application.Common.DTOs;
using MediatR;

namespace Lanflix.Application.Features.Profiles.Queries.GetWatchHistory;

public class GetWatchHistoryQuery : IRequest<List<WatchHistoryDto>>, ICacheableQuery
{
    public int ProfileId { get; set; }
    public int? Limit { get; set; } = 50;

    public string CacheKey => $"profile:{ProfileId}:history:{Limit}";
    
    public TimeSpan? CacheExpiration => TimeSpan.FromMinutes(5);
}
