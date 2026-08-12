using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.Modules.Music;

public static class MusicEndpoints
{
    public static IEndpointRouteBuilder MapMusicModule(this IEndpointRouteBuilder endpoints)
    {
        var music = endpoints.MapGroup("/api/v2/music").RequireAuthorization().WithTags("Music");
        music.MapGet("/home", async (int? limit, IMusicCatalog c, CancellationToken ct) => Results.Ok(await c.GetHomeAsync(Math.Clamp(limit ?? 20, 1, 50), ct)));
        music.MapGet("/albums", async (string? q, int? limit, IMusicCatalog c, CancellationToken ct) => Results.Ok(await c.GetAlbumsAsync(q, Math.Clamp(limit ?? 50, 1, 100), ct)));
        music.MapGet("/albums/{id:long}/tracks", async (long id, IMusicCatalog c, CancellationToken ct) => Results.Ok(await c.GetAlbumTracksAsync(id, ct)));
        music.MapGet("/albums/{id:long}/artwork", ServeArtworkAsync);
        music.MapGet("/tracks/{id:long}", async (long id, IMusicCatalog c, CancellationToken ct) => await c.GetTrackAsync(id, ct) is { } track ? Results.Ok(track) : Results.NotFound());
        music.MapGet("/tracks/{id:long}/file", async (long id, IMusicCatalog c, CancellationToken ct) => await c.GetPlayableTrackAsync(id, ct) is { } track ? Results.File(track.FilePath, track.MimeType, enableRangeProcessing: true) : Results.NotFound());
        music.MapGet("/tracks/{id:long}/lyrics", GetLyricsAsync);
        music.MapGet("/tracks/{id:long}/waveform", async (long id, IMusicCatalog c, CancellationToken ct) =>
            await c.GetWaveformAsync(id, ct) is { } waveform ? Results.Ok(waveform) : Results.NotFound());
        music.MapPost("/tracks/{id:long}/play", RecordPlayAsync);
        music.MapPost("/scan", async (IMusicCatalog c, CancellationToken ct) => Results.Ok(await c.ScanAsync(ct))).RequireAuthorization("ServerManage");

        music.MapGet("/favorites", GetFavoritesAsync);
        music.MapPut("/favorites/{trackId:long}", AddFavoriteAsync);
        music.MapDelete("/favorites/{trackId:long}", RemoveFavoriteAsync);
        music.MapGet("/history", GetHistoryAsync);
        music.MapGet("/me/stats", GetListeningStatsAsync);
        music.MapGet("/queue", GetQueueAsync);
        music.MapPut("/queue", ReplaceQueueAsync);
        music.MapDelete("/queue", ClearQueueAsync);

        music.MapGet("/playlists", GetPlaylistsAsync);
        music.MapGet("/playlists/{id:long}", GetPlaylistAsync);
        music.MapPost("/playlists", CreatePlaylistAsync);
        music.MapPut("/playlists/{id:long}", RenamePlaylistAsync);
        music.MapPost("/playlists/{id:long}/tracks", AddPlaylistTrackAsync);
        music.MapDelete("/playlists/{id:long}/tracks/{trackId:long}", RemovePlaylistTrackAsync);
        music.MapDelete("/playlists/{id:long}", DeletePlaylistAsync);
        return endpoints;
    }

    private static async Task<IResult> ServeArtworkAsync(long id, HttpContext http, IMusicCatalog catalog, CancellationToken ct)
    { var file = await catalog.GetAlbumArtworkAsync(id, ct); if (file is null) return Results.NotFound(); http.Response.Headers.ETag = file.ETag; http.Response.Headers.CacheControl = "public,max-age=31536000,immutable"; return Results.File(file.Path, file.ContentType); }

    private static async Task<IResult> GetLyricsAsync(long id, IMusicDbContext db, CancellationToken ct)
    { var lyrics = await db.MusicLyrics.AsNoTracking().SingleOrDefaultAsync(x => x.TrackId == id, ct); return lyrics is null ? Results.NotFound() : Results.Ok(new MusicLyricsDto(id, lyrics.Text, lyrics.IsSynchronized, lyrics.Source)); }

    private static async Task<IResult> RecordPlayAsync(long id, RecordPlayRequest request, ClaimsPrincipal user, IMusicDbContext db, CancellationToken ct)
    { if (request.PositionMilliseconds < 0 || !await db.MusicTracks.AnyAsync(x => x.Id == id, ct)) return Results.Problem(statusCode: 400, title: "Invalid play event"); db.MusicPlayHistory.Add(MusicPlayHistory.Create(AccountId(user), id, request.PositionMilliseconds, request.Completed)); await db.SaveChangesAsync(ct); return Results.NoContent(); }

