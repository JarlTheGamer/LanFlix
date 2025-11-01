using Lanflix.Application.Common.Behaviors;
using Lanflix.Application.Common.DTOs;
using MediatR;

namespace Lanflix.Application.Features.Profiles.Queries.GetProfiles;

public class GetProfilesQuery : IRequest<List<ProfileDto>>, ICacheableQuery
{
    public string CacheKey => "profiles:all";
    
    public TimeSpan? CacheExpiration => TimeSpan.FromMinutes(10);
}
