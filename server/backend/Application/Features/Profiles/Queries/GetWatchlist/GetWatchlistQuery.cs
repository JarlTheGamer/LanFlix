using Lanflix.Application.Common.Behaviors;
using Lanflix.Application.Common.DTOs;
using MediatR;

namespace Lanflix.Application.Features.Profiles.Queries.GetWatchlist;

public class GetWatchlistQuery : IRequest<List<ContentDto>>, ICacheableQuery
{
    public int ProfileId { get; set; }

    public string CacheKey => $"profile:{ProfileId}:watchlist";
    
    public TimeSpan? CacheExpiration => TimeSpan.FromMinutes(5);
}
