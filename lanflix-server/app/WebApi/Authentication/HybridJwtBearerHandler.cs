using System.Security.Claims;
using System.Text.Encodings.Web;
using Lanflix.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace Lanflix.WebApi.Authentication;

/// <summary>
/// Custom authentication handler that supports both new and legacy JWT tokens
/// </summary>
public class HybridJwtBearerHandler : JwtBearerHandler
{
    private readonly ILegacyTokenService _legacyTokenService;
    private readonly ILogger<HybridJwtBearerHandler> _logger;

    public HybridJwtBearerHandler(
        IOptionsMonitor<JwtBearerOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ILegacyTokenService legacyTokenService)
        : base(options, logger, encoder)
    {
        _legacyTokenService = legacyTokenService;
        _logger = logger.CreateLogger<HybridJwtBearerHandler>();
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // First, try standard JWT authentication
        var result = await base.HandleAuthenticateAsync();

        // If standard authentication succeeded, return it
        if (result.Succeeded)
        {
            return result;
        }

        // If standard authentication failed, try legacy token validation
        var token = GetTokenFromRequest();

        if (string.IsNullOrEmpty(token))
        {
            return AuthenticateResult.NoResult();
        }

        // Check if this is a legacy token
        if (!_legacyTokenService.IsLegacyToken(token))
        {
            // Not a legacy token, return the original failure
            return result;
        }

        _logger.LogInformation("Detected legacy token, attempting validation");

        // Validate legacy token
        var profileId = _legacyTokenService.ValidateLegacyToken(token);

        if (!profileId.HasValue)
        {
            _logger.LogWarning("Legacy token validation failed");
            return AuthenticateResult.Fail("Invalid legacy token");
        }

        // Create claims for the authenticated user
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, profileId.Value.ToString()),
            new Claim("ProfileId", profileId.Value.ToString()),
            new Claim("TokenType", "Legacy")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        _logger.LogInformation("Successfully authenticated using legacy token for profile {ProfileId}", profileId);

        // Store flag indicating this is a legacy token
        Context.Items["IsLegacyToken"] = true;
        Context.Items["LegacyToken"] = token;

        return AuthenticateResult.Success(ticket);
    }

    private string? GetTokenFromRequest()
    {
        // Check Authorization header
        var authorization = Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authorization.Substring("Bearer ".Length).Trim();
        }

        // Check query parameter (for legacy clients that might use this)
        if (Request.Query.TryGetValue("token", out var tokenQuery))
        {
            return tokenQuery.ToString();
        }

        return null;
    }
}
