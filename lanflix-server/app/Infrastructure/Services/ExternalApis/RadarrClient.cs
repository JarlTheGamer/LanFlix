using System.Net.Http.Json;
using System.Text.Json;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.ExternalApis;

public class RadarrClient : IRadarrClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RadarrClient> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly JsonSerializerOptions _jsonOptions;

    public RadarrClient(
        HttpClient httpClient,
        IServiceProvider serviceProvider,
        ILogger<RadarrClient> logger)
    {
        _httpClient = httpClient;
        _serviceProvider = serviceProvider;
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    private async Task<(string Url, string ApiKey)> GetConfigAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();
        var settings = await settingsService.GetSettingsAsync(cancellationToken);
        return (settings.ExternalApis.Radarr.Url, settings.ExternalApis.Radarr.ApiKey);
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(
        HttpMethod method,
        string requestUri,
        CancellationToken cancellationToken = default)
    {
        var (url, apiKey) = await GetConfigAsync(cancellationToken);
        
        if (string.IsNullOrEmpty(url))
        {
            throw new InvalidOperationException("Radarr URL is not configured. Please configure Radarr in the admin settings.");
        }

        // Ensure URL doesn't end with slash and requestUri starts with slash
        var baseUrl = url.TrimEnd('/');
        var path = requestUri.StartsWith('/') ? requestUri : '/' + requestUri;
        var fullUrl = baseUrl + path;
        
        var request = new HttpRequestMessage(method, fullUrl);
        
        if (!string.IsNullOrEmpty(apiKey))
        {
            request.Headers.Add("X-Api-Key", apiKey);
        }
        
        return request;
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await CreateRequestAsync(HttpMethod.Get, "/api/v3/system/status", cancellationToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);
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
            var request = await CreateRequestAsync(HttpMethod.Get, $"/api/v3/movie/lookup?term={Uri.EscapeDataString(query)}", cancellationToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var results = await response.Content.ReadFromJsonAsync<List<RadarrSearchResult>>(_jsonOptions, cancellationToken);
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
            // Get subtitle settings from server settings
            using var scope = _serviceProvider.CreateScope();
            var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();
            var settings = await settingsService.GetSettingsAsync(cancellationToken);
            
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
                    searchForMovie = request.SearchForMovie,
                    // Enable subtitle search if auto-download is enabled
                    searchForSubtitles = settings.ExternalApis.Subtitles.AutoDownload
                }
            };

            var httpRequest = await CreateRequestAsync(HttpMethod.Post, "/api/v3/movie", cancellationToken);
            httpRequest.Content = JsonContent.Create(payload, options: _jsonOptions);
            
            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            var movie = await response.Content.ReadFromJsonAsync<RadarrMovie>(_jsonOptions, cancellationToken);
            _logger.LogInformation("Movie added to Radarr: {Title} (Subtitle auto-download: {SubtitleEnabled})", 
                request.Title, settings.ExternalApis.Subtitles.AutoDownload);

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
            var request = await CreateRequestAsync(HttpMethod.Get, "/api/v3/movie", cancellationToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var movies = await response.Content.ReadFromJsonAsync<List<RadarrMovie>>(_jsonOptions, cancellationToken);
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
            var request = await CreateRequestAsync(HttpMethod.Get, $"/api/v3/queue?page={page}&pageSize={pageSize}", cancellationToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var queueResponse = await response.Content.ReadFromJsonAsync<RadarrQueueResponse>(_jsonOptions, cancellationToken);
            return queueResponse ?? new RadarrQueueResponse();
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
            var request = await CreateRequestAsync(HttpMethod.Delete, $"/api/v3/movie/{id}?deleteFiles={deleteFiles}", cancellationToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            _logger.LogInformation("Movie deleted from Radarr: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete movie from Radarr: {Id}", id);
            throw;
        }
    }

    public async Task RemoveQueueItemAsync(int id, bool removeFromClient = true, bool blocklist = false, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync(HttpMethod.Delete,
            $"/api/v3/queue/{id}?removeFromClient={removeFromClient.ToString().ToLowerInvariant()}&blocklist={blocklist.ToString().ToLowerInvariant()}", cancellationToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<RadarrRootFolder>> GetRootFoldersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await CreateRequestAsync(HttpMethod.Get, "/api/v3/rootfolder", cancellationToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var folders = await response.Content.ReadFromJsonAsync<List<RadarrRootFolder>>(_jsonOptions, cancellationToken);
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
            var request = await CreateRequestAsync(HttpMethod.Get, "/api/v3/qualityprofile", cancellationToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var profiles = await response.Content.ReadFromJsonAsync<List<RadarrQualityProfile>>(_jsonOptions, cancellationToken);
            return profiles ?? new List<RadarrQualityProfile>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get quality profiles from Radarr");
            throw;
        }
    }
}
