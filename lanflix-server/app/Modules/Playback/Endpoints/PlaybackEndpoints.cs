using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using System.Globalization;

namespace Lanflix.Modules.Playback;

public static class PlaybackModule
{
    private const double HlsSegmentDuration = 4.0; // seconds per HLS segment

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

        // HLS adaptive streaming endpoints
        playback.MapGet("/{kind:regex(^(movie|episode)$)}/{id:int}/hls/playlist.m3u8", GetHlsPlaylistAsync);
        playback.MapGet("/{kind:regex(^(movie|episode)$)}/{id:int}/hls/segment/{start}.ts", GetHlsSegmentAsync);

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
        string kind, int id, ClaimsPrincipal user, IPlaybackSourceCatalog sources,
        IPlaybackDbContext db, IAdaptivePlaybackService playback, string? client, CancellationToken ct)
    {
        var source = await sources.FindAsync(kind, id, ct);
        if (source is null) return Results.Problem(statusCode: 404, title: "Playable media unavailable");
        var accountId = AccountId(user);
        var progressEntity = await db.PlaybackProgress.AsNoTracking()
            .SingleOrDefaultAsync(item => item.AccountId == accountId && item.MediaKind == kind && item.MediaId == id, ct);
        var progress = progressEntity?.ToDto();
        var playbackMode = await playback.GetPlaybackModeAsync(source, client ?? "mobile-high", ct);
        return Results.Ok(new PlaybackInfoDto(source.Id, source.Kind, source.Title,
            $"/api/v2/playback/{kind}/{id}/file", source.MimeType, source.FileSize,
            source.SeasonNumber, source.EpisodeNumber, source.IntroStartSeconds, source.IntroEndSeconds,
            source.CreditsStartSeconds, progress, source.DurationSeconds, playbackMode));
    }

    private static async Task<IResult> StreamAsync(
        string kind, int id, string? client, double? startTime, HttpContext context,
        IPlaybackSourceCatalog sources, IAdaptivePlaybackService playback, CancellationToken ct)
    {
        var source = await sources.FindAsync(kind, id, ct);
        if (source is null) return Results.Problem(statusCode: 404, title: "Playable media unavailable");
        if (string.Equals(client, "direct", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Headers["X-Playback-Mode"] = "DirectPlay";
            context.Response.Headers["Access-Control-Expose-Headers"] = "X-Playback-Mode";
            return Results.File(source.FilePath, source.MimeType, enableRangeProcessing: true);
        }
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

    /// <summary>
    /// Generates an HLS M3U8 playlist for a given media item.
    /// Each segment references the on-demand segment endpoint.
    /// </summary>
    private static async Task<IResult> GetHlsPlaylistAsync(
        string kind, int id, string? client, HttpContext context,
        IPlaybackSourceCatalog sources, IAdaptivePlaybackService playback, CancellationToken ct)
    {
        var source = await sources.FindAsync(kind, id, ct);
        if (source is null) return Results.Problem(statusCode: 404, title: "Playable media unavailable");

        // If the DB has no duration, probe it from the file via ffprobe
        var duration = source.DurationSeconds > 0
            ? source.DurationSeconds
            : await playback.ProbeDurationAsync(source.FilePath, ct);

        if (duration <= 0) return Results.Problem(statusCode: 500, title: "Could not determine media duration");

        var clientParam = client ?? "mobile";
        var sb = new StringBuilder();
        sb.AppendLine("#EXTM3U");
        sb.AppendLine("#EXT-X-VERSION:3");
        sb.AppendLine($"#EXT-X-TARGETDURATION:{(int)Math.Ceiling(HlsSegmentDuration)}");
        sb.AppendLine("#EXT-X-MEDIA-SEQUENCE:0");
        sb.AppendLine("#EXT-X-PLAYLIST-TYPE:VOD");
        sb.AppendLine("#EXT-X-INDEPENDENT-SEGMENTS");

        double position = 0;
        while (position < duration)
        {
            var segLen = Math.Min(HlsSegmentDuration, duration - position);
            sb.AppendLine($"#EXTINF:{segLen.ToString("0.0000", CultureInfo.InvariantCulture)},");
            sb.AppendLine($"/api/v2/playback/{kind}/{id}/hls/segment/{position.ToString("0.000", CultureInfo.InvariantCulture)}.ts?client={Uri.EscapeDataString(clientParam)}");
            position += HlsSegmentDuration;
        }
        sb.AppendLine("#EXT-X-ENDLIST");

        context.Response.Headers["Cache-Control"] = "no-cache";
        return Results.Content(sb.ToString(), "application/vnd.apple.mpegurl", Encoding.UTF8);
    }

    /// <summary>
    /// Transcodes a single HLS segment starting at <paramref name="start"/> seconds,
    /// with a duration of <see cref="HlsSegmentDuration"/> seconds.
    /// </summary>
    private static async Task<IResult> GetHlsSegmentAsync(
        string kind, int id, double start, string? client, HttpContext context,
        IPlaybackSourceCatalog sources, IAdaptivePlaybackService playback, CancellationToken ct)
    {
        var source = await sources.FindAsync(kind, id, ct);
        if (source is null) return Results.Problem(statusCode: 404, title: "Playable media unavailable");

        // Use the client type to drive transcoding decision, but force the segment window
        var delivery = await playback.OpenSegmentAsync(source, client ?? "mobile", start, HlsSegmentDuration, ct);
        context.Response.Headers["X-Playback-Mode"] = delivery.Mode;
        context.Response.Headers["Access-Control-Expose-Headers"] = "X-Playback-Mode";
        context.Response.Headers["Cache-Control"] = "public, max-age=31536000, immutable";
        return Results.Stream(delivery.Stream, "video/mp2t", enableRangeProcessing: false);
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
