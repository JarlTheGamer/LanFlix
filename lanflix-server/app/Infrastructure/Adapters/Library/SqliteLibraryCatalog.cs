using System.Security.Cryptography;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Domain.Entities;
using Lanflix.Domain.Enums;
using Lanflix.Infrastructure.Persistence;
using Lanflix.Modules.Library;
using Lanflix.Modules.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Lanflix.Infrastructure.Adapters.Library;

/// <summary>
/// Reads the current SQLite media catalog through the Library module contract.
/// The implementation stays outside the module so its API remains persistence-agnostic.
/// </summary>
internal sealed class SqliteLibraryCatalog(
    ApplicationDbContext db,
    ITmdbClient tmdb,
    IMetadataService metadata,
    IMemoryCache cache,
    ArtworkPaletteService palettes) : ILibraryCatalog
{
    public async Task<HomeDto> GetHomeAsync(Guid accountId, int limit, CancellationToken cancellationToken)
    {
        var progress = await db.PlaybackProgress.AsNoTracking()
            .Where(item => item.AccountId == accountId && !item.Completed && item.PositionMilliseconds > 0)
            .OrderByDescending(item => item.UpdatedAtUtc ?? item.CreatedAtUtc)
            .Take(limit * 4)
            .ToListAsync(cancellationToken);

        var movieProgress = progress
            .Where(item => item.MediaKind == "movie")
            .GroupBy(item => item.MediaId)
            .ToDictionary(group => group.Key, group => group.First());
        var episodeProgress = progress
            .Where(item => item.MediaKind == "episode")
            .GroupBy(item => item.MediaId)
            .ToDictionary(group => group.Key, group => group.First());

        var movieEntities = movieProgress.Count == 0
            ? []
            : await db.Contents.AsNoTracking()
                .Where(item => movieProgress.Keys.Contains(item.Id))
                .ToListAsync(cancellationToken);
        var episodes = episodeProgress.Count == 0
            ? []
            : await db.Episodes.AsNoTracking()
                .Where(item => episodeProgress.Keys.Contains(item.Id))
                .Select(item => new { item.Id, item.ContentId })
                .ToListAsync(cancellationToken);
        var episodeSeriesIds = episodes.Select(item => item.ContentId).Distinct().ToArray();
        var seriesEntities = episodeSeriesIds.Length == 0
            ? []
            : await db.Contents.AsNoTracking()
                .Where(item => episodeSeriesIds.Contains(item.Id))
                .ToListAsync(cancellationToken);

        var movieById = movieEntities.ToDictionary(item => item.Id);
        var seriesById = seriesEntities.ToDictionary(item => item.Id);
        var episodeToSeries = episodes.ToDictionary(item => item.Id, item => item.ContentId);
        var includedSeries = new HashSet<int>();
        var continueWatching = new List<MediaItemDto>(limit);
        foreach (var item in progress)
        {
            Content? content = null;
            if (item.MediaKind == "movie")
                movieById.TryGetValue(item.MediaId, out content);
            else if (item.MediaKind == "episode"
                && episodeToSeries.TryGetValue(item.MediaId, out var seriesId)
                && includedSeries.Add(seriesId))
                seriesById.TryGetValue(seriesId, out content);

            if (content is null) continue;
            var media = await MapAsync(content, cancellationToken);
            continueWatching.Add(media with { ProgressPercentage = Percentage(item.PositionMilliseconds, item.DurationMilliseconds) });
            if (continueWatching.Count == limit) break;
        }

        var entities = await db.Contents.AsNoTracking()
            .OrderByDescending(item => item.AddedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
        var recent = await MapAsync(entities, cancellationToken);
        return new HomeDto(continueWatching, recent, continueWatching.FirstOrDefault() ?? recent.FirstOrDefault());
    }

    public async Task<PageDto<MediaItemDto>> GetLibraryAsync(string? type, int offset, int limit, CancellationToken cancellationToken)
    {
        var query = db.Contents.AsNoTracking();
        query = type?.ToLowerInvariant() switch
        {
            "movie" => query.Where(item => item.Type == ContentType.Movie),
            "series" => query.Where(item => item.Type == ContentType.Series),
            _ => query
        };

        var total = await query.CountAsync(cancellationToken);
        var entities = await query.OrderByDescending(item => item.AddedAt)
            .Skip(offset).Take(limit).ToListAsync(cancellationToken);
        return new PageDto<MediaItemDto>(await MapAsync(entities, cancellationToken), total, offset, limit);
    }

    public async Task<MediaDetailDto?> GetDetailAsync(int id, CancellationToken cancellationToken)
    {
        var content = await db.Contents.AsNoTracking()
            .Include(item => item.Episodes)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (content is null) return null;

        var media = await MapAsync(content, cancellationToken);
        var seasons = content.Episodes
            .OrderBy(episode => episode.SeasonNumber)
            .ThenBy(episode => episode.EpisodeNumber)
            .GroupBy(episode => episode.SeasonNumber)
            .Select(group => new SeasonDto(group.Key, group.Select(MapEpisode).ToArray()))
            .ToArray();
        return new MediaDetailDto(media, seasons);
    }

    public async Task<IReadOnlyList<MediaItemDto>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0) return Array.Empty<MediaItemDto>();
        var order = ids.Select((id, index) => (id, index)).ToDictionary(item => item.id, item => item.index);
        var entities = await db.Contents.AsNoTracking().Where(item => ids.Contains(item.Id)).ToListAsync(cancellationToken);
        entities.Sort((left, right) => order[left.Id].CompareTo(order[right.Id]));
        return await MapAsync(entities, cancellationToken);
    }

    public async Task<DownloadManifestDto?> GetDownloadManifestAsync(int id, CancellationToken cancellationToken)
    {
        var content = await db.Contents.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (content is null || string.IsNullOrWhiteSpace(content.FilePath) || !File.Exists(content.FilePath)) return null;

        var file = new FileInfo(content.FilePath);
        await using var stream = file.OpenRead();
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return new DownloadManifestDto(content.Id, Type(content), content.Title, file.Length, MimeType(file.Extension),
            Convert.ToHexString(hash).ToLowerInvariant(), $"/api/v2/playback/movie/{content.Id}/file", file.LastWriteTimeUtc);
    }

    public async Task<string?> GetLogoRedirectAsync(int id, CancellationToken cancellationToken)
    {
        var content = await db.Contents.AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new { item.TmdbId, item.Type })
            .SingleOrDefaultAsync(cancellationToken);
        if (content is null || content.TmdbId <= 0) return null;

        var key = $"title-logo:{content.Type}:{content.TmdbId}";
        if (!cache.TryGetValue(key, out string? path))
        {
            path = await tmdb.GetLogoPathAsync(content.TmdbId, content.Type == ContentType.Series, cancellationToken: cancellationToken);
            cache.Set(key, path ?? string.Empty, new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(string.IsNullOrWhiteSpace(path) ? 1 : 24))
                .SetSize(1));
        }
        return string.IsNullOrWhiteSpace(path) ? null : $"https://image.tmdb.org/t/p/w500{path}";
    }

    public async Task<ArtworkFileDto?> GetContentArtworkAsync(int contentId, string kind, CancellationToken cancellationToken)
    {
        if (kind is not ("poster" or "backdrop")) return null;
        var filePath = await db.Contents.AsNoTracking().Where(item => item.Id == contentId)
            .Select(item => item.FilePath).SingleOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(filePath)) return null;

        var folder = Directory.Exists(filePath) ? filePath : Path.GetDirectoryName(filePath);
        if (string.IsNullOrWhiteSpace(folder)) return null;
        return CreateArtworkFile(Path.Combine(folder, $"{kind}.jpg"));
    }

    public async Task<ArtworkFileDto?> GetEpisodeArtworkAsync(int episodeId, CancellationToken cancellationToken)
    {
        var episode = await db.Episodes.Include(item => item.Content)
            .SingleOrDefaultAsync(item => item.Id == episodeId, cancellationToken);
        if (episode is null) return null;

        if (!string.IsNullOrWhiteSpace(episode.StillPath) && File.Exists(episode.StillPath))
            return CreateArtworkFile(episode.StillPath);
        if (string.IsNullOrWhiteSpace(episode.FilePath)) return null;
        var folder = Path.GetDirectoryName(episode.FilePath);
        if (string.IsNullOrWhiteSpace(folder)) return null;

        var canonicalStill = Path.Combine(folder, $"S{episode.SeasonNumber:00}E{episode.EpisodeNumber:00}.jpg");
        if (File.Exists(canonicalStill))
        {
            if (!string.Equals(episode.StillPath, canonicalStill, StringComparison.OrdinalIgnoreCase))
            {
                episode.StillPath = canonicalStill;
                await db.SaveChangesAsync(cancellationToken);
            }
            return CreateArtworkFile(canonicalStill);
        }

        var remoteStill = episode.StillPath?.StartsWith('/') == true ? episode.StillPath : null;
        if (remoteStill is null && episode.Content.TmdbId > 0)
        {
            var season = await tmdb.GetSeasonDetailsAsync(episode.Content.TmdbId, episode.SeasonNumber, cancellationToken);
            remoteStill = season?.Episodes.FirstOrDefault(item => item.EpisodeNumber == episode.EpisodeNumber)?.StillPath;
        }
        if (string.IsNullOrWhiteSpace(remoteStill)) return null;

        var downloaded = await metadata.DownloadEpisodeStillAsync(
            remoteStill, folder, episode.SeasonNumber, episode.EpisodeNumber, cancellationToken);
        if (string.IsNullOrWhiteSpace(downloaded) || !File.Exists(downloaded)) return null;
        episode.StillPath = downloaded;
        await db.SaveChangesAsync(cancellationToken);
        return CreateArtworkFile(downloaded);
    }

    private async Task<IReadOnlyList<MediaItemDto>> MapAsync(IEnumerable<Content> entities, CancellationToken cancellationToken)
    {
        var result = new List<MediaItemDto>();
        foreach (var entity in entities) result.Add(await MapAsync(entity, cancellationToken));
        return result;
    }

    private async Task<MediaItemDto> MapAsync(Content content, CancellationToken cancellationToken) => new(
        content.Id,
        content.TmdbId,
        Type(content),
        content.Title,
        content.Overview,
        content.ReleaseDate?.Year,
        content.Rating,
        content.Genres ?? Array.Empty<string>(),
        ArtworkUrl(content.Id, content.PosterPath, "poster"),
        ArtworkUrl(content.Id, content.BackdropPath, "backdrop"),
        content.TmdbId > 0 ? $"/api/v2/artwork/{content.Id}/logo" : null,
        Type(content) == "series" || (!string.IsNullOrWhiteSpace(content.FilePath) && File.Exists(content.FilePath)),
        null,
        await palettes.GetOrCreateAsync(content.Id, content.FilePath, PaletteArtworkReference(content), cancellationToken));

    private static string? PaletteArtworkReference(Content content)
    {
        var path = content.BackdropPath ?? content.PosterPath;
        if (string.IsNullOrWhiteSpace(path)) return null;
        if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase) || File.Exists(path)) return path;
        return path.StartsWith('/') ? $"https://image.tmdb.org/t/p/w780{path}" : null;
    }

    private static EpisodeDto MapEpisode(Episode episode) => new(
        episode.Id, episode.SeasonNumber, episode.EpisodeNumber, episode.Title, episode.Overview,
        EpisodeArtworkUrl(episode.Id, episode.StillPath),
        !string.IsNullOrWhiteSpace(episode.FilePath) && File.Exists(episode.FilePath), null);

    private static string Type(Content content) => content.Type == ContentType.Movie ? "movie" : "series";

    private static double Percentage(long positionMilliseconds, long durationMilliseconds) =>
        durationMilliseconds <= 0 ? 0 : Math.Clamp(positionMilliseconds * 100d / durationMilliseconds, 0, 100);

    private static string? ArtworkUrl(int id, string? path, string kind)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return path;
        if (path.StartsWith('/'))
            return $"https://image.tmdb.org/t/p/{(kind == "poster" ? "w500" : "w1280")}{path}";
        return $"/api/v2/artwork/{id}/{kind}";
    }

    private static string? EpisodeArtworkUrl(int id, string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return path;
        return $"/api/v2/artwork/episode/{id}/still";
    }

    private static ArtworkFileDto? CreateArtworkFile(string path)
    {
        if (!File.Exists(path)) return null;
        var file = new FileInfo(path);
        var contentType = file.Extension.ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };
        return new ArtworkFileDto(path, contentType, $"\"{file.Length:x}-{file.LastWriteTimeUtc.Ticks:x}\"");
    }

    private static string MimeType(string extension) => extension.ToLowerInvariant() switch
    {
        ".mkv" => "video/x-matroska",
        ".webm" => "video/webm",
        ".m4v" => "video/x-m4v",
        _ => "video/mp4"
    };
}
