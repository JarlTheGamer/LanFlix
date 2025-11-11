using System.Net.Http.Json;
using System.Text.Json;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.ExternalApis;

public class SonarrClient : ISonarrClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SonarrClient> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly JsonSerializerOptions _jsonOptions;

    public SonarrClient(
        HttpClient httpClient,
        IServiceProvider serviceProvider,
        ILogger<SonarrClient> logger)
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
        return (settings.ExternalApis.Sonarr.Url, settings.ExternalApis.Sonarr.ApiKey);
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(
        HttpMethod method,
        string requestUri,
        CancellationToken cancellationToken = default)
    {
        var (url, apiKey) = await GetConfigAsync(cancellationToken);
        
        if (string.IsNullOrEmpty(url))
        {
            throw new InvalidOperationException("Sonarr URL is not configured. Please configure Sonarr in the admin settings.");
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
            _logger.LogError(ex, "Failed to connect to Sonarr");
            return false;
        }
    }

    public async Task<List<SonarrSearchResult>> SearchSeriesAsync(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await CreateRequestAsync(HttpMethod.Get, $"/api/v3/series/lookup?term={Uri.EscapeDataString(query)}", cancellationToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var results = await response.Content.ReadFromJsonAsync<List<SonarrSearchResult>>(_jsonOptions, cancellationToken);
            return results ?? new List<SonarrSearchResult>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search series in Sonarr: {Query}", query);
            throw;
        }
    }

    public async Task<SonarrSeries> AddSeriesAsync(AddSonarrSeriesRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get subtitle settings from server settings
            using var scope = _serviceProvider.CreateScope();
            var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();
            var settings = await settingsService.GetSettingsAsync(cancellationToken);
            
            var payload = new
            {
                tvdbId = request.TvdbId,
                title = request.Title,
                qualityProfileId = request.QualityProfileId,
                titleSlug = request.Title.ToLower().Replace(" ", "-"),
                images = new object[] { },
                seasons = new object[] { },
                path = $"{request.RootFolderPath}/{request.Title}",
                rootFolderPath = request.RootFolderPath,
                seasonFolder = true,
                monitored = request.Monitored,
                addOptions = new
                {
                    searchForMissingEpisodes = request.SearchForMissingEpisodes,
                    // Enable subtitle search if auto-download is enabled
                    searchForSubtitles = settings.ExternalApis.Subtitles.AutoDownload
                }
            };

            var httpRequest = await CreateRequestAsync(HttpMethod.Post, "/api/v3/series", cancellationToken);
            httpRequest.Content = JsonContent.Create(payload, options: _jsonOptions);
            
            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            var series = await response.Content.ReadFromJsonAsync<SonarrSeries>(_jsonOptions, cancellationToken);
            _logger.LogInformation("Series added to Sonarr: {Title} (Subtitle auto-download: {SubtitleEnabled})", 
                request.Title, settings.ExternalApis.Subtitles.AutoDownload);

            return series ?? throw new InvalidOperationException("Failed to deserialize Sonarr response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add series to Sonarr: {Title}", request.Title);
            throw;
        }
    }

    public async Task<List<SonarrSeries>> GetSeriesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await CreateRequestAsync(HttpMethod.Get, "/api/v3/series", cancellationToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var series = await response.Content.ReadFromJsonAsync<List<SonarrSeries>>(_jsonOptions, cancellationToken);
            return series ?? new List<SonarrSeries>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get series from Sonarr");
            throw;
        }
    }

    public async Task<SonarrSeries?> GetSeriesByTvdbIdAsync(int tvdbId, CancellationToken cancellationToken = default)
    {
        try
        {
            var series = await GetSeriesAsync(cancellationToken);
            return series.FirstOrDefault(s => s.TvdbId == tvdbId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get series by TVDB ID: {TvdbId}", tvdbId);
            throw;
        }
    }

    public async Task<SonarrQueueResponse> GetQueueAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await CreateRequestAsync(HttpMethod.Get, $"/api/v3/queue?page={page}&pageSize={pageSize}", cancellationToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var queueResponse = await response.Content.ReadFromJsonAsync<SonarrQueueResponse>(_jsonOptions, cancellationToken);
            return queueResponse ?? new SonarrQueueResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get queue from Sonarr");
            throw;
        }
    }

    public async Task DeleteSeriesAsync(int id, bool deleteFiles = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await CreateRequestAsync(HttpMethod.Delete, $"/api/v3/series/{id}?deleteFiles={deleteFiles}", cancellationToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            _logger.LogInformation("Series deleted from Sonarr: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete series from Sonarr: {Id}", id);
            throw;
        }
    }

    public async Task<List<SonarrRootFolder>> GetRootFoldersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await CreateRequestAsync(HttpMethod.Get, "/api/v3/rootfolder", cancellationToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var folders = await response.Content.ReadFromJsonAsync<List<SonarrRootFolder>>(_jsonOptions, cancellationToken);
            return folders ?? new List<SonarrRootFolder>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get root folders from Sonarr");
            throw;
        }
    }

    public async Task<List<SonarrQualityProfile>> GetQualityProfilesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await CreateRequestAsync(HttpMethod.Get, "/api/v3/qualityprofile", cancellationToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var profiles = await response.Content.ReadFromJsonAsync<List<SonarrQualityProfile>>(_jsonOptions, cancellationToken);
            return profiles ?? new List<SonarrQualityProfile>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get quality profiles from Sonarr");
            throw;
        }
    }

    public async Task<List<SonarrEpisode>> GetEpisodesAsync(int seriesId, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await CreateRequestAsync(HttpMethod.Get, $"/api/v3/episode?seriesId={seriesId}", cancellationToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var episodes = await response.Content.ReadFromJsonAsync<List<SonarrEpisode>>(_jsonOptions, cancellationToken);
            return episodes ?? new List<SonarrEpisode>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get episodes from Sonarr: {SeriesId}", seriesId);
            throw;
        }
    }

    public async Task SearchEpisodeAsync(int episodeId, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                name = "EpisodeSearch",
                episodeIds = new[] { episodeId }
            };

            var httpRequest = await CreateRequestAsync(HttpMethod.Post, "/api/v3/command", cancellationToken);
            httpRequest.Content = JsonContent.Create(payload, options: _jsonOptions);
            
            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Episode search triggered in Sonarr: {EpisodeId}", episodeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger episode search in Sonarr: {EpisodeId}", episodeId);
            throw;
        }
    }

    public async Task SearchSeasonAsync(int seriesId, int seasonNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                name = "SeasonSearch",
                seriesId,
                seasonNumber
            };

            var httpRequest = await CreateRequestAsync(HttpMethod.Post, "/api/v3/command", cancellationToken);
            httpRequest.Content = JsonContent.Create(payload, options: _jsonOptions);
            
            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Season search triggered in Sonarr: Series={SeriesId}, Season={SeasonNumber}", seriesId, seasonNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger season search in Sonarr: Series={SeriesId}, Season={SeasonNumber}", seriesId, seasonNumber);
            throw;
        }
    }
}
