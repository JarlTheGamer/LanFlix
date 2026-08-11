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

        // Batch stills — single round-trip for a whole season's episode thumbnails
        api.MapGet("/artwork/episode/stills", async (string? ids, HttpContext context, ILibraryCatalog catalog, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(ids)) return Results.BadRequest("ids query param required");
            var idList = ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.TryParse(s, out var v) ? (int?)v : null)
                .Where(v => v.HasValue).Select(v => v!.Value).Distinct().Take(200).ToArray();
            var result = new Dictionary<int, string?>();
            foreach (var id in idList)
            {
                var art = await catalog.GetEpisodeArtworkAsync(id, ct);
                result[id] = art is not null ? $"/api/v2/artwork/episode/{id}/still" : null;
            }
            return Results.Ok(result);
        }).WithTags("Artwork");

        var watchlist = api.MapGroup("/watchlist").WithTags("Library").RequireAuthorization();
        watchlist.MapGet("/", GetWatchlistAsync);
        watchlist.MapPut("/{contentId:int}", AddWatchlistAsync);
        watchlist.MapDelete("/{contentId:int}", RemoveWatchlistAsync);

        var history = api.MapGroup("/history").WithTags("History").RequireAuthorization();
        history.MapGet("/", GetHistoryAsync);
        history.MapDelete("/", ClearHistoryAsync);

        // Cast & crew — use a separate CTS so navigation away doesn't abort the TMDB fetch
        api.MapGet("/content/{id:int}/cast", async (int id, ILibraryCatalog catalog, CancellationToken ct) =>
        {
            // Allow up to 15 s for TMDB, independent of the client's request lifetime
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(15));
            var cast = await catalog.GetCastAsync(id, cts.Token);
            return Results.Ok(cast);
        }).WithTags("Library");

        return endpoints;
    }

    private static async Task<IResult> GetHistoryAsync(
        ClaimsPrincipal user, ILibraryCatalog catalog, CancellationToken ct)
    {
        var accountId = AccountId(user);
        return Results.Ok(await catalog.GetWatchHistoryAsync(accountId, ct));
    }

    private static async Task<IResult> ClearHistoryAsync(
        ClaimsPrincipal user, ILibraryCatalog catalog, CancellationToken ct)
    {
        var accountId = AccountId(user);
        await catalog.ClearWatchHistoryAsync(accountId, ct);
        return Results.NoContent();
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
