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
    private readonly string _apiKey;
    private readonly JsonSerializerOptions _jsonOptions;

    public TmdbClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<TmdbClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["Lanflix:ExternalApis:Tmdb:ApiKey"]
            ?? throw new InvalidOperationException("TMDB API key not configured");

        // Configure JSON serialization options
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<TmdbSearchResult> SearchMoviesAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Searching TMDB for movies: {Query}", query);

        var url = $"search/movie?api_key={_apiKey}&query={Uri.EscapeDataString(query)}";

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
        _logger.LogDebug("Searching TMDB for TV series: {Query}", query);

        var url = $"search/tv?api_key={_apiKey}&query={Uri.EscapeDataString(query)}";

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
        _logger.LogDebug("Getting TMDB movie details: {TmdbId}", tmdbId);

        var url = $"movie/{tmdbId}?api_key={_apiKey}";

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
        _logger.LogDebug("Getting TMDB TV series details: {TmdbId}", tmdbId);

        var url = $"tv/{tmdbId}?api_key={_apiKey}";

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
        _logger.LogDebug("Getting TMDB season details: Series={SeriesId}, Season={SeasonNumber}",
            seriesId, seasonNumber);

        var url = $"tv/{seriesId}/season/{seasonNumber}?api_key={_apiKey}";

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
}
