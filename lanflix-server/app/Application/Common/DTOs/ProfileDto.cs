using Lanflix.Domain.ValueObjects;

namespace Lanflix.Application.Common.DTOs;

public class ProfileDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AvatarPath { get; set; }
    public bool IsKidsProfile { get; set; }
    public UserPreferences? Preferences { get; set; }
    public DateTime CreatedAt { get; set; }
}
