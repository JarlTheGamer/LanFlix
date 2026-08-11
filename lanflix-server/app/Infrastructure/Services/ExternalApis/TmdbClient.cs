using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.ExternalApis;

/// <summary>
/// TMDB API client with HTTP client pooling and connection management
/// </summary>
public class TmdbClient : ITmdbClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TmdbClient> _logger;
    private readonly ISettingsService _settingsService;
    private readonly JsonSerializerOptions _jsonOptions;

    public TmdbClient(
        HttpClient httpClient,
        ISettingsService settingsService,
        ILogger<TmdbClient> logger)
    {
        _httpClient = httpClient;
        _settingsService = settingsService;
        _logger = logger;

        // Configure JSON serialization options
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new NullableDateTimeConverter() }
        };
    }

    private async Task<string> GetApiKeyAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.GetSettingsAsync(cancellationToken);
        return settings.ExternalApis.Tmdb.ApiKey;
    }

    public async Task<string?> GetLogoPathAsync(
        int tmdbId,
        bool isSeries,
        string language = "en",
        CancellationToken cancellationToken = default)
    {
        var apiKey = await GetApiKeyAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey)) return null;

        var mediaType = isSeries ? "tv" : "movie";
        var url = $"{mediaType}/{tmdbId}/images?api_key={apiKey}&include_image_language={Uri.EscapeDataString(language)},null";
        try
        {
            var result = await _httpClient.GetFromJsonAsync<TmdbImagesResponse>(url, _jsonOptions, cancellationToken);
            return result?.Logos
                .OrderByDescending(logo => string.Equals(logo.Language, language, StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(logo => logo.VoteAverage)
                .ThenByDescending(logo => logo.Width)
                .Select(logo => logo.FilePath)
                .FirstOrDefault(path => !string.IsNullOrWhiteSpace(path));
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            _logger.LogWarning(ex, "Unable to retrieve TMDB logo artwork for {MediaType} {TmdbId}", mediaType, tmdbId);
            return null;
        }
    }

    public async Task<TmdbSearchResult> SearchMoviesAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        var apiKey = await GetApiKeyAsync(cancellationToken);
        
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("TMDB API key not configured. Cannot search movies.");
            return new TmdbSearchResult();
        }

        _logger.LogDebug("Searching TMDB for movies: {Query}", query);

        var url = $"search/movie?api_key={apiKey}&query={Uri.EscapeDataString(query)}";

        try
        {
            var result = await _httpClient.GetFromJsonAsync<TmdbSearchResult>(
                url,
                _jsonOptions,
                cancellationToken);

            _logger.LogInformation(
                "TMDB movie search completed: {Query}, Results: {Count}",
                query,
                result?.Results.Count ?? 0);

            return result ?? new TmdbSearchResult();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error searching TMDB for movies: {Query}", query);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error for TMDB movie search: {Query}", query);
            throw;
        }
    }

    public async Task<TmdbSearchResult> SearchTvSeriesAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        var apiKey = await GetApiKeyAsync(cancellationToken);
        
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("TMDB API key not configured. Cannot search TV series.");
            return new TmdbSearchResult();
        }

        _logger.LogDebug("Searching TMDB for TV series: {Query}", query);

        var url = $"search/tv?api_key={apiKey}&query={Uri.EscapeDataString(query)}";

        try
        {
            var result = await _httpClient.GetFromJsonAsync<TmdbSearchResult>(
                url,
                _jsonOptions,
                cancellationToken);

            _logger.LogInformation(
                "TMDB TV series search completed: {Query}, Results: {Count}",
                query,
                result?.Results.Count ?? 0);

            return result ?? new TmdbSearchResult();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error searching TMDB for TV series: {Query}", query);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error for TMDB TV series search: {Query}", query);
            throw;
        }
    }

    public async Task<TmdbMovieDetails?> GetMovieDetailsAsync(
        int tmdbId,
        CancellationToken cancellationToken = default)
    {
        var apiKey = await GetApiKeyAsync(cancellationToken);
        
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("TMDB API key not configured. Cannot get movie details.");
            return null;
        }

        _logger.LogDebug("Getting TMDB movie details: {TmdbId}", tmdbId);

        var url = $"movie/{tmdbId}?api_key={apiKey}";

        try
        {
            var result = await _httpClient.GetFromJsonAsync<TmdbMovieDetails>(
                url,
                _jsonOptions,
                cancellationToken);

            _logger.LogInformation("TMDB movie details retrieved: {TmdbId}, Title: {Title}",
                tmdbId, result?.Title);

            return result;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("TMDB movie not found: {TmdbId}", tmdbId);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error getting TMDB movie details: {TmdbId}", tmdbId);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error for TMDB movie details: {TmdbId}", tmdbId);
            throw;
        }
    }

    public async Task<TmdbTvSeriesDetails?> GetTvSeriesDetailsAsync(
        int tmdbId,
        CancellationToken cancellationToken = default)
    {
        var apiKey = await GetApiKeyAsync(cancellationToken);
        
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("TMDB API key not configured. Cannot get TV series details.");
            return null;
        }

        _logger.LogDebug("Getting TMDB TV series details: {TmdbId}", tmdbId);

        var url = $"tv/{tmdbId}?api_key={apiKey}";

        try
        {
            var result = await _httpClient.GetFromJsonAsync<TmdbTvSeriesDetails>(
                url,
                _jsonOptions,
                cancellationToken);

            _logger.LogInformation("TMDB TV series details retrieved: {TmdbId}, Name: {Name}",
                tmdbId, result?.Name);

            return result;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("TMDB TV series not found: {TmdbId}", tmdbId);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error getting TMDB TV series details: {TmdbId}", tmdbId);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error for TMDB TV series details: {TmdbId}", tmdbId);
            throw;
        }
    }

    public async Task<TmdbSeasonDetails?> GetSeasonDetailsAsync(
        int seriesId,
        int seasonNumber,
        CancellationToken cancellationToken = default)
    {
        var apiKey = await GetApiKeyAsync(cancellationToken);
        
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("TMDB API key not configured. Cannot get season details.");
            return null;
        }

        _logger.LogDebug("Getting TMDB season details: Series={SeriesId}, Season={SeasonNumber}",
            seriesId, seasonNumber);

        var url = $"tv/{seriesId}/season/{seasonNumber}?api_key={apiKey}";

        try
        {
            var result = await _httpClient.GetFromJsonAsync<TmdbSeasonDetails>(
                url,
                _jsonOptions,
                cancellationToken);

            _logger.LogInformation(
                "TMDB season details retrieved: Series={SeriesId}, Season={SeasonNumber}, Episodes={EpisodeCount}",
                seriesId, seasonNumber, result?.Episodes.Count ?? 0);

            return result;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("TMDB season not found: Series={SeriesId}, Season={SeasonNumber}",
                seriesId, seasonNumber);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error getting TMDB season details: Series={SeriesId}, Season={SeasonNumber}",
                seriesId, seasonNumber);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error for TMDB season details: Series={SeriesId}, Season={SeasonNumber}",
                seriesId, seasonNumber);
            throw;
        }
    }

    public async Task<TmdbSearchResult> GetTrendingAsync(
        string mediaType = "all",
        string timeWindow = "week",
        CancellationToken cancellationToken = default)
    {
        var apiKey = await GetApiKeyAsync(cancellationToken);
        
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("TMDB API key not configured. Cannot get trending content.");
            return new TmdbSearchResult();
        }

        _logger.LogDebug("Getting TMDB trending content: MediaType={MediaType}, TimeWindow={TimeWindow}",
            mediaType, timeWindow);

        var url = $"trending/{mediaType}/{timeWindow}?api_key={apiKey}";

        try
        {
            var result = await _httpClient.GetFromJsonAsync<TmdbSearchResult>(
                url,
                _jsonOptions,
                cancellationToken);

            _logger.LogInformation(
                "TMDB trending content retrieved: MediaType={MediaType}, Results={Count}",
                mediaType, result?.Results.Count ?? 0);

            return result ?? new TmdbSearchResult();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error getting TMDB trending content: MediaType={MediaType}", mediaType);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error for TMDB trending content: MediaType={MediaType}", mediaType);
            throw;
        }
    }

    public async Task<TmdbSearchResult> GetPopularMoviesAsync(
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var apiKey = await GetApiKeyAsync(cancellationToken);
        
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("TMDB API key not configured. Cannot get popular movies.");
            return new TmdbSearchResult();
        }

        _logger.LogDebug("Getting TMDB popular movies: Page={Page}", page);

        var url = $"movie/popular?api_key={apiKey}&page={page}";

        try
        {
            var result = await _httpClient.GetFromJsonAsync<TmdbSearchResult>(
                url,
                _jsonOptions,
                cancellationToken);

            _logger.LogInformation(
                "TMDB popular movies retrieved: Page={Page}, Results={Count}",
                page, result?.Results.Count ?? 0);

            return result ?? new TmdbSearchResult();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error getting TMDB popular movies: Page={Page}", page);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error for TMDB popular movies: Page={Page}", page);
            throw;
        }
    }

    public async Task<TmdbSearchResult> GetPopularTvSeriesAsync(
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var apiKey = await GetApiKeyAsync(cancellationToken);
        
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("TMDB API key not configured. Cannot get popular TV series.");
            return new TmdbSearchResult();
        }

        _logger.LogDebug("Getting TMDB popular TV series: Page={Page}", page);

        var url = $"tv/popular?api_key={apiKey}&page={page}";

        try
        {
            var result = await _httpClient.GetFromJsonAsync<TmdbSearchResult>(
                url,
                _jsonOptions,
                cancellationToken);

            _logger.LogInformation(
                "TMDB popular TV series retrieved: Page={Page}, Results={Count}",
                page, result?.Results.Count ?? 0);

            return result ?? new TmdbSearchResult();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error getting TMDB popular TV series: Page={Page}", page);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error for TMDB popular TV series: Page={Page}", page);
            throw;
        }
    }

    public async Task<TmdbCreditsResult?> GetMovieCreditsAsync(int tmdbId, CancellationToken cancellationToken = default)
    {
        var apiKey = await GetApiKeyAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey)) return null;
        var url = $"movie/{tmdbId}/credits?api_key={apiKey}";
        try { return await _httpClient.GetFromJsonAsync<TmdbCreditsResult>(url, _jsonOptions, cancellationToken); }
        catch (Exception ex) { _logger.LogWarning(ex, "Error getting movie credits for TMDB ID {TmdbId}", tmdbId); return null; }
    }

    public async Task<TmdbCreditsResult?> GetTvCreditsAsync(int tmdbId, CancellationToken cancellationToken = default)
    {
        var apiKey = await GetApiKeyAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey)) return null;
        var url = $"tv/{tmdbId}/credits?api_key={apiKey}";
        try { return await _httpClient.GetFromJsonAsync<TmdbCreditsResult>(url, _jsonOptions, cancellationToken); }
        catch (Exception ex) { _logger.LogWarning(ex, "Error getting TV credits for TMDB ID {TmdbId}", tmdbId); return null; }
    }
}

internal sealed class TmdbImagesResponse
{
    [JsonPropertyName("logos")]
    public List<TmdbLogoImage> Logos { get; init; } = [];
}

internal sealed class TmdbLogoImage
{
    [JsonPropertyName("file_path")]
    public string FilePath { get; init; } = string.Empty;

    [JsonPropertyName("iso_639_1")]
    public string? Language { get; init; }

    [JsonPropertyName("vote_average")]
    public double VoteAverage { get; init; }

    [JsonPropertyName("width")]
    public int Width { get; init; }
}

/// <summary>
/// Custom JSON converter for nullable DateTime that handles empty strings from TMDB API
/// </summary>
public class NullableDateTimeConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            
            // Handle empty strings or whitespace as null
            if (string.IsNullOrWhiteSpace(stringValue))
            {
                return null;
            }

            // Try to parse the date
            if (DateTime.TryParse(stringValue, out var dateValue))
            {
                return dateValue;
            }

            // If parsing fails, return null instead of throwing
            return null;
        }

        // For other token types, try the default behavior
        return JsonSerializer.Deserialize<DateTime?>(ref reader, options);
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd"));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
