using Lanflix.Application.Common.DTOs;
using Lanflix.Domain.ValueObjects;
using MediatR;

namespace Lanflix.Application.Features.Profiles.Commands.CreateProfile;

public class CreateProfileCommand : IRequest<ProfileDto>
{
    public string Name { get; set; } = string.Empty;
    public string? AvatarPath { get; set; }
    public bool IsKidsProfile { get; set; }
    public string? PinCode { get; set; }
    public bool IsGuest { get; set; }
    public bool CanDownload { get; set; } = true;
    public bool CanManageSettings { get; set; } = true;
    public UserPreferences? Preferences { get; set; }
}
