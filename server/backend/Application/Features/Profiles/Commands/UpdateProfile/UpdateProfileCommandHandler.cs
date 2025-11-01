using Lanflix.Application.Common.DTOs;
using Lanflix.Application.Common.Exceptions;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.Application.Features.Profiles.Commands.UpdateProfile;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, ProfileDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cacheService;

    public UpdateProfileCommandHandler(
        IApplicationDbContext context,
        ICacheService cacheService)
    {
        _context = context;
        _cacheService = cacheService;
    }

    public async Task<ProfileDto> Handle(
        UpdateProfileCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await _context.Profiles
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (profile == null)
        {
            throw new NotFoundException(nameof(Profile), request.Id);
        }

        profile.Name = request.Name;
        profile.AvatarPath = request.AvatarPath;
        profile.IsKidsProfile = request.IsKidsProfile;
        
        if (request.Preferences != null)
        {
            profile.Preferences = request.Preferences;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate profiles cache
        await _cacheService.RemoveAsync("profiles:all", cancellationToken);
        await _cacheService.RemoveAsync($"profile:{profile.Id}:prefs", cancellationToken);

        return new ProfileDto
        {
            Id = profile.Id,
            Name = profile.Name,
            AvatarPath = profile.AvatarPath,
            IsKidsProfile = profile.IsKidsProfile,
            Preferences = profile.Preferences,
            CreatedAt = profile.CreatedAt
        };
    }
}
