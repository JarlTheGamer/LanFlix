using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.Modules.Library;

public static class LibraryEndpoints
{
    public static IEndpointRouteBuilder MapLibraryModule(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v2");

        api.MapGet("/status", () => Results.Ok(new
        {
            apiVersion = "2.0",
            serverTimeUtc = DateTime.UtcNow,
            capabilities = new
            {
                movies = true,
                series = true,
                offlineDownloads = true,
                liveTv = true,
                music = true,
                social = true
            }
        })).WithTags("Server");

        api.MapGet("/home", async (int? limit, ClaimsPrincipal user, ILibraryCatalog catalog, CancellationToken ct) =>
            Results.Ok(await catalog.GetHomeAsync(AccountId(user), Math.Clamp(limit ?? 20, 1, 50), ct)))
            .RequireAuthorization()
            .WithTags("Library");

        api.MapGet("/library", async (string? type, int? offset, int? limit, ILibraryCatalog catalog, CancellationToken ct) =>
            Results.Ok(await catalog.GetLibraryAsync(type, Math.Max(offset ?? 0, 0), Math.Clamp(limit ?? 50, 1, 100), ct)))
            .WithTags("Library");

        api.MapGet("/content/{id:int}", async (int id, ILibraryCatalog catalog, CancellationToken ct) =>
        {
            var result = await catalog.GetDetailAsync(id, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).WithTags("Library");

        api.MapGet("/content/{id:int}/download-manifest", async (int id, ILibraryCatalog catalog, CancellationToken ct) =>
        {
            var result = await catalog.GetDownloadManifestAsync(id, ct);
            return result is null
                ? Results.Problem(statusCode: 404, title: "Media file unavailable")
                : Results.Ok(result);
        }).RequireAuthorization().WithTags("Downloads");

        api.MapGet("/artwork/{id:int}/logo", async (int id, ILibraryCatalog catalog, CancellationToken ct) =>
        {
            var location = await catalog.GetLogoRedirectAsync(id, ct);
            return location is null ? Results.NotFound() : Results.Redirect(location);
        }).WithTags("Artwork");

        api.MapGet("/artwork/content/{id:int}/{kind}", ServeContentArtworkAsync).WithTags("Artwork");
        api.MapGet("/artwork/episode/{id:int}/still", ServeEpisodeArtworkAsync).WithTags("Artwork");

        var watchlist = api.MapGroup("/watchlist").WithTags("Library").RequireAuthorization();
        watchlist.MapGet("/", GetWatchlistAsync);
        watchlist.MapPut("/{contentId:int}", AddWatchlistAsync);
        watchlist.MapDelete("/{contentId:int}", RemoveWatchlistAsync);

        return endpoints;
    }

    private static async Task<IResult> GetWatchlistAsync(
        ClaimsPrincipal user, ILibraryDbContext db, ILibraryCatalog catalog, CancellationToken ct)
    {
        var accountId = AccountId(user);
        var ids = await db.AccountWatchlist.AsNoTracking()
            .Where(item => item.AccountId == accountId)
            .OrderByDescending(item => item.CreatedAtUtc)
            .Select(item => item.ContentId)
            .ToArrayAsync(ct);
        return Results.Ok(await catalog.GetByIdsAsync(ids, ct));
    }

    private static async Task<IResult> AddWatchlistAsync(
        int contentId, ClaimsPrincipal user, ILibraryDbContext db, ILibraryCatalog catalog, CancellationToken ct)
    {
        if (await catalog.GetDetailAsync(contentId, ct) is null) return Results.NotFound();
        var accountId = AccountId(user);
        if (!await db.AccountWatchlist.AnyAsync(item => item.AccountId == accountId && item.ContentId == contentId, ct))
        {
            db.AccountWatchlist.Add(AccountWatchlistItem.Create(accountId, contentId));
            await db.SaveChangesAsync(ct);
        }
        return Results.NoContent();
    }

    private static async Task<IResult> RemoveWatchlistAsync(
        int contentId, ClaimsPrincipal user, ILibraryDbContext db, CancellationToken ct)
    {
        var accountId = AccountId(user);
        var item = await db.AccountWatchlist.SingleOrDefaultAsync(
            entry => entry.AccountId == accountId && entry.ContentId == contentId, ct);
        if (item is not null)
        {
            db.AccountWatchlist.Remove(item);
            await db.SaveChangesAsync(ct);
        }
        return Results.NoContent();
    }

    private static Guid AccountId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(value, out var accountId) ? accountId : throw new UnauthorizedAccessException();
    }

    private static async Task<IResult> ServeContentArtworkAsync(
        int id, string kind, HttpContext context, ILibraryCatalog catalog, CancellationToken ct)
    {
        var artwork = await catalog.GetContentArtworkAsync(id, kind, ct);
        return ServeArtwork(artwork, context);
    }

    private static async Task<IResult> ServeEpisodeArtworkAsync(
        int id, HttpContext context, ILibraryCatalog catalog, CancellationToken ct)
    {
        var artwork = await catalog.GetEpisodeArtworkAsync(id, ct);
        return ServeArtwork(artwork, context);
    }

    private static IResult ServeArtwork(ArtworkFileDto? artwork, HttpContext context)
    {
        if (artwork is null) return Results.NotFound();
        context.Response.Headers.ETag = artwork.ETag;
        context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        return Results.File(artwork.Path, artwork.ContentType, enableRangeProcessing: false);
    }
}
