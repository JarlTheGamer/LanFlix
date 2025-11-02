using System.Net.Http.Json;
using System.Text.Json;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.ExternalApis;

public class RadarrClient : IRadarrClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RadarrClient> _logger;
    private readonly string _apiKey;
    private readonly JsonSerializerOptions _jsonOptions;

    public RadarrClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<RadarrClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["Lanflix:ExternalApis:Radarr:ApiKey"] ?? string.Empty;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _apiKey);
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/v3/system/status", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Radarr");
            return false;
        }
    }

    public async Task<List<RadarrSearchResult>> SearchMoviesAsync(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            var results = await _httpClient.GetFromJsonAsync<List<RadarrSearchResult>>(
                $"/api/v3/movie/lookup?term={Uri.EscapeDataString(query)}",
                _jsonOptions,
                cancellationToken);

            return results ?? new List<RadarrSearchResult>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search movies in Radarr: {Query}", query);
            throw;
        }
    }

    public async Task<RadarrMovie> AddMovieAsync(AddRadarrMovieRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                tmdbId = request.TmdbId,
                title = request.Title,
                year = request.Year,
                qualityProfileId = request.QualityProfileId,
                titleSlug = $"{request.Title.ToLower().Replace(" ", "-")}-{request.TmdbId}",
                images = new object[] { },
                path = $"{request.RootFolderPath}/{request.Title} ({request.Year})",
                rootFolderPath = request.RootFolderPath,
                monitored = request.Monitored,
                minimumAvailability = "released",
                addOptions = new
                {
                    searchForMovie = request.SearchForMovie
                }
            };

            var response = await _httpClient.PostAsJsonAsync("/api/v3/movie", payload, _jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var movie = await response.Content.ReadFromJsonAsync<RadarrMovie>(_jsonOptions, cancellationToken);
            _logger.LogInformation("Movie added to Radarr: {Title}", request.Title);

            return movie ?? throw new InvalidOperationException("Failed to deserialize Radarr response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add movie to Radarr: {Title}", request.Title);
            throw;
        }
    }

    public async Task<List<RadarrMovie>> GetMoviesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var movies = await _httpClient.GetFromJsonAsync<List<RadarrMovie>>(
                "/api/v3/movie",
                _jsonOptions,
                cancellationToken);

            return movies ?? new List<RadarrMovie>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get movies from Radarr");
            throw;
        }
    }

    public async Task<RadarrMovie?> GetMovieByTmdbIdAsync(int tmdbId, CancellationToken cancellationToken = default)
    {
        try
        {
            var movies = await GetMoviesAsync(cancellationToken);
            return movies.FirstOrDefault(m => m.TmdbId == tmdbId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get movie by TMDB ID: {TmdbId}", tmdbId);
            throw;
        }
    }

    public async Task<RadarrQueueResponse> GetQueueAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<RadarrQueueResponse>(
                $"/api/v3/queue?page={page}&pageSize={pageSize}",
                _jsonOptions,
                cancellationToken);

            return response ?? new RadarrQueueResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get queue from Radarr");
            throw;
        }
    }

    public async Task DeleteMovieAsync(int id, bool deleteFiles = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(
                $"/api/v3/movie/{id}?deleteFiles={deleteFiles}",
                cancellationToken);

            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Movie deleted from Radarr: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete movie from Radarr: {Id}", id);
            throw;
        }
    }

    public async Task<List<RadarrRootFolder>> GetRootFoldersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var folders = await _httpClient.GetFromJsonAsync<List<RadarrRootFolder>>(
                "/api/v3/rootfolder",
                _jsonOptions,
                cancellationToken);

            return folders ?? new List<RadarrRootFolder>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get root folders from Radarr");
            throw;
        }
    }

    public async Task<List<RadarrQualityProfile>> GetQualityProfilesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var profiles = await _httpClient.GetFromJsonAsync<List<RadarrQualityProfile>>(
                "/api/v3/qualityprofile",
                _jsonOptions,
                cancellationToken);

            return profiles ?? new List<RadarrQualityProfile>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get quality profiles from Radarr");
            throw;
        }
    }
}
