using Lanflix.Application.Common.Behaviors;
using Lanflix.Application.Common.DTOs;
using MediatR;

namespace Lanflix.Application.Features.Library.Queries.GetContentDetails;

public class GetContentDetailsQuery : IRequest<ContentDto>, ICacheableQuery
{
    public int Id { get; set; }

    public string CacheKey => $"content:{Id}";
    
    public TimeSpan? CacheExpiration => TimeSpan.FromHours(1);
}