    private static async Task<IResult> GetFavoritesAsync(ClaimsPrincipal user, IMusicDbContext db, IMusicCatalog catalog, CancellationToken ct)
    { var ids = await db.MusicFavorites.AsNoTracking().Where(x => x.AccountId == AccountId(user)).OrderByDescending(x => x.CreatedAtUtc).Select(x => x.TrackId).ToArrayAsync(ct); return Results.Ok(await catalog.GetTracksByIdsAsync(ids, ct)); }
    private static async Task<IResult> AddFavoriteAsync(long trackId, ClaimsPrincipal user, IMusicDbContext db, CancellationToken ct)
    { var accountId = AccountId(user); if (!await db.MusicTracks.AnyAsync(x => x.Id == trackId, ct)) return Results.NotFound(); if (!await db.MusicFavorites.AnyAsync(x => x.AccountId == accountId && x.TrackId == trackId, ct)) { db.MusicFavorites.Add(MusicFavorite.Create(accountId, trackId)); await db.SaveChangesAsync(ct); } return Results.NoContent(); }
    private static async Task<IResult> RemoveFavoriteAsync(long trackId, ClaimsPrincipal user, IMusicDbContext db, CancellationToken ct)
    { var item = await db.MusicFavorites.SingleOrDefaultAsync(x => x.AccountId == AccountId(user) && x.TrackId == trackId, ct); if (item is not null) { db.MusicFavorites.Remove(item); await db.SaveChangesAsync(ct); } return Results.NoContent(); }

    private static async Task<IResult> GetHistoryAsync(int? limit, ClaimsPrincipal user, IMusicDbContext db, IMusicCatalog catalog, CancellationToken ct)
    { var ids = await db.MusicPlayHistory.AsNoTracking().Where(x => x.AccountId == AccountId(user)).OrderByDescending(x => x.PlayedAtUtc).Take(Math.Clamp(limit ?? 50, 1, 200)).Select(x => x.TrackId).ToArrayAsync(ct); return Results.Ok(await catalog.GetTracksByIdsAsync(ids.Distinct().ToArray(), ct)); }

    private static async Task<IResult> GetListeningStatsAsync(ClaimsPrincipal user, IMusicDbContext db, CancellationToken ct)
    {
        var accountId = AccountId(user);
        var listens = await db.MusicPlayHistory.AsNoTracking().CountAsync(x => x.AccountId == accountId, ct);
        var completed = await db.MusicPlayHistory.AsNoTracking().CountAsync(x => x.AccountId == accountId && x.Completed, ct);
        return Results.Ok(new MusicListeningStatsDto(listens, completed));
    }

    private static async Task<IResult> GetQueueAsync(ClaimsPrincipal user, IMusicDbContext db, IMusicCatalog catalog, CancellationToken ct)
    { var ids = await db.MusicQueueItems.AsNoTracking().Where(x => x.AccountId == AccountId(user)).OrderBy(x => x.Position).Select(x => x.TrackId).ToArrayAsync(ct); return Results.Ok(await catalog.GetTracksByIdsAsync(ids, ct)); }
    private static async Task<IResult> ReplaceQueueAsync(ReplaceQueueRequest request, ClaimsPrincipal user, IMusicDbContext db, CancellationToken ct)
    { var ids = request.TrackIds.Take(1000).ToArray(); var uniqueIds = ids.Distinct().ToArray(); if (await db.MusicTracks.CountAsync(x => uniqueIds.Contains(x.Id), ct) != uniqueIds.Length) return Results.Problem(statusCode: 400, title: "Queue contains unknown tracks"); var accountId = AccountId(user); db.MusicQueueItems.RemoveRange(db.MusicQueueItems.Where(x => x.AccountId == accountId)); for (var i = 0; i < ids.Length; i++) db.MusicQueueItems.Add(MusicQueueItem.Create(accountId, ids[i], i)); await db.SaveChangesAsync(ct); return Results.NoContent(); }
    private static async Task<IResult> ClearQueueAsync(ClaimsPrincipal user, IMusicDbContext db, CancellationToken ct)
    { db.MusicQueueItems.RemoveRange(db.MusicQueueItems.Where(x => x.AccountId == AccountId(user))); await db.SaveChangesAsync(ct); return Results.NoContent(); }

