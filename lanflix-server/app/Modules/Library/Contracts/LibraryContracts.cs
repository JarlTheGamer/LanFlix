using Lanflix.Modules.Metadata;

namespace Lanflix.Modules.Library;

public sealed record MediaItemDto(
    int Id,
    int TmdbId,
    string Type,
    string Title,
    string? Overview,
    int? Year,
    double? Rating,
    IReadOnlyList<string> Genres,
    string? PosterUrl,
    string? BackdropUrl,
    string? LogoUrl,
    bool ServerAvailable,
    double? ProgressPercentage,
    ArtworkPaletteDto Palette);

public sealed record HomeDto(
    IReadOnlyList<MediaItemDto> ContinueWatching,
    IReadOnlyList<MediaItemDto> RecentlyAdded,
    MediaItemDto? Hero);

public sealed record PageDto<T>(IReadOnlyList<T> Items, int Total, int Offset, int Limit);

public sealed record EpisodeDto(
    int Id,
    int SeasonNumber,
    int EpisodeNumber,
    string Title,
    string? Overview,
    string? StillUrl,
    bool ServerAvailable,
    double? ProgressPercentage);

public sealed record SeasonDto(int SeasonNumber, IReadOnlyList<EpisodeDto> Episodes);

public sealed record MediaDetailDto(MediaItemDto Media, IReadOnlyList<SeasonDto> Seasons);

public sealed record DownloadManifestDto(
    int Id,
    string Type,
    string Title,
    long FileSize,
    string MimeType,
    string Sha256,
    string DownloadUrl,
    DateTime LastModifiedUtc);

public sealed record ArtworkFileDto(string Path, string ContentType, string ETag);

public sealed record WatchHistoryDto(
    long Id,
    int MediaId,
    string Kind,
    string Title,
    string? EpisodeTitle,
    string? PosterUrl,
    string? BackdropUrl,
    double ProgressPercentage,
    bool Completed,
    DateTime WatchedAtUtc);

public interface ILibraryCatalog
{
    Task<HomeDto> GetHomeAsync(Guid accountId, int limit, CancellationToken cancellationToken);
    Task<PageDto<MediaItemDto>> GetLibraryAsync(string? type, int offset, int limit, CancellationToken cancellationToken);
    Task<MediaDetailDto?> GetDetailAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<MediaItemDto>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken);
    Task<DownloadManifestDto?> GetDownloadManifestAsync(int id, CancellationToken cancellationToken);
    Task<string?> GetLogoRedirectAsync(int id, CancellationToken cancellationToken);
    Task<ArtworkFileDto?> GetContentArtworkAsync(int contentId, string kind, CancellationToken cancellationToken);
    Task<ArtworkFileDto?> GetEpisodeArtworkAsync(int episodeId, CancellationToken cancellationToken);
    Task<IReadOnlyList<WatchHistoryDto>> GetWatchHistoryAsync(Guid accountId, CancellationToken cancellationToken);
    Task ClearWatchHistoryAsync(Guid accountId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CastMemberDto>> GetCastAsync(int id, CancellationToken cancellationToken);
    Task<ArtworkFileDto?> GetCastProfileArtworkAsync(int contentId, int personId, CancellationToken cancellationToken);
}

public sealed record CastMemberDto(
    int Id,
    string Name,
    string? Character,
    string? ProfileUrl,
    int Order,
    string? SourceProfileUrl = null);
