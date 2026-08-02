using Lanflix.Application.Common.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lanflix.Modules.Discovery;

public static class DiscoveryEndpoints
{
    public static IEndpointRouteBuilder MapDiscoveryModule(this IEndpointRouteBuilder endpoints)
    {
        var discovery = endpoints.MapGroup("/api/v2/discovery").WithTags("Discovery").RequireAuthorization();
        discovery.MapGet("/", async (int? page, IDiscoveryProvider provider, CancellationToken ct) =>
            Results.Ok(await provider.GetPageAsync(Math.Clamp(page ?? 1, 1, 500), ct)));
        discovery.MapGet("/search", async (string q, string? type, IDiscoveryProvider provider, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
                return Results.Problem(statusCode: 400, title: "Search text must contain at least two characters");
            return Results.Ok(await provider.SearchAsync(q.Trim(), type ?? "all", ct));
        });
        discovery.MapGet("/{type}/{tmdbId:int}/logo", async (string type, int tmdbId, IDiscoveryProvider provider, IImageCacheService imageCache, HttpContext httpContext, CancellationToken ct) =>
        {
            if (type is not ("movie" or "series")) return Results.BadRequest();
            var url = await provider.GetLogoUrlAsync(tmdbId, type, ct);
            if (url is null) return Results.NotFound();

            var cached = await imageCache.GetOrFetchImageAsync(url, ct);
            if (cached is null) return Results.Redirect(url, permanent: false);

            httpContext.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
            return Results.Bytes(cached.Value.Bytes, contentType: cached.Value.ContentType);
        }).AllowAnonymous();
        discovery.MapPost("/{tmdbId:int}/acquire", async (int tmdbId, AcquireMediaRequest request, IDiscoveryProvider provider, CancellationToken ct) =>
        {
            var result = await provider.AcquireAsync(tmdbId, request, ct);
            return result.Accepted ? Results.Accepted(value: result) : Results.Problem(statusCode: 409, title: result.Code, detail: result.Message);
        }).RequireAuthorization("ServerManage");
        discovery.MapPost("/connections/{service}", async (string service, IDiscoveryProvider provider, CancellationToken ct) =>
            Results.Ok(await provider.TestConnectionAsync(service, ct))).RequireAuthorization("ServerManage");
        return endpoints;
    }
}