    private static async Task<IResult> GetPlaylistsAsync(ClaimsPrincipal user, IMusicDbContext db, IMusicCatalog catalog, CancellationToken ct)
    { var values = await db.MusicPlaylists.AsNoTracking().Where(x => x.AccountId == AccountId(user)).OrderBy(x => x.Name).ToArrayAsync(ct); var result = new List<MusicPlaylistDto>(); foreach (var value in values) result.Add(await MapPlaylistAsync(value, db, catalog, ct)); return Results.Ok(result); }
    private static async Task<IResult> GetPlaylistAsync(long id, ClaimsPrincipal user, IMusicDbContext db, IMusicCatalog catalog, CancellationToken ct)
    { var value = await db.MusicPlaylists.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.AccountId == AccountId(user), ct); return value is null ? Results.NotFound() : Results.Ok(await MapPlaylistAsync(value, db, catalog, ct)); }
    private static async Task<IResult> CreatePlaylistAsync(CreatePlaylistRequest request, ClaimsPrincipal user, IMusicDbContext db, CancellationToken ct)
    { if (!ValidName(request.Name)) return Results.Problem(statusCode: 400, title: "Playlist name must contain 1-160 characters"); var accountId = AccountId(user); if (await db.MusicPlaylists.AnyAsync(x => x.AccountId == accountId && x.Name == request.Name.Trim(), ct)) return Results.Conflict(); var item = MusicPlaylist.Create(accountId, request.Name); db.MusicPlaylists.Add(item); await db.SaveChangesAsync(ct); return Results.Created($"/api/v2/music/playlists/{item.Id}", new { item.Id, item.Name }); }
    private static async Task<IResult> RenamePlaylistAsync(long id, RenamePlaylistRequest request, ClaimsPrincipal user, IMusicDbContext db, CancellationToken ct)
    { if (!ValidName(request.Name)) return Results.Problem(statusCode: 400, title: "Playlist name must contain 1-160 characters"); var accountId = AccountId(user); var item = await db.MusicPlaylists.SingleOrDefaultAsync(x => x.Id == id && x.AccountId == accountId, ct); if (item is null) return Results.NotFound(); var name = request.Name.Trim(); if (await db.MusicPlaylists.AnyAsync(x => x.AccountId == accountId && x.Id != id && x.Name == name, ct)) return Results.Conflict(); item.Rename(name); await db.SaveChangesAsync(ct); return Results.NoContent(); }
    private static async Task<IResult> AddPlaylistTrackAsync(long id, AddPlaylistTrackRequest request, ClaimsPrincipal user, IMusicDbContext db, CancellationToken ct)
    { if (await OwnedPlaylistAsync(id, user, db, ct) is null || !await db.MusicTracks.AnyAsync(x => x.Id == request.TrackId, ct)) return Results.NotFound(); if (await db.MusicPlaylistTracks.AnyAsync(x => x.PlaylistId == id && x.TrackId == request.TrackId, ct)) return Results.NoContent(); var position = await db.MusicPlaylistTracks.Where(x => x.PlaylistId == id).Select(x => (int?)x.Position).MaxAsync(ct) ?? -1; db.MusicPlaylistTracks.Add(MusicPlaylistTrack.Create(id, request.TrackId, position + 1)); await db.SaveChangesAsync(ct); return Results.NoContent(); }
    private static async Task<IResult> RemovePlaylistTrackAsync(long id, long trackId, ClaimsPrincipal user, IMusicDbContext db, CancellationToken ct)
    { if (await OwnedPlaylistAsync(id, user, db, ct) is null) return Results.NotFound(); var item = await db.MusicPlaylistTracks.SingleOrDefaultAsync(x => x.PlaylistId == id && x.TrackId == trackId, ct); if (item is not null) { db.MusicPlaylistTracks.Remove(item); await db.SaveChangesAsync(ct); } return Results.NoContent(); }
    private static async Task<IResult> DeletePlaylistAsync(long id, ClaimsPrincipal user, IMusicDbContext db, CancellationToken ct)
    { var item = await OwnedPlaylistAsync(id, user, db, ct); if (item is null) return Results.NotFound(); db.MusicPlaylists.Remove(item); await db.SaveChangesAsync(ct); return Results.NoContent(); }

    private static async Task<MusicPlaylistDto> MapPlaylistAsync(MusicPlaylist value, IMusicDbContext db, IMusicCatalog catalog, CancellationToken ct)
    { var ids = await db.MusicPlaylistTracks.AsNoTracking().Where(x => x.PlaylistId == value.Id).OrderBy(x => x.Position).Select(x => x.TrackId).ToArrayAsync(ct); return new(value.Id, value.Name, await catalog.GetTracksByIdsAsync(ids, ct), value.UpdatedAtUtc ?? value.CreatedAtUtc); }
    private static Task<MusicPlaylist?> OwnedPlaylistAsync(long id, ClaimsPrincipal user, IMusicDbContext db, CancellationToken ct) => db.MusicPlaylists.SingleOrDefaultAsync(x => x.Id == id && x.AccountId == AccountId(user), ct);
    private static bool ValidName(string? name) => !string.IsNullOrWhiteSpace(name) && name.Trim().Length <= 160;
    private static Guid AccountId(ClaimsPrincipal user) => Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub"), out var id) ? id : throw new UnauthorizedAccessException();
}
