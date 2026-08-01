using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Common.Models;
using Lanflix.Modules.Discovery;

namespace Lanflix.Infrastructure.Adapters.Discovery;

internal sealed class ExternalDiscoveryProvider(
    ITmdbClient tmdb,
    IRadarrClient radarr,
    ISonarrClient sonarr,
    IProwlarrClient prowlarr,
    ISettingsService settings) : IDiscoveryProvider
{
    public async Task<DiscoveryPageDto> GetPageAsync(int page, CancellationToken cancellationToken)
    {
        var trendingMovies = tmdb.GetTrendingAsync("movie", "week", cancellationToken);
        var trendingSeries = tmdb.GetTrendingAsync("tv", "week", cancellationToken);
        var popularMovies = tmdb.GetPopularMoviesAsync(page, cancellationToken);
        var popularSeries = tmdb.GetPopularTvSeriesAsync(page, cancellationToken);
        await Task.WhenAll(trendingMovies, trendingSeries, popularMovies, popularSeries);
        return new DiscoveryPageDto(
            trendingMovies.Result.Results.Select(item => Map(item, "movie")).ToArray(),
            trendingSeries.Result.Results.Select(item => Map(item, "series")).ToArray(),
            popularMovies.Result.Results.Select(item => Map(item, "movie")).ToArray(),
            popularSeries.Result.Results.Select(item => Map(item, "series")).ToArray());
    }

    public async Task<DiscoverySearchDto> SearchAsync(string query, string type, CancellationToken cancellationToken)
    {
        var includeMovies = type is "all" or "movie";
        var includeSeries = type is "all" or "series" or "tv";
        var movieTask = includeMovies ? tmdb.SearchMoviesAsync(query, cancellationToken) : Task.FromResult(new TmdbSearchResult());
        var seriesTask = includeSeries ? tmdb.SearchTvSeriesAsync(query, cancellationToken) : Task.FromResult(new TmdbSearchResult());
        await Task.WhenAll(movieTask, seriesTask);
        return new DiscoverySearchDto(movieTask.Result.Results.Select(item => Map(item, "movie")).ToArray(),
            seriesTask.Result.Results.Select(item => Map(item, "series")).ToArray());
    }

    public async Task<AcquisitionResult> AcquireAsync(int tmdbId, AcquireMediaRequest request, CancellationToken cancellationToken)
    {
        var current = await settings.GetSettingsAsync(cancellationToken);
        if (request.Type.Equals("movie", StringComparison.OrdinalIgnoreCase))
        {
            var existing = await radarr.GetMovieByTmdbIdAsync(tmdbId, cancellationToken);
            if (existing is not null) return new(true, "already-present", "Movie already exists in Radarr", existing.Id);
            var roots = await radarr.GetRootFoldersAsync(cancellationToken);
            var qualities = await radarr.GetQualityProfilesAsync(cancellationToken);
            var root = SelectRoot(current.MediaPaths.Movies, roots.Select(item => item.Path));
            if (root is null || qualities.Count == 0) return new(false, "radarr-not-ready", "Configure a matching Radarr root folder and quality profile", null);
            var movie = await radarr.AddMovieAsync(new AddRadarrMovieRequest
            {
                TmdbId = tmdbId, Title = request.Title, Year = request.Year ?? DateTime.UtcNow.Year,
                RootFolderPath = root, QualityProfileId = qualities[0].Id, Monitored = true, SearchForMovie = true
            }, cancellationToken);
            return new(true, "queued", "Movie queued in Radarr", movie.Id);
        }

        if (!request.Type.Equals("series", StringComparison.OrdinalIgnoreCase))
            return new(false, "invalid-type", "Type must be movie or series", null);
        var matches = await sonarr.SearchSeriesAsync(request.Title, cancellationToken);
        var match = matches.OrderByDescending(item => string.Equals(item.Title, request.Title, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        if (match is null) return new(false, "not-found", "Series was not found", null);
        var present = await sonarr.GetSeriesByTvdbIdAsync(match.TvdbId, cancellationToken);
        if (present is not null) return new(true, "already-present", "Series already exists in Sonarr", present.Id);
        var seriesRoots = await sonarr.GetRootFoldersAsync(cancellationToken);
        var seriesQualities = await sonarr.GetQualityProfilesAsync(cancellationToken);
        var seriesRoot = SelectRoot(current.MediaPaths.Series, seriesRoots.Select(item => item.Path));
        if (seriesRoot is null || seriesQualities.Count == 0) return new(false, "sonarr-not-ready", "Configure a matching Sonarr root folder and quality profile", null);
        var series = await sonarr.AddSeriesAsync(new AddSonarrSeriesRequest
        {
            TvdbId = match.TvdbId, Title = match.Title, RootFolderPath = seriesRoot,
            QualityProfileId = seriesQualities[0].Id, Monitored = true, SearchForMissingEpisodes = true
        }, cancellationToken);
        return new(true, "queued", "Series queued in Sonarr", series.Id);
    }

    public async Task<ServiceConnectionDto> TestConnectionAsync(string service, CancellationToken cancellationToken)
    {
        var available = service.ToLowerInvariant() switch
        {
            "tmdb" => (await tmdb.SearchMoviesAsync("Lanflix", cancellationToken)).Results.Count >= 0,
            "radarr" => await radarr.TestConnectionAsync(cancellationToken),
            "sonarr" => await sonarr.TestConnectionAsync(cancellationToken),
            "prowlarr" => await prowlarr.TestConnectionAsync(cancellationToken),
            _ => false
        };
        return new ServiceConnectionDto(service.ToLowerInvariant(), available);
    }

    private static DiscoveryItemDto Map(TmdbSearchItem item, string type) => new(
        item.Id, type, item.NormalizedTitle ?? "Untitled", item.Overview,
        (type == "movie" ? item.ReleaseDate : item.FirstAirDate)?.Year,
        item.VoteAverage, item.PosterUrl, item.BackdropUrl);

    private static string? SelectRoot(string configured, IEnumerable<string> available)
    {
        var roots = available.ToArray();
        if (string.IsNullOrWhiteSpace(configured)) return roots.FirstOrDefault();
        return roots.FirstOrDefault(path => string.Equals(path.TrimEnd('\\', '/'), configured.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase));
    }
}
