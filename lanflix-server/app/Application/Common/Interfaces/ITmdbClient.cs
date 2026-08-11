using Lanflix.Application.Common.Models;

namespace Lanflix.Application.Common.Interfaces;

/// <summary>
/// Interface for TMDB (The Movie Database) API client
/// </summary>
public interface ITmdbClient
{
    /// <summary>
    /// Search for movies by title
    /// </summary>
    Task<TmdbSearchResult> SearchMoviesAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search for TV series by title
    /// </summary>
    Task<TmdbSearchResult> SearchTvSeriesAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get detailed movie information by TMDB ID
    /// </summary>
    Task<TmdbMovieDetails?> GetMovieDetailsAsync(int tmdbId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get detailed TV series information by TMDB ID
    /// </summary>
    Task<TmdbTvSeriesDetails?> GetTvSeriesDetailsAsync(int tmdbId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get TV season details
    /// </summary>
    Task<TmdbSeasonDetails?> GetSeasonDetailsAsync(int seriesId, int seasonNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get trending content (movies or TV)
    /// </summary>
    Task<TmdbSearchResult> GetTrendingAsync(string mediaType = "all", string timeWindow = "week", CancellationToken cancellationToken = default);

    /// <summary>
    /// Get popular movies
    /// </summary>
    Task<TmdbSearchResult> GetPopularMoviesAsync(int page = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get popular TV series
    /// </summary>
    Task<TmdbSearchResult> GetPopularTvSeriesAsync(int page = 1, CancellationToken cancellationToken = default);

    /// <summary>Get the best transparent title/logo artwork path for a movie or series.</summary>
    Task<string?> GetLogoPathAsync(
        int tmdbId,
        bool isSeries,
        string language = "en",
        CancellationToken cancellationToken = default);

    /// <summary>Get movie cast credits</summary>
    Task<TmdbCreditsResult?> GetMovieCreditsAsync(int tmdbId, CancellationToken cancellationToken = default);

    /// <summary>Get TV series cast credits</summary>
    Task<TmdbCreditsResult?> GetTvCreditsAsync(int tmdbId, CancellationToken cancellationToken = default);
}
