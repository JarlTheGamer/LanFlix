using System.Security.Cryptography;
using System.Text.Json;
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

        if (content.Type == ContentType.Series && content.TmdbId > 0)
        {
            await EnsureSeriesEpisodesFetchedAsync(content.Id, content.TmdbId, cancellationToken);
            content = await db.Contents.AsNoTracking()
                .Include(item => item.Episodes)
                .SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? content;
        }

        var media = await MapAsync(content, cancellationToken);
        var seasons = content.Episodes
            .OrderBy(episode => episode.SeasonNumber)
            .ThenBy(episode => episode.EpisodeNumber)
            .GroupBy(episode => episode.SeasonNumber)
            .Select(group => new SeasonDto(group.Key, group.Select(MapEpisode).ToArray()))
            .ToArray();
        return new MediaDetailDto(media, seasons);
    }

    private async Task EnsureSeriesEpisodesFetchedAsync(int contentId, int tmdbId, CancellationToken cancellationToken)
    {
        try
        {
            var existingEpisodes = await db.Episodes
                .Where(e => e.ContentId == contentId)
                .ToListAsync(cancellationToken);

            if (existingEpisodes.Count > 0) return;

            var tvDetails = await tmdb.GetTvSeriesDetailsAsync(tmdbId, cancellationToken);
            if (tvDetails?.Seasons is null) return;

            var existingMap = existingEpisodes
                .ToDictionary(e => (e.SeasonNumber, e.EpisodeNumber));

            var newEpisodes = new List<Episode>();
            var modified = false;

            foreach (var seasonSummary in tvDetails.Seasons.Where(s => s.SeasonNumber > 0))
            {
                var seasonDetails = await tmdb.GetSeasonDetailsAsync(tmdbId, seasonSummary.SeasonNumber, cancellationToken);
                if (seasonDetails?.Episodes is null) continue;

                foreach (var tmdbEp in seasonDetails.Episodes)
                {
                    if (existingMap.TryGetValue((tmdbEp.SeasonNumber, tmdbEp.EpisodeNumber), out var dbEp))
                    {
                        if (string.IsNullOrWhiteSpace(dbEp.StillPath) && !string.IsNullOrWhiteSpace(tmdbEp.StillPath))
                        {
                            dbEp.StillPath = tmdbEp.StillPath;
                            modified = true;
                        }
                        if (string.IsNullOrWhiteSpace(dbEp.Overview) && !string.IsNullOrWhiteSpace(tmdbEp.Overview))
                        {
                            dbEp.Overview = tmdbEp.Overview;
                            modified = true;
                        }
                        if (string.IsNullOrWhiteSpace(dbEp.Title) && !string.IsNullOrWhiteSpace(tmdbEp.Name))
                        {
                            dbEp.Title = tmdbEp.Name;
                            modified = true;
                        }
                        if (dbEp.AirDate is null && tmdbEp.AirDate is not null)
                        {
                            dbEp.AirDate = tmdbEp.AirDate;
                            modified = true;
                        }
                    }
                    else
                    {
                        var newEp = new Episode
                        {
                            ContentId = contentId,
                            TmdbId = tmdbEp.Id,
                            SeasonNumber = tmdbEp.SeasonNumber,
                            EpisodeNumber = tmdbEp.EpisodeNumber,
                            Title = tmdbEp.Name,
                            Overview = tmdbEp.Overview,
                            AirDate = tmdbEp.AirDate,
                            StillPath = tmdbEp.StillPath,
                            AddedAt = DateTime.UtcNow
                        };
                        newEpisodes.Add(newEp);
                    }
                }
            }

            if (newEpisodes.Count > 0)
            {
                await db.Episodes.AddRangeAsync(newEpisodes, cancellationToken);
                modified = true;
            }

            if (modified)
            {
                await db.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception)
        {
            // Silently handle TMDB network issues or test doubles that don't support TMDB
        }
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

        var targetFile = Path.Combine(folder, $"{kind}.jpg");
        if (!File.Exists(targetFile))
        {
            try
            {
                await metadata.SaveMetadataToMediaFolderAsync(contentId, folder, cancellationToken);
            }
            catch
            {
                // Continue if auto-download fails
            }
        }

        return CreateArtworkFile(targetFile);
    }

    public async Task<ArtworkFileDto?> GetEpisodeArtworkAsync(int episodeId, CancellationToken cancellationToken)
    {
        var episode = await db.Episodes.Include(item => item.Content)
            .SingleOrDefaultAsync(item => item.Id == episodeId, cancellationToken);
        if (episode is null) return null;

        if (!string.IsNullOrWhiteSpace(episode.StillPath) && File.Exists(episode.StillPath))
            return CreateArtworkFile(episode.StillPath);

        var folders = new List<string>();
        if (!string.IsNullOrWhiteSpace(episode.FilePath))
        {
            var epDir = Path.GetDirectoryName(episode.FilePath);
            if (!string.IsNullOrWhiteSpace(epDir)) folders.Add(epDir);
        }
        if (episode.Content != null && !string.IsNullOrWhiteSpace(episode.Content.FilePath))
        {
            var contentDir = Directory.Exists(episode.Content.FilePath)
                ? episode.Content.FilePath
                : Path.GetDirectoryName(episode.Content.FilePath);

            if (!string.IsNullOrWhiteSpace(contentDir))
            {
                folders.Add(contentDir);
                folders.Add(Path.Combine(contentDir, $"Season {episode.SeasonNumber}"));
                folders.Add(Path.Combine(contentDir, $"Season {episode.SeasonNumber:00}"));
            }
        }

        var filenames = new[]
        {
            $"S{episode.SeasonNumber:00}E{episode.EpisodeNumber:00}.jpg",
            $"S{episode.SeasonNumber:00}E{episode.EpisodeNumber:00}.png",
            $"S{episode.SeasonNumber:01}E{episode.EpisodeNumber:02}.jpg",
            $"S{episode.SeasonNumber:01}E{episode.EpisodeNumber:02}.png",
            $"E{episode.EpisodeNumber:00}.jpg",
            $"E{episode.EpisodeNumber:02}.jpg"
        };

        foreach (var folder in folders.Distinct())
        {
            if (!Directory.Exists(folder)) continue;
            foreach (var filename in filenames)
            {
                var candidate = Path.Combine(folder, filename);
                if (File.Exists(candidate))
                {
                    if (!string.Equals(episode.StillPath, candidate, StringComparison.OrdinalIgnoreCase))
                    {
                        episode.StillPath = candidate;
                        await db.SaveChangesAsync(cancellationToken);
                    }
                    return CreateArtworkFile(candidate);
                }
            }
        }

        return null;
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
        ArtworkUrl(content, "poster"),
        ArtworkUrl(content, "backdrop"),
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

    public async Task<IReadOnlyList<WatchHistoryDto>> GetWatchHistoryAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var progressList = await db.PlaybackProgress.AsNoTracking()
            .Where(item => item.AccountId == accountId)
            .OrderByDescending(item => item.UpdatedAtUtc ?? item.CreatedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        var movieIds = progressList.Where(p => p.MediaKind == "movie").Select(p => p.MediaId).Distinct().ToArray();
        var epIds = progressList.Where(p => p.MediaKind == "episode").Select(p => p.MediaId).Distinct().ToArray();

        var movies = movieIds.Length == 0 ? new Dictionary<int, Content>() : await db.Contents.AsNoTracking().Where(c => movieIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, cancellationToken);
        var episodes = epIds.Length == 0 ? new Dictionary<int, Episode>() : await db.Episodes.AsNoTracking().Include(e => e.Content).Where(e => epIds.Contains(e.Id)).ToDictionaryAsync(e => e.Id, cancellationToken);

        var result = new List<WatchHistoryDto>();
        foreach (var p in progressList)
        {
            if (p.MediaKind == "movie" && movies.TryGetValue(p.MediaId, out var movie))
            {
                result.Add(new WatchHistoryDto(
                    p.Id,
                    p.MediaId,
                    "movie",
                    movie.Title,
                    null,
                    $"/api/v2/artwork/content/{movie.Id}/poster",
                    $"/api/v2/artwork/content/{movie.Id}/backdrop",
                    p.DurationMilliseconds > 0 ? Math.Clamp(p.PositionMilliseconds * 100d / p.DurationMilliseconds, 0, 100) : 0,
                    p.Completed,
                    p.UpdatedAtUtc ?? p.CreatedAtUtc
                ));
            }
            else if (p.MediaKind == "episode" && episodes.TryGetValue(p.MediaId, out var ep))
            {
                result.Add(new WatchHistoryDto(
                    p.Id,
                    p.MediaId,
                    "episode",
                    $"{ep.Content?.Title ?? "Series"} - S{ep.SeasonNumber:00}E{ep.EpisodeNumber:00}",
                    ep.Title,
                    ep.Content != null ? $"/api/v2/artwork/content/{ep.Content.Id}/poster" : null,
                    $"/api/v2/artwork/episode/{ep.Id}/still",
                    p.DurationMilliseconds > 0 ? Math.Clamp(p.PositionMilliseconds * 100d / p.DurationMilliseconds, 0, 100) : 0,
                    p.Completed,
                    p.UpdatedAtUtc ?? p.CreatedAtUtc
                ));
            }
        }
        return result;
    }

    public async Task ClearWatchHistoryAsync(Guid accountId, CancellationToken cancellationToken)
    {
        await db.PlaybackProgress.Where(item => item.AccountId == accountId).ExecuteDeleteAsync(cancellationToken);
    }

    private static string? ArtworkUrl(Content content, string kind)
    {
        return $"/api/v2/artwork/content/{content.Id}/{kind}";
    }

    private static string? EpisodeArtworkUrl(int id, string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return path;
        if (path.StartsWith('/'))
            return $"https://image.tmdb.org/t/p/w500{path}";
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

    public async Task<IReadOnlyList<CastMemberDto>> GetCastAsync(int id, CancellationToken cancellationToken)
    {
        // 1. Try reading cached CastJson via ADO.NET (EF ignores this column)
        string? cachedJson = null;
        var conn = db.Database.GetDbConnection();
        var wasOpen = conn.State == System.Data.ConnectionState.Open;
        if (!wasOpen) await conn.OpenAsync(cancellationToken);
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT CastJson FROM Contents WHERE Id = @id AND IsDeleted = 0 LIMIT 1";
            var p = cmd.CreateParameter(); p.ParameterName = "@id"; p.Value = id;
            cmd.Parameters.Add(p);
            var scalar = await cmd.ExecuteScalarAsync(cancellationToken);
            cachedJson = scalar is DBNull ? null : scalar as string;
        }
        finally { if (!wasOpen) await conn.CloseAsync(); }

        if (!string.IsNullOrEmpty(cachedJson))
        {
            try
            {
                var cached = JsonSerializer.Deserialize<List<CastMemberDto>>(cachedJson);
                if (cached is { Count: > 0 }) return cached;
            }
            catch { /* fall through to TMDB fetch */ }
        }

        // 2. Look up TmdbId and type to call TMDB
        var row = await db.Contents.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new { c.TmdbId, c.Type })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null) return Array.Empty<CastMemberDto>();

        var isSeries = row.Type == ContentType.Series;
        var credits = isSeries
            ? await tmdb.GetTvCreditsAsync(row.TmdbId, cancellationToken)
            : await tmdb.GetMovieCreditsAsync(row.TmdbId, cancellationToken);

        if (credits is null) return Array.Empty<CastMemberDto>();

        var cast = credits.Cast
            .OrderBy(c => c.Order)
            .Take(20)
            .Select(c => new CastMemberDto(c.Id, c.Name, c.Character, c.ProfileUrl, c.Order))
            .ToList();

        // 3. Persist back to the DB column via raw SQL
        try
        {
            var json = JsonSerializer.Serialize(cast);
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE Contents SET CastJson = {0} WHERE Id = {1}",
                json, id);
        }
        catch { /* non-fatal */ }

        return cast;
    }

    private static string MimeType(string extension) => extension.ToLowerInvariant() switch
    {
        ".mkv" => "video/x-matroska",
        ".webm" => "video/webm",
        ".m4v" => "video/x-m4v",
        _ => "video/mp4"
    };
}
