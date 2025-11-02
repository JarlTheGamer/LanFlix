using Lanflix.Application.Common.Models;

namespace Lanflix.Application.Common.Interfaces;

/// <summary>
/// Interface for Sonarr API client (TV series management)
/// </summary>
public interface ISonarrClient
{
    /// <summary>
    /// Test connection to Sonarr
    /// </summary>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Search for TV series by title
    /// </summary>
    Task<List<SonarrSearchResult>> SearchSeriesAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a series to Sonarr
    /// </summary>
    Task<SonarrSeries> AddSeriesAsync(AddSonarrSeriesRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all series
    /// </summary>
    Task<List<SonarrSeries>> GetSeriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get series by TVDB ID
    /// </summary>
    Task<SonarrSeries?> GetSeriesByTvdbIdAsync(int tvdbId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get download queue
    /// </summary>
    Task<SonarrQueueResponse> GetQueueAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete series from Sonarr
    /// </summary>
    Task DeleteSeriesAsync(int id, bool deleteFiles = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get root folders
    /// </summary>
    Task<List<SonarrRootFolder>> GetRootFoldersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get quality profiles
    /// </summary>
    Task<List<SonarrQualityProfile>> GetQualityProfilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get episodes for a series
    /// </summary>
    Task<List<SonarrEpisode>> GetEpisodesAsync(int seriesId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search for a specific episode
    /// </summary>
    Task SearchEpisodeAsync(int episodeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search for all episodes in a season
    /// </summary>
    Task SearchSeasonAsync(int seriesId, int seasonNumber, CancellationToken cancellationToken = default);
}
