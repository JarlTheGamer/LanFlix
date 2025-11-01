using Lanflix.Application.Common.DTOs;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Domain.Entities;
using Lanflix.Domain.ValueObjects;
using MediatR;

namespace Lanflix.Application.Features.Profiles.Commands.CreateProfile;

public class CreateProfileCommandHandler : IRequestHandler<CreateProfileCommand, ProfileDto>
{
    private readonly IApplicationDbContext _context;

    public CreateProfileCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProfileDto> Handle(
        CreateProfileCommand request,
        CancellationToken cancellationToken)
    {
        var profile = new Profile
        {
            Name = request.Name,
            AvatarPath = request.AvatarPath,
            IsKidsProfile = request.IsKidsProfile,
            Preferences = request.Preferences ?? new UserPreferences(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Profiles.Add(profile);
        await _context.SaveChangesAsync(cancellationToken);

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
