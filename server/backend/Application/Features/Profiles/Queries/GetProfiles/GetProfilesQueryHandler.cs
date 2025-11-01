using Lanflix.Application.Common.DTOs;
using Lanflix.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.Application.Features.Profiles.Queries.GetProfiles;

public class GetProfilesQueryHandler : IRequestHandler<GetProfilesQuery, List<ProfileDto>>
{
    private readonly IApplicationDbContext _context;

    public GetProfilesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProfileDto>> Handle(
        GetProfilesQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Profiles
            .OrderBy(p => p.CreatedAt)
            .Select(p => new ProfileDto
            {
                Id = p.Id,
                Name = p.Name,
                AvatarPath = p.AvatarPath,
                IsKidsProfile = p.IsKidsProfile,
                Preferences = p.Preferences,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
