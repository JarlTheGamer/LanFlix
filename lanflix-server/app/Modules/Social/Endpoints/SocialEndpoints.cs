using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Lanflix.Modules.Social;

public static class SocialModule
{
    public static IServiceCollection AddSocialModule(this IServiceCollection services) => services;

    public static IEndpointRouteBuilder MapSocialModule(this IEndpointRouteBuilder endpoints)
    {
        var social = endpoints.MapGroup("/api/v2/social").WithTags("Social").RequireAuthorization();
        social.MapRelationshipEndpoints();
        social.MapFeedEndpoints();
        social.MapReviewEndpoints();
        social.MapSafetyAndNotificationEndpoints();
        endpoints.MapSocialModerationEndpoints();
        return endpoints;
    }
}
