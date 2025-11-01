using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Lanflix.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Lanflix.Infrastructure.Services.Authentication;

/// <summary>
/// Service for validating and migrating legacy authentication tokens from the Node.js backend
/// </summary>
public class LegacyTokenService : ILegacyTokenService
{
    private readonly IConfiguration _configuration;
    private readonly ITokenService _tokenService;
    private readonly ILogger<LegacyTokenService> _logger;
    private readonly string? _legacySecretKey;
    private readonly string? _legacyIssuer;
    private readonly string? _legacyAudience;

    public LegacyTokenService(
        IConfiguration configuration,
        ITokenService tokenService,
        ILogger<LegacyTokenService> logger)
    {
        _configuration = configuration;
        _tokenService = tokenService;
        _logger = logger;

        // Load legacy token configuration
        _legacySecretKey = configuration["LegacyJwt:Key"];
        _legacyIssuer = configuration["LegacyJwt:Issuer"] ?? "LanflixLegacy";
        _legacyAudience = configuration["LegacyJwt:Audience"] ?? "LanflixLegacyClient";
    }

    public int? ValidateLegacyToken(string legacyToken)
    {
        if (string.IsNullOrEmpty(legacyToken) || string.IsNullOrEmpty(_legacySecretKey))
        {
            return null;
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_legacySecretKey);

        try
        {
            // Try to validate with legacy settings (more permissive)
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = !string.IsNullOrEmpty(_legacyIssuer),
                ValidIssuer = _legacyIssuer,
                ValidateAudience = !string.IsNullOrEmpty(_legacyAudience),
                ValidAudience = _legacyAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5) // Allow 5 minutes clock skew
            };

            tokenHandler.ValidateToken(legacyToken, validationParameters, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;

            // Try different claim types that might have been used in legacy backend
            var profileIdClaim = jwtToken.Claims.FirstOrDefault(x =>
                x.Type == "ProfileId" ||
                x.Type == "profileId" ||
                x.Type == "sub" ||
                x.Type == ClaimTypes.NameIdentifier);

            if (profileIdClaim != null && int.TryParse(profileIdClaim.Value, out var profileId))
            {
                _logger.LogInformation("Successfully validated legacy token for profile {ProfileId}", profileId);
                return profileId;
            }

            _logger.LogWarning("Legacy token validated but no profile ID claim found");
            return null;
        }
        catch (SecurityTokenExpiredException ex)
        {
            _logger.LogWarning(ex, "Legacy token has expired");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to validate legacy token");
            return null;
        }
    }

    public string? MigrateLegacyToken(string legacyToken)
    {
        var profileId = ValidateLegacyToken(legacyToken);

        if (!profileId.HasValue)
        {
            _logger.LogWarning("Cannot migrate invalid legacy token");
            return null;
        }

        try
        {
            // Extract additional information from legacy token
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(legacyToken);

            var profileName = jwtToken.Claims.FirstOrDefault(x =>
                x.Type == ClaimTypes.Name ||
                x.Type == "name" ||
                x.Type == "profileName")?.Value ?? $"Profile{profileId}";

            var isAdmin = jwtToken.Claims.Any(x =>
                (x.Type == ClaimTypes.Role || x.Type == "role") &&
                x.Value.Equals("Admin", StringComparison.OrdinalIgnoreCase));

            // Generate new token with current system
            var newToken = _tokenService.GenerateToken(profileId.Value, profileName, isAdmin);

            _logger.LogInformation(
                "Successfully migrated legacy token for profile {ProfileId} to new format",
                profileId);

            return newToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error migrating legacy token for profile {ProfileId}", profileId);
            return null;
        }
    }

    public bool IsLegacyToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            // Check if token has legacy issuer
            if (!string.IsNullOrEmpty(_legacyIssuer) &&
                jwtToken.Issuer.Equals(_legacyIssuer, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Check if token has legacy audience
            if (!string.IsNullOrEmpty(_legacyAudience) &&
                jwtToken.Audiences.Any(a => a.Equals(_legacyAudience, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // Check for legacy claim structure
            var hasLegacyClaims = jwtToken.Claims.Any(c =>
                c.Type == "profileId" || // Legacy used lowercase
                c.Type == "userId");     // Or might have used userId

            return hasLegacyClaims;
        }
        catch
        {
            return false;
        }
    }
}
