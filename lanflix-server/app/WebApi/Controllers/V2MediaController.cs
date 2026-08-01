using System.Security.Cryptography;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Lanflix.WebApi.Controllers;

/// <summary>
/// Stable, normalized media contract for new native and web clients.
/// Existing v1 controllers remain untouched during migration.
/// </summary>
[ApiController]
[Route("api/v2")]
public sealed class V2MediaController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ITmdbClient _tmdb;
    private readonly IMemoryCache _cache;

    public V2MediaController(IApplicationDbContext db, ITmdbClient tmdb, IMemoryCache cache)
    {
        _db = db;
        _tmdb = tmdb;
        _cache = cache;
    }

    [HttpGet("status")]
    public IActionResult GetStatus() => Ok(new
    {
        apiVersion = "2.0",
        serverTimeUtc = DateTime.UtcNow,
        capabilities = new
        {
            movies = true,
            series = true,
            offlineDownloads = true,
            liveTv = false,
            music = false,
            social = false
        }
    });

    [HttpGet("home")]
    public async Task<ActionResult<V2HomeResponse>> GetHome(
        [FromQuery] int? profileId = null,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 50);
        var recent = await _db.Contents.AsNoTracking()
            .OrderByDescending(content => content.AddedAt)
            .Take(limit)
            .Select(content => new V2MediaItem(
                content.Id,
                content.TmdbId,
                content.Type == ContentType.Movie ? "movie" : "series",
                content.Title,
                content.Overview,
                content.ReleaseDate.HasValue ? content.ReleaseDate.Value.Year : null,
                content.Rating,
                content.Genres ?? Array.Empty<string>(),
                ArtworkUrl(content.Id, content.PosterPath, "poster"),
                ArtworkUrl(content.Id, content.BackdropPath, "backdrop"),
                !string.IsNullOrWhiteSpace(content.FilePath),
                null))
            .ToListAsync(cancellationToken);

        var continueWatching = new List<V2MediaItem>();
        if (profileId.HasValue)
        {
            continueWatching = await _db.WatchHistories.AsNoTracking()
                .Where(history => history.ProfileId == profileId.Value && history.Content != null)
                .OrderByDescending(history => history.LastWatchedAt)
                .Take(limit)
                .Select(history => new V2MediaItem(
                    history.ContentId,
                    history.Content!.TmdbId,
                    history.Content.Type == ContentType.Movie ? "movie" : "series",
                    history.Content.Title,
                    history.Content.Overview,
                    history.Content.ReleaseDate.HasValue ? history.Content.ReleaseDate.Value.Year : null,
                history.Content.Rating,
                    history.Content.Genres ?? Array.Empty<string>(),
                    ArtworkUrl(history.ContentId, history.Content.PosterPath, "poster"),
                    ArtworkUrl(history.ContentId, history.Content.BackdropPath, "backdrop"),
                    !string.IsNullOrWhiteSpace(history.Content.FilePath),
                    history.WatchedPercentage))
                .ToListAsync(cancellationToken);
        }

        return Ok(new V2HomeResponse(continueWatching, recent, recent.FirstOrDefault()));
    }

    [HttpGet("library")]
    public async Task<ActionResult<V2Page<V2MediaItem>>> GetLibrary(
        [FromQuery] string? type = null,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        offset = Math.Max(0, offset);
        limit = Math.Clamp(limit, 1, 100);
        var query = _db.Contents.AsNoTracking();
        if (type?.Equals("movie", StringComparison.OrdinalIgnoreCase) == true)
            query = query.Where(content => content.Type == ContentType.Movie);
        else if (type?.Equals("series", StringComparison.OrdinalIgnoreCase) == true)
            query = query.Where(content => content.Type == ContentType.Series);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(content => content.AddedAt)
            .Skip(offset).Take(limit)
            .Select(content => new V2MediaItem(
                content.Id,
                content.TmdbId,
                content.Type == ContentType.Movie ? "movie" : "series",
                content.Title,
                content.Overview,
                content.ReleaseDate.HasValue ? content.ReleaseDate.Value.Year : null,
                content.Rating,
                content.Genres ?? Array.Empty<string>(),
                ArtworkUrl(content.Id, content.PosterPath, "poster"),
                ArtworkUrl(content.Id, content.BackdropPath, "backdrop"),
                !string.IsNullOrWhiteSpace(content.FilePath),
                null))
            .ToListAsync(cancellationToken);

        return Ok(new V2Page<V2MediaItem>(items, total, offset, limit));
    }

    [HttpGet("content/{id:int}/download-manifest")]
    public async Task<ActionResult<V2DownloadManifest>> GetDownloadManifest(
        int id,
        CancellationToken cancellationToken = default)
    {
        var content = await _db.Contents.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (content is null || string.IsNullOrWhiteSpace(content.FilePath) || !System.IO.File.Exists(content.FilePath))
            return NotFound(new ProblemDetails { Title = "Media file unavailable", Status = StatusCodes.Status404NotFound });

        var file = new FileInfo(content.FilePath);
        await using var stream = file.OpenRead();
        using var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(stream, cancellationToken);
        var checksum = Convert.ToHexString(hash).ToLowerInvariant();

        return Ok(new V2DownloadManifest(
            content.Id,
            "movie",
            content.Title,
            file.Length,
            MimeType(file.Extension),
            checksum,
            $"/api/stream/movie/{content.Id}/file",
            file.LastWriteTimeUtc));
    }

    [HttpGet("artwork/{id:int}/logo")]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> GetLogoArtwork(int id, CancellationToken cancellationToken = default)
    {
        var content = await _db.Contents.AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new { item.TmdbId, item.Type })
            .FirstOrDefaultAsync(cancellationToken);
        if (content is null || content.TmdbId <= 0) return NotFound();

        var cacheKey = $"tmdb-logo:{content.Type}:{content.TmdbId}";
        if (!_cache.TryGetValue(cacheKey, out string? path))
        {
            path = await _tmdb.GetLogoPathAsync(
                content.TmdbId,
                content.Type == ContentType.Series,
                cancellationToken: cancellationToken) ?? string.Empty;
            _cache.Set(cacheKey, path, TimeSpan.FromHours(path.Length == 0 ? 1 : 24));
        }

        return string.IsNullOrWhiteSpace(path)
            ? NotFound()
            : Redirect($"https://image.tmdb.org/t/p/w500{path}");
    }

    private static string? ArtworkUrl(int id, string? path, string kind)
    {
        if (string.IsNullOrWhiteSpace(path)) return $"/api/image/{id}/{kind}";
        if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return path;
        if (path.StartsWith('/'))
            return $"https://image.tmdb.org/t/p/{(kind == "poster" ? "w500" : "w1280")}{path}";
        return $"/api/image/{id}/{kind}";
    }

    private static string MimeType(string extension) => extension.ToLowerInvariant() switch
    {
        ".mkv" => "video/x-matroska",
        ".webm" => "video/webm",
        ".m4v" => "video/x-m4v",
        _ => "video/mp4"
    };
}

public sealed record V2MediaItem(
    int Id, int TmdbId, string Type, string Title, string? Overview, int? Year,
    double? Rating, string[] Genres, string? PosterUrl, string? BackdropUrl,
    bool ServerAvailable, double? ProgressPercentage);

public sealed record V2HomeResponse(
    IReadOnlyList<V2MediaItem> ContinueWatching,
    IReadOnlyList<V2MediaItem> RecentlyAdded,
    V2MediaItem? Hero);

public sealed record V2Page<T>(IReadOnlyList<T> Items, int Total, int Offset, int Limit);

public sealed record V2DownloadManifest(
    int Id, string Type, string Title, long FileSize, string MimeType,
    string Sha256, string DownloadUrl, DateTime LastModifiedUtc);
