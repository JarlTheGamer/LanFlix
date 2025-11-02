using System.Text.Json;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Common.Models;
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

    /// <summary>
    /// Search for movie with different variations of the folder name
    /// Based on the old backend's search logic
    /// </summary>
    public async Task<TmdbSearchItem?> SearchMovieWithVariationsAsync(
        string folderName,
        CancellationToken cancellationToken = default)
    {
        var searchQueries = GenerateSearchVariations(folderName);
        
        foreach (var query in searchQueries)
        {
            try
            {
                _logger.LogDebug("Searching TMDB for movie: {Query}", query);
                var searchResults = await _tmdbClient.SearchMoviesAsync(query, cancellationToken);
                
                if (searchResults.Results.Any())
                {
                    _logger.LogInformation("Found TMDB match for movie: {Query} -> {Title}", query, searchResults.Results.First().Title);
                    return searchResults.Results.First();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Search failed for query: {Query}", query);
            }
        }
        
        _logger.LogWarning("No TMDB match found for movie folder: {FolderName} (tried {Count} variations)", folderName, searchQueries.Count);
        return null;
    }

    /// <summary>
    /// Search for TV series with different variations of the folder name
    /// Based on the old backend's search logic
    /// </summary>
    public async Task<TmdbSearchItem?> SearchSeriesWithVariationsAsync(
        string folderName,
        CancellationToken cancellationToken = default)
    {
        var searchQueries = GenerateSearchVariations(folderName);
        
        foreach (var query in searchQueries)
        {
            try
            {
                _logger.LogDebug("Searching TMDB for series: {Query}", query);
                var searchResults = await _tmdbClient.SearchTvSeriesAsync(query, cancellationToken);
                
                if (searchResults.Results.Any())
                {
                    _logger.LogInformation("Found TMDB match for series: {Query} -> {Title}", query, searchResults.Results.First().Name);
                    return searchResults.Results.First();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Search failed for query: {Query}", query);
            }
        }
        
        _logger.LogWarning("No TMDB match found for series folder: {FolderName} (tried {Count} variations)", folderName, searchQueries.Count);
        return null;
    }

    /// <summary>
    /// Generate different search variations for a folder name
    /// Based on the old backend's search logic
    /// </summary>
    private List<string> GenerateSearchVariations(string folderName)
    {
        var variations = new List<string>();
        
        // Original name
        variations.Add(folderName);
        
        // Remove year in parentheses (e.g., "Movie (2023)" -> "Movie")
        var withoutYear = System.Text.RegularExpressions.Regex.Replace(folderName, @"\s*\(\d{4}\)\s*", "").Trim();
        if (withoutYear != folderName)
        {
            variations.Add(withoutYear);
        }
        
        // Remove common suffixes
        var commonSuffixes = new[] { "REMASTERED", "EXTENDED", "DIRECTOR'S CUT", "UNCUT", "4K", "HDR", "REMUX" };
        foreach (var suffix in commonSuffixes)
        {
            var withoutSuffix = System.Text.RegularExpressions.Regex.Replace(folderName, $@"\s*{suffix}\s*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
            if (withoutSuffix != folderName && !variations.Contains(withoutSuffix))
            {
                variations.Add(withoutSuffix);
            }
        }
        
        // Replace dots and underscores with spaces
        var withSpaces = folderName.Replace(".", " ").Replace("_", " ");
        if (withSpaces != folderName && !variations.Contains(withSpaces))
        {
            variations.Add(withSpaces);
        }
        
        // Try without "The" prefix
        if (folderName.StartsWith("The ", StringComparison.OrdinalIgnoreCase))
        {
            var withoutThe = folderName.Substring(4).Trim();
            if (!variations.Contains(withoutThe))
            {
                variations.Add(withoutThe);
            }
        }
        
        // Try with "The" prefix if it doesn't have it
        if (!folderName.StartsWith("The ", StringComparison.OrdinalIgnoreCase))
        {
            var withThe = $"The {folderName}";
            if (!variations.Contains(withThe))
            {
                variations.Add(withThe);
            }
        }
        
        // Remove common release group tags in brackets
        var withoutBrackets = System.Text.RegularExpressions.Regex.Replace(folderName, @"\[.*?\]", "").Trim();
        if (withoutBrackets != folderName && !variations.Contains(withoutBrackets))
        {
            variations.Add(withoutBrackets);
        }
        
        // Remove extra spaces and normalize
        for (int i = 0; i < variations.Count; i++)
        {
            variations[i] = System.Text.RegularExpressions.Regex.Replace(variations[i], @"\s+", " ").Trim();
        }
        
        // Remove duplicates and empty strings
        var finalVariations = variations.Where(v => !string.IsNullOrWhiteSpace(v)).Distinct().ToList();
        
        _logger.LogDebug("Generated search variations for '{FolderName}': {Variations}", folderName, string.Join(", ", finalVariations));
        return finalVariations;
    }
}
