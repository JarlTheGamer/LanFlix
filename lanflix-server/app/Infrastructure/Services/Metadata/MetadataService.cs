using System.Text.Json;
using Lanflix.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.Metadata;

/// <summary>
/// Service for managing content metadata from TMDB
/// Handles fetching, downloading, and storing metadata, posters, and backdrops in media folders
/// Based on the old backend metadata.service.ts
/// </summary>
public class MetadataService : IMetadataService
{
    private readonly ITmdbClient _tmdbClient;
    private readonly ILogger<MetadataService> _logger;
    private readonly HttpClient _httpClient;
    private const string ImageBaseUrl = "https://image.tmdb.org/t/p";
    private const string PosterSize = "w500";
    private const string BackdropSize = "w1280";
    private const string StillSize = "w300";

    public MetadataService(
        ITmdbClient tmdbClient,
        ILogger<MetadataService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _tmdbClient = tmdbClient;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    /// <summary>
    /// Save metadata (poster, backdrop, metadata.json) to media folder
    /// Matches the old backend's saveMetadataToMediaFolder function
    /// </summary>
    public async Task SaveMetadataToMediaFolderAsync(
        int tmdbId,
        string type,
        string mediaFolderPath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Saving metadata to media folder: {Path}", mediaFolderPath);

            // Ensure directory exists
            Directory.CreateDirectory(mediaFolderPath);

            // Fetch metadata from TMDB
            object metadata;
            string? posterPath = null;
            string? backdropPath = null;

            if (type == "movie")
            {
                var movieDetails = await _tmdbClient.GetMovieDetailsAsync(tmdbId, cancellationToken);
                posterPath = movieDetails.PosterPath;
                backdropPath = movieDetails.BackdropPath;

                metadata = new
                {
                    tmdbId = movieDetails.Id,
                    title = movieDetails.Title,
                    originalTitle = movieDetails.OriginalTitle,
                    overview = movieDetails.Overview,
                    releaseDate = movieDetails.ReleaseDate,
                    runtime = movieDetails.Runtime,
                    voteAverage = movieDetails.VoteAverage,
                    genres = movieDetails.Genres.Select(g => g.Name).ToList(),
                    posterPath = movieDetails.PosterPath,
                    backdropPath = movieDetails.BackdropPath,
                    tagline = movieDetails.Tagline,
                    imdbId = movieDetails.ImdbId,
                    fetchedAt = DateTime.UtcNow.ToString("o")
                };
            }
            else // series
            {
                var seriesDetails = await _tmdbClient.GetTvSeriesDetailsAsync(tmdbId, cancellationToken);
                posterPath = seriesDetails.PosterPath;
                backdropPath = seriesDetails.BackdropPath;

                metadata = new
                {
                    tmdbId = seriesDetails.Id,
                    title = seriesDetails.Name,
                    originalTitle = seriesDetails.OriginalName,
                    overview = seriesDetails.Overview,
                    firstAirDate = seriesDetails.FirstAirDate,
                    lastAirDate = seriesDetails.LastAirDate,
                    numberOfSeasons = seriesDetails.NumberOfSeasons,
                    numberOfEpisodes = seriesDetails.NumberOfEpisodes,
                    genres = seriesDetails.Genres.Select(g => g.Name).ToList(),
                    voteAverage = seriesDetails.VoteAverage,
                    posterPath = seriesDetails.PosterPath,
                    backdropPath = seriesDetails.BackdropPath,
                    seasons = seriesDetails.Seasons
                        .Where(s => s.SeasonNumber > 0) // Exclude specials
                        .Select(s => new
                        {
                            seasonNumber = s.SeasonNumber,
                            episodeCount = s.EpisodeCount,
                            airDate = s.AirDate
                        }).ToList(),
                    fetchedAt = DateTime.UtcNow.ToString("o")
                };
            }

            // Save metadata.json
            var metadataPath = Path.Combine(mediaFolderPath, "metadata.json");
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var json = JsonSerializer.Serialize(metadata, jsonOptions);
            await File.WriteAllTextAsync(metadataPath, json, cancellationToken);
            _logger.LogInformation("Saved metadata to {Path}", metadataPath);

            // Download and save poster if available
            if (!string.IsNullOrEmpty(posterPath))
            {
                await DownloadPosterAsync(posterPath, mediaFolderPath, cancellationToken);
            }

            // Download and save backdrop if available
            if (!string.IsNullOrEmpty(backdropPath))
            {
                await DownloadBackdropAsync(backdropPath, mediaFolderPath, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save metadata to media folder: {Path}", mediaFolderPath);
            throw;
        }
    }

    /// <summary>
    /// Download poster image to media folder as poster.jpg
    /// Matches the old backend's downloadPosterImage function
    /// </summary>
    public async Task<string?> DownloadPosterAsync(
        string posterPath,
        string mediaFolderPath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(posterPath))
            {
                return null;
            }

            var imageUrl = $"{ImageBaseUrl}/{PosterSize}{posterPath}";
            var localPath = Path.Combine(mediaFolderPath, "poster.jpg");

            // Check if already exists
            if (File.Exists(localPath))
            {
                _logger.LogDebug("Poster already exists: {Path}", localPath);
                return localPath;
            }

            // Ensure directory exists
            Directory.CreateDirectory(mediaFolderPath);

            // Download image
            var response = await _httpClient.GetAsync(imageUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var imageBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            await File.WriteAllBytesAsync(localPath, imageBytes, cancellationToken);

            _logger.LogInformation("Downloaded poster: {Path}", localPath);
            return localPath;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download poster from {PosterPath}", posterPath);
            return null;
        }
    }

    /// <summary>
    /// Download backdrop image to media folder as backdrop.jpg
    /// Matches the old backend's downloadBackdropImage function
    /// </summary>
    public async Task<string?> DownloadBackdropAsync(
        string backdropPath,
        string mediaFolderPath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(backdropPath))
            {
                return null;
            }

            var imageUrl = $"{ImageBaseUrl}/{BackdropSize}{backdropPath}";
            var localPath = Path.Combine(mediaFolderPath, "backdrop.jpg");

            // Check if already exists
            if (File.Exists(localPath))
            {
                _logger.LogDebug("Backdrop already exists: {Path}", localPath);
                return localPath;
            }

            // Ensure directory exists
            Directory.CreateDirectory(mediaFolderPath);

            // Download image
            var response = await _httpClient.GetAsync(imageUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var imageBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            await File.WriteAllBytesAsync(localPath, imageBytes, cancellationToken);

            _logger.LogInformation("Downloaded backdrop: {Path}", localPath);
            return localPath;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download backdrop from {BackdropPath}", backdropPath);
            return null;
        }
    }

