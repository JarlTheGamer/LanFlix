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
}
