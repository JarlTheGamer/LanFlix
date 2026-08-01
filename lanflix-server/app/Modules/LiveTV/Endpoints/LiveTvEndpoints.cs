using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.Modules.LiveTV;

public static class LiveTvEndpoints
{
    public static IEndpointRouteBuilder MapLiveTvModule(this IEndpointRouteBuilder endpoints)
    {
        var tv = endpoints.MapGroup("/api/v2/live-tv").RequireAuthorization().WithTags("Live TV");
        tv.MapGet("/channels", async (ClaimsPrincipal user, ILiveTvCatalog catalog, CancellationToken ct) => Results.Ok(await catalog.GetChannelsAsync(AccountId(user), ct)));
        tv.MapGet("/guide", async (DateTime? from, DateTime? to, ClaimsPrincipal user, ILiveTvCatalog catalog, CancellationToken ct) =>
        { var start = (from ?? DateTime.UtcNow).ToUniversalTime(); var end = (to ?? start.AddHours(6)).ToUniversalTime(); if (end <= start || end - start > TimeSpan.FromDays(7)) return Results.Problem(statusCode: 400, title: "Guide range must be between zero and seven days"); return Results.Ok(await catalog.GetGuideAsync(AccountId(user), start, end, ct)); });
        tv.MapGet("/channels/{id:long}/stream", StreamAsync);
        tv.MapPut("/favorites/{channelId:long}", AddFavoriteAsync);
        tv.MapDelete("/favorites/{channelId:long}", RemoveFavoriteAsync);

        var admin = tv.MapGroup("/sources").RequireAuthorization("ServerManage");
        admin.MapGet("/", async (ILiveTvCatalog catalog, CancellationToken ct) => Results.Ok(await catalog.GetSourcesAsync(ct)));
        admin.MapPost("/", CreateSourceAsync);
        admin.MapPut("/{id:long}", UpdateSourceAsync);
        admin.MapDelete("/{id:long}", DeleteSourceAsync);
        admin.MapPost("/{id:long}/refresh", async (long id, ILiveTvCatalog catalog, CancellationToken ct) => Results.Ok(await catalog.RefreshAsync(id, ct)));
        return endpoints;
    }

    private static async Task<IResult> StreamAsync(long id, ClaimsPrincipal user, HttpContext http, ILiveTvCatalog catalog, IHttpClientFactory clients, CancellationToken ct)
    {
        var value = await catalog.AcquireStreamAsync(id, AccountId(user), ct);
        if (value is null) return Results.Problem(statusCode: 409, title: "No tuner is currently available");
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, value.Uri);
            var response = await clients.CreateClient("LiveTvStream").SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode) { response.Dispose(); await catalog.ReleaseStreamAsync(value.LeaseId, CancellationToken.None); return Results.Problem(statusCode: 502, title: "Live TV source rejected playback"); }
            http.Response.RegisterForDispose(response);
            http.Response.OnCompleted(async () => await catalog.ReleaseStreamAsync(value.LeaseId, CancellationToken.None));
            return Results.Stream(await response.Content.ReadAsStreamAsync(ct), response.Content.Headers.ContentType?.ToString() ?? value.ContentType);
        }
        catch { await catalog.ReleaseStreamAsync(value.LeaseId, CancellationToken.None); throw; }
    }

    private static async Task<IResult> CreateSourceAsync(SaveLiveTvSourceRequest request, ILiveTvDbContext db, CancellationToken ct)
    { if (!TryKind(request.Kind, out var kind)) return Results.Problem(statusCode: 400, title: "Kind must be m3u or hdhomerun"); try { var source = LiveTvSource.Create(request.Name, kind, request.SourceUri, request.GuideUri, request.MaxTuners); source.Update(request.Name, request.SourceUri, request.GuideUri, request.MaxTuners, request.Enabled); db.LiveTvSources.Add(source); await db.SaveChangesAsync(ct); return Results.Created($"/api/v2/live-tv/sources/{source.Id}", new { source.Id }); } catch (ArgumentException e) { return Results.Problem(statusCode: 400, title: e.Message); } }
    private static async Task<IResult> UpdateSourceAsync(long id, SaveLiveTvSourceRequest request, ILiveTvDbContext db, CancellationToken ct)
    { var source = await db.LiveTvSources.SingleOrDefaultAsync(x => x.Id == id, ct); if (source is null) return Results.NotFound(); if (!TryKind(request.Kind, out var kind) || kind != source.Kind) return Results.Problem(statusCode: 400, title: "Source kind cannot be changed"); try { source.Update(request.Name, request.SourceUri, request.GuideUri, request.MaxTuners, request.Enabled); await db.SaveChangesAsync(ct); return Results.NoContent(); } catch (ArgumentException e) { return Results.Problem(statusCode: 400, title: e.Message); } }
    private static async Task<IResult> DeleteSourceAsync(long id, ILiveTvDbContext db, CancellationToken ct)
    { var source = await db.LiveTvSources.SingleOrDefaultAsync(x => x.Id == id, ct); if (source is null) return Results.NotFound(); db.LiveTvSources.Remove(source); await db.SaveChangesAsync(ct); return Results.NoContent(); }
    private static async Task<IResult> AddFavoriteAsync(long channelId, ClaimsPrincipal user, ILiveTvDbContext db, CancellationToken ct)
    { var accountId = AccountId(user); if (!await db.LiveTvChannels.AnyAsync(x => x.Id == channelId && x.Enabled, ct)) return Results.NotFound(); if (!await db.LiveTvFavorites.AnyAsync(x => x.AccountId == accountId && x.ChannelId == channelId, ct)) { db.LiveTvFavorites.Add(LiveTvFavorite.Create(accountId, channelId)); await db.SaveChangesAsync(ct); } return Results.NoContent(); }
    private static async Task<IResult> RemoveFavoriteAsync(long channelId, ClaimsPrincipal user, ILiveTvDbContext db, CancellationToken ct)
    { var item = await db.LiveTvFavorites.SingleOrDefaultAsync(x => x.AccountId == AccountId(user) && x.ChannelId == channelId, ct); if (item is not null) { db.LiveTvFavorites.Remove(item); await db.SaveChangesAsync(ct); } return Results.NoContent(); }
    private static bool TryKind(string value, out LiveTvSourceKind kind) { if (value.Equals("m3u", StringComparison.OrdinalIgnoreCase) || value.Equals("m3uxmltv", StringComparison.OrdinalIgnoreCase)) { kind = LiveTvSourceKind.M3uXmlTv; return true; } if (value.Equals("hdhomerun", StringComparison.OrdinalIgnoreCase)) { kind = LiveTvSourceKind.HdHomeRun; return true; } kind = default; return false; }
    private static Guid AccountId(ClaimsPrincipal user) => Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub"), out var id) ? id : throw new UnauthorizedAccessException();
}
