namespace Lanflix.Application.Common.Interfaces;

/// <summary>
/// Service for validating and migrating legacy authentication tokens
/// </summary>
public interface ILegacyTokenService
{
    /// <summary>
    /// Validates a legacy token from the old Node.js backend
    /// </summary>
    /// <param name="legacyToken">The legacy token to validate</param>
    /// <returns>Profile ID if valid, null otherwise</returns>
    int? ValidateLegacyToken(string legacyToken);

    /// <summary>
    /// Migrates a legacy token to a new token format
    /// </summary>
    /// <param name="legacyToken">The legacy token to migrate</param>
    /// <returns>New token if migration successful, null otherwise</returns>
    string? MigrateLegacyToken(string legacyToken);

    /// <summary>
    /// Checks if a token is in legacy format
    /// </summary>
    /// <param name="token">The token to check</param>
    /// <returns>True if token is in legacy format</returns>
    bool IsLegacyToken(string token);
}
