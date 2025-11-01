using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Features.Profiles.Queries.GetProfiles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lanflix.WebApi.Controllers;

/// <summary>
/// Controller for authentication operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IMediator mediator, 
        ITokenService tokenService,
        ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticate with a profile and receive a JWT token
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        // Get all profiles
        var profiles = await _mediator.Send(new GetProfilesQuery());
        
        // Find the profile by ID
        var profile = profiles.FirstOrDefault(p => p.Id == request.ProfileId);
        
        if (profile == null)
        {
            _logger.LogWarning("Login attempt with invalid profile ID: {ProfileId}", request.ProfileId);
            return Unauthorized(new { message = "Invalid profile" });
        }

        // Generate JWT token
        var token = _tokenService.GenerateToken(profile.Id, profile.Name, isAdmin: false);

        _logger.LogInformation("Profile {ProfileId} ({ProfileName}) logged in successfully", 
            profile.Id, profile.Name);

        return Ok(new LoginResponse
        {
            Token = token,
            ProfileId = profile.Id,
            ProfileName = profile.Name,
            ExpiresAt = DateTime.UtcNow.AddMinutes(43200) // 30 days
        });
    }

    /// <summary>
    /// Validate the current token and return profile information
    /// </summary>
    [HttpGet("validate")]
    [Authorize]
    public ActionResult<ValidateTokenResponse> ValidateToken()
    {
        var profileIdClaim = User.FindFirst("ProfileId")?.Value;
        var profileName = User.Identity?.Name;

        if (string.IsNullOrEmpty(profileIdClaim))
        {
            return Unauthorized();
        }

        return Ok(new ValidateTokenResponse
        {
            IsValid = true,
            ProfileId = int.Parse(profileIdClaim),
            ProfileName = profileName ?? "Unknown"
        });
    }
}

public class LoginRequest
{
    public int ProfileId { get; set; }
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public int ProfileId { get; set; }
    public string ProfileName { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class ValidateTokenResponse
{
    public bool IsValid { get; set; }
    public int ProfileId { get; set; }
    public string ProfileName { get; set; } = string.Empty;
}
