using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Lanflix.WebApi.Authorization;

/// <summary>
/// Authorization requirement for profile-based access control
/// </summary>
public class ProfileOwnerRequirement : IAuthorizationRequirement
{
    public string ProfileIdParameterName { get; }

    public ProfileOwnerRequirement(string profileIdParameterName = "profileId")
    {
        ProfileIdParameterName = profileIdParameterName;
    }
}

/// <summary>
/// Authorization handler that ensures users can only access their own profile data
/// </summary>
public class ProfileAuthorizationHandler : AuthorizationHandler<ProfileOwnerRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ProfileAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, 
        ProfileOwnerRequirement requirement)
    {
        // Admin users can access any profile
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Get the profile ID from the user's claims
        var userProfileIdClaim = context.User.FindFirst("ProfileId")?.Value;
        if (string.IsNullOrEmpty(userProfileIdClaim))
        {
            return Task.CompletedTask;
        }

        // Get the profile ID from the route or query parameters
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return Task.CompletedTask;
        }

        string? requestedProfileId = null;

        // Try to get from route values
        if (httpContext.Request.RouteValues.TryGetValue(requirement.ProfileIdParameterName, out var routeValue))
        {
            requestedProfileId = routeValue?.ToString();
        }
        // Try to get from query string
        else if (httpContext.Request.Query.TryGetValue(requirement.ProfileIdParameterName, out var queryValue))
        {
            requestedProfileId = queryValue.ToString();
        }

        // If the requested profile ID matches the user's profile ID, authorize
        if (requestedProfileId == userProfileIdClaim)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
