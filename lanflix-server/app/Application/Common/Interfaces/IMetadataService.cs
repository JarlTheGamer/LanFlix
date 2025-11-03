using Lanflix.Application.Common.Models;

namespace Lanflix.Application.Common.Interfaces;

public interface IMetadataService
{
    /// <summary>
    /// Save metadata to media folder as JSON file
    /// </summary>
    Task SaveMetadataToMediaFolderAsync(
        int contentId,
        string mediaFolderPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Download poster image to media folder
    /// </summary>
    Task<string?> DownloadPosterAsync(
        string posterPath,
        string mediaFolderPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Download backdrop image to media folder
    /// </summary>
    Task<string?> DownloadBackdropAsync(
        string backdropPath,
        string mediaFolderPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Download episode still image to season folder
    /// </summary>
    Task<string?> DownloadEpisodeStillAsync(
        string stillPath,
        string seasonFolderPath,
        int seasonNumber,
        int episodeNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Load metadata from media folder metadata.json file
    /// </summary>
    Task<object?> LoadMetadataFromMediaFolderAsync(
        string mediaFolderPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search for movie with different variations of the folder name
    /// </summary>
    Task<TmdbSearchItem?> SearchMovieWithVariationsAsync(
        string folderName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search for TV series with different variations of the folder name
    /// </summary>
    Task<TmdbSearchItem?> SearchSeriesWithVariationsAsync(
        string folderName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch season details with episodes from TMDB
    /// </summary>
    Task<TmdbSeasonDetails?> FetchSeasonDetailsAsync(
        int tmdbId,
        int seasonNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch and store episode metadata for a series
    /// </summary>
    Task FetchAndStoreEpisodeMetadataAsync(
        int contentId,
        int tmdbId,
        string seriesFolder,
        CancellationToken cancellationToken = default);
}
