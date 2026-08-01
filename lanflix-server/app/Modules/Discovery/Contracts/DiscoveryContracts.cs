namespace Lanflix.Modules.Discovery;

public sealed record DiscoveryItemDto(
    int TmdbId, string Type, string Title, string? Overview, int? Year,
    double Rating, string? PosterUrl, string? BackdropUrl);
public sealed record DiscoveryPageDto(
    IReadOnlyList<DiscoveryItemDto> TrendingMovies,
    IReadOnlyList<DiscoveryItemDto> TrendingSeries,
    IReadOnlyList<DiscoveryItemDto> PopularMovies,
    IReadOnlyList<DiscoveryItemDto> PopularSeries);
public sealed record DiscoverySearchDto(
    IReadOnlyList<DiscoveryItemDto> Movies, IReadOnlyList<DiscoveryItemDto> Series);
public sealed record AcquireMediaRequest(string Type, string Title, int? Year);
public sealed record AcquisitionResult(bool Accepted, string Code, string Message, int? ProviderId);
public sealed record ServiceConnectionDto(string Service, bool Available);

public interface IDiscoveryProvider
{
    Task<DiscoveryPageDto> GetPageAsync(int page, CancellationToken cancellationToken);
    Task<DiscoverySearchDto> SearchAsync(string query, string type, CancellationToken cancellationToken);
    Task<AcquisitionResult> AcquireAsync(int tmdbId, AcquireMediaRequest request, CancellationToken cancellationToken);
    Task<ServiceConnectionDto> TestConnectionAsync(string service, CancellationToken cancellationToken);
}
