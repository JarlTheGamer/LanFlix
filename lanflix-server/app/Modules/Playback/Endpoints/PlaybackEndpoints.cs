using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;

namespace Lanflix.Modules.Playback;

public static class PlaybackModule
{
    public static IServiceCollection AddPlaybackModule(this IServiceCollection services) => services;

    public static IEndpointRouteBuilder MapPlaybackModule(this IEndpointRouteBuilder endpoints)
    {
        var playback = endpoints.MapGroup("/api/v2/playback")
            .WithTags("Playback")
            .RequireAuthorization();

        playback.MapGet("/{kind:regex(^(movie|episode)$)}/{id:int}", GetInfoAsync);
        playback.MapGet("/{kind:regex(^(movie|episode)$)}/{id:int}/file", StreamAsync);
        playback.MapGet("/{kind:regex(^(movie|episode)$)}/{id:int}/download-manifest", GetDownloadManifestAsync);
        playback.MapPut("/{kind:regex(^(movie|episode)$)}/{id:int}/progress", UpdateProgressAsync);
        playback.MapDelete("/{kind:regex(^(movie|episode)$)}/{id:int}/progress", DeleteProgressAsync);
        return endpoints;
    }

    private static async Task<IResult> GetDownloadManifestAsync(
        string kind, int id, IPlaybackSourceCatalog sources, CancellationToken ct)
    {
        var source = await sources.FindAsync(kind, id, ct);
        if (source is null || !File.Exists(source.FilePath)) return Results.NotFound();
        var file = new FileInfo(source.FilePath);
        await using var stream = file.OpenRead();
        var hash = await SHA256.HashDataAsync(stream, ct);
        return Results.Ok(new PlaybackDownloadManifestDto(id, kind, source.Title, file.Length, source.MimeType,
            Convert.ToHexString(hash).ToLowerInvariant(), $"/api/v2/playback/{kind}/{id}/file", file.LastWriteTimeUtc));
    }

    private static async Task<IResult> GetInfoAsync(
        string kind, int id, ClaimsPrincipal user, IPlaybackSourceCatalog sources, IPlaybackDbContext db, CancellationToken ct)
    {
        var source = await sources.FindAsync(kind, id, ct);
        if (source is null) return Results.Problem(statusCode: 404, title: "Playable media unavailable");
        var accountId = AccountId(user);
        var progressEntity = await db.PlaybackProgress.AsNoTracking()
            .SingleOrDefaultAsync(item => item.AccountId == accountId && item.MediaKind == kind && item.MediaId == id, ct);
        var progress = progressEntity?.ToDto();
        return Results.Ok(new PlaybackInfoDto(source.Id, source.Kind, source.Title,
            $"/api/v2/playback/{kind}/{id}/file", source.MimeType, source.FileSize,
            source.SeasonNumber, source.EpisodeNumber, source.IntroStartSeconds, source.IntroEndSeconds,
            source.CreditsStartSeconds, progress));
    }

    private static async Task<IResult> StreamAsync(
        string kind, int id, string? client, double? startTime, HttpContext context,
        IPlaybackSourceCatalog sources, IAdaptivePlaybackService playback, CancellationToken ct)
    {
        var source = await sources.FindAsync(kind, id, ct);
        if (source is null) return Results.Problem(statusCode: 404, title: "Playable media unavailable");
        var delivery = await playback.OpenAsync(source, client ?? "mobile", startTime,
            context.Request.Headers.Range.FirstOrDefault(), ct);
        context.Response.Headers["X-Playback-Mode"] = delivery.Mode;
        context.Response.Headers["Access-Control-Expose-Headers"] = "X-Playback-Mode";
        if (delivery.RangeStart is not null && delivery.ContentLength is not null)
        {
            context.Response.StatusCode = StatusCodes.Status206PartialContent;
            context.Response.Headers.ContentRange = $"bytes {delivery.RangeStart}-{delivery.RangeEnd ?? delivery.ContentLength - 1}/{delivery.ContentLength}";
        }
        return Results.Stream(delivery.Stream, delivery.ContentType, enableRangeProcessing: delivery.SupportsRanges);
    }

    private static async Task<IResult> UpdateProgressAsync(
        string kind, int id, UpdatePlaybackProgressRequest request, ClaimsPrincipal user, IPlaybackSourceCatalog sources,
        IPlaybackDbContext db, CancellationToken ct)
    {
        if (request.PositionMilliseconds < 0 || request.DurationMilliseconds < 0 ||
            (request.DurationMilliseconds > 0 && request.PositionMilliseconds > request.DurationMilliseconds + 30_000))
            return Results.Problem(statusCode: 400, title: "Invalid playback progress");
        if (await sources.FindAsync(kind, id, ct) is null) return Results.NotFound();

        var accountId = AccountId(user);
        var progress = await db.PlaybackProgress.SingleOrDefaultAsync(
            item => item.AccountId == accountId && item.MediaKind == kind && item.MediaId == id, ct);
        if (progress is null)
        {
            progress = PlaybackProgress.Create(accountId, kind, id);
            db.PlaybackProgress.Add(progress);
        }
        progress.Update(request.PositionMilliseconds, request.DurationMilliseconds, request.Completed);
        await db.SaveChangesAsync(ct);
        return Results.Ok(progress.ToDto());
    }

    private static async Task<IResult> DeleteProgressAsync(
        string kind, int id, ClaimsPrincipal user, IPlaybackDbContext db, CancellationToken ct)
    {
        var accountId = AccountId(user);
        var progress = await db.PlaybackProgress.SingleOrDefaultAsync(
            item => item.AccountId == accountId && item.MediaKind == kind && item.MediaId == id, ct);
        if (progress is null) return Results.NoContent();
        db.PlaybackProgress.Remove(progress);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static Guid AccountId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(value, out var accountId) ? accountId : throw new UnauthorizedAccessException();
    }
}