    /// <summary>
    /// Download episode still image to season folder as S01E01.jpg
    /// Matches the old backend's downloadEpisodeStill function
    /// </summary>
    public async Task<string?> DownloadEpisodeStillAsync(
        string stillPath,
        string seasonFolderPath,
        int seasonNumber,
        int episodeNumber,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(stillPath))
            {
                return null;
            }

            // Create filename: S01E01.jpg
            var filename = $"S{seasonNumber:D2}E{episodeNumber:D2}.jpg";
            var localPath = Path.Combine(seasonFolderPath, filename);

            // Check if already exists
            if (File.Exists(localPath))
            {
                _logger.LogDebug("Episode still already exists: {Path}", localPath);
                return filename;
            }

            // Ensure directory exists
            Directory.CreateDirectory(seasonFolderPath);

            // Download from TMDB
            var stillUrl = $"{ImageBaseUrl}/{StillSize}{stillPath}";
            var response = await _httpClient.GetAsync(stillUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var imageBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            await File.WriteAllBytesAsync(localPath, imageBytes, cancellationToken);

            _logger.LogInformation("Downloaded episode still: {Path}", localPath);
            return filename;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download episode still for S{Season}E{Episode}", seasonNumber, episodeNumber);
            return null;
        }
    }

    /// <summary>
    /// Load metadata from media folder metadata.json file
    /// Matches the old backend's loadMetadataFromMediaFolder function
    /// </summary>
    public async Task<object?> LoadMetadataFromMediaFolderAsync(
        string mediaFolderPath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadataPath = Path.Combine(mediaFolderPath, "metadata.json");

            if (!File.Exists(metadataPath))
            {
                _logger.LogDebug("No metadata file found at {Path}", metadataPath);
                return null;
            }

            var json = await File.ReadAllTextAsync(metadataPath, cancellationToken);
            var metadata = JsonSerializer.Deserialize<object>(json);

            _logger.LogInformation("Loaded metadata from {Path}", metadataPath);
            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load metadata from {Path}", mediaFolderPath);
            throw;
        }
    }
}
