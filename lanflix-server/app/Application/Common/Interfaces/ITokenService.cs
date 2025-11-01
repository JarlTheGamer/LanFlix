namespace Lanflix.Application.Common.Interfaces;

/// <summary>
/// Service for generating and validating JWT tokens
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a JWT token for the specified profile
    /// </summary>
    string GenerateToken(int profileId, string profileName, bool isAdmin = false);
    
    /// <summary>
    /// Validates a JWT token and returns the profile ID if valid
    /// </summary>
    int? ValidateToken(string token);
}
