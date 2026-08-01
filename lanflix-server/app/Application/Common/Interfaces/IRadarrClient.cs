using Lanflix.Application.Common.Models;

namespace Lanflix.Application.Common.Interfaces;

/// <summary>
/// Interface for Radarr API client (movie management)
/// </summary>
public interface IRadarrClient
{
    /// <summary>
    /// Test connection to Radarr
    /// </summary>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Search for movies by title
    /// </summary>
    Task<List<RadarrSearchResult>> SearchMoviesAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a movie to Radarr
    /// </summary>
    Task<RadarrMovie> AddMovieAsync(AddRadarrMovieRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all movies
    /// </summary>
    Task<List<RadarrMovie>> GetMoviesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get movie by TMDB ID
    /// </summary>
    Task<RadarrMovie?> GetMovieByTmdbIdAsync(int tmdbId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get download queue
    /// </summary>
    Task<RadarrQueueResponse> GetQueueAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    Task RemoveQueueItemAsync(int id, bool removeFromClient = true, bool blocklist = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete movie from Radarr
    /// </summary>
    Task DeleteMovieAsync(int id, bool deleteFiles = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get root folders
    /// </summary>
    Task<List<RadarrRootFolder>> GetRootFoldersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get quality profiles
    /// </summary>
    Task<List<RadarrQualityProfile>> GetQualityProfilesAsync(CancellationToken cancellationToken = default);
}
