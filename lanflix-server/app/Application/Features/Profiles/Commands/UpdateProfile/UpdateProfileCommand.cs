using Lanflix.Application.Common.DTOs;
using Lanflix.Domain.ValueObjects;
using MediatR;

namespace Lanflix.Application.Features.Profiles.Commands.UpdateProfile;

public class UpdateProfileCommand : IRequest<ProfileDto>
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AvatarPath { get; set; }
    public bool IsKidsProfile { get; set; }
    public string? PinCode { get; set; }
    public bool? IsGuest { get; set; }
    public bool? CanDownload { get; set; }
    public bool? CanManageSettings { get; set; }
    public UserPreferences? Preferences { get; set; }
}
