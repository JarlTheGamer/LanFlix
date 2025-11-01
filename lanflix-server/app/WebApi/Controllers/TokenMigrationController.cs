using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lanflix.WebApi.Controllers;

/// <summary>
/// Controller for migrating legacy authentication tokens to new format
/// </summary>
[ApiController]
[Route("api/auth")]
public class TokenMigrationController : ControllerBase
{
    private readonly ILegacyTokenService _legacyTokenService;
    private readonly ILogger<TokenMigrationController> _logger;

    public TokenMigrationController(
        ILegacyTokenService legacyTokenService,
        ILogger<TokenMigrationController> logger)
    {
        _legacyTokenService = legacyTokenService;
        _logger = logger;
    }

    /// <summary>
    /// Migrate a legacy token to the new token format
    /// </summary>
    /// <param name="request">The migration request containing the legacy token</param>
    /// <returns>A new token in the current format</returns>
    [HttpPost("migrate-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenMigrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LegacyApiResponse<object>), StatusCodes.Status400BadRequest)]
    public ActionResult<TokenMigrationResponse> MigrateToken([FromBody] TokenMigrationRequest request)
    {
        if (string.IsNullOrEmpty(request.LegacyToken))
        {
            _logger.LogWarning("Token migration request received with empty token");
            return BadRequest(new
            {
                success = false,
                message = "Legacy token is required"
            });
        }

        // Check if token is actually a legacy token
        if (!_legacyTokenService.IsLegacyToken(request.LegacyToken))
        {
            _logger.LogWarning("Token migration requested for non-legacy token");
            return BadRequest(new
            {
                success = false,
                message = "Provided token is not a legacy token"
            });
        }

        // Migrate the token
        var newToken = _legacyTokenService.MigrateLegacyToken(request.LegacyToken);

        if (string.IsNullOrEmpty(newToken))
        {
            _logger.LogWarning("Failed to migrate legacy token");
            return BadRequest(new
            {
                success = false,
                message = "Failed to migrate token. Token may be invalid or expired."
            });
        }

        _logger.LogInformation("Successfully migrated legacy token");

        return Ok(new TokenMigrationResponse
        {
            Success = true,
            Token = newToken,
            Message = "Token successfully migrated to new format",
            ExpiresIn = 43200 // 30 days in minutes
        });
    }

    /// <summary>
    /// Validate if a token is a legacy token
    /// </summary>
    /// <param name="request">The validation request</param>
    /// <returns>Information about the token</returns>
    [HttpPost("validate-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenValidationResponse), StatusCodes.Status200OK)]
    public ActionResult<TokenValidationResponse> ValidateToken([FromBody] TokenValidationRequest request)
    {
        if (string.IsNullOrEmpty(request.Token))
        {
            return Ok(new TokenValidationResponse
            {
                IsValid = false,
                IsLegacy = false,
                Message = "Token is empty"
            });
        }

        var isLegacy = _legacyTokenService.IsLegacyToken(request.Token);
        var profileId = isLegacy
            ? _legacyTokenService.ValidateLegacyToken(request.Token)
            : null;

        return Ok(new TokenValidationResponse
        {
            IsValid = profileId.HasValue,
            IsLegacy = isLegacy,
            ProfileId = profileId,
            Message = profileId.HasValue
                ? "Token is valid"
                : "Token is invalid or expired",
            ShouldMigrate = isLegacy && profileId.HasValue
        });
    }

    /// <summary>
    /// Get information about the current authentication token
    /// </summary>
    [HttpGet("token-info")]
    [Authorize]
    [ProducesResponseType(typeof(TokenInfoResponse), StatusCodes.Status200OK)]
    public ActionResult<TokenInfoResponse> GetTokenInfo()
    {
        var isLegacyToken = HttpContext.Items["IsLegacyToken"] as bool? ?? false;
        var profileId = User.FindFirst("ProfileId")?.Value;

        return Ok(new TokenInfoResponse
        {
            IsLegacy = isLegacyToken,
            ProfileId = profileId != null ? int.Parse(profileId) : null,
            ShouldMigrate = isLegacyToken,
            Message = isLegacyToken
                ? "You are using a legacy token. Consider migrating to the new format."
                : "You are using the current token format."
        });
    }
}

/// <summary>
/// Request model for token migration
/// </summary>
public class TokenMigrationRequest
{
    public string LegacyToken { get; set; } = string.Empty;
}

/// <summary>
/// Response model for token migration
/// </summary>
public class TokenMigrationResponse
{
    public bool Success { get; set; }
    public string Token { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
}

/// <summary>
/// Request model for token validation
/// </summary>
public class TokenValidationRequest
{
    public string Token { get; set; } = string.Empty;
}

/// <summary>
/// Response model for token validation
/// </summary>
public class TokenValidationResponse
{
    public bool IsValid { get; set; }
    public bool IsLegacy { get; set; }
    public int? ProfileId { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool ShouldMigrate { get; set; }
}

/// <summary>
/// Response model for token info
/// </summary>
public class TokenInfoResponse
{
    public bool IsLegacy { get; set; }
    public int? ProfileId { get; set; }
    public bool ShouldMigrate { get; set; }
    public string Message { get; set; } = string.Empty;
}
