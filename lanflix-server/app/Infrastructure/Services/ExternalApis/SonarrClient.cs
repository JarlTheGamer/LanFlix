using System.Net.Http.Json;
using System.Text.Json;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.ExternalApis;

public class SonarrClient : ISonarrClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SonarrClient> _logger;
    private readonly string _apiKey;
    private readonly JsonSerializerOptions _jsonOptions;

    public SonarrClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<SonarrClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["Lanflix:ExternalApis:Sonarr:ApiKey"] ?? string.Empty;

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
            _logger.LogError(ex, "Failed to connect to Sonarr");
            return false;
        }
    }

    public async Task<List<SonarrSearchResult>> SearchSeriesAsync(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            var results = await _httpClient.GetFromJsonAsync<List<SonarrSearchResult>>(
                $"/api/v3/series/lookup?term={Uri.EscapeDataString(query)}",
                _jsonOptions,
                cancellationToken);

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
                    searchForMissingEpisodes = request.SearchForMissingEpisodes
                }
            };

            var response = await _httpClient.PostAsJsonAsync("/api/v3/series", payload, _jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var series = await response.Content.ReadFromJsonAsync<SonarrSeries>(_jsonOptions, cancellationToken);
            _logger.LogInformation("Series added to Sonarr: {Title}", request.Title);

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
            var series = await _httpClient.GetFromJsonAsync<List<SonarrSeries>>(
                "/api/v3/series",
                _jsonOptions,
                cancellationToken);

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
            var response = await _httpClient.GetFromJsonAsync<SonarrQueueResponse>(
                $"/api/v3/queue?page={page}&pageSize={pageSize}",
                _jsonOptions,
                cancellationToken);

            return response ?? new SonarrQueueResponse();
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
            var response = await _httpClient.DeleteAsync(
                $"/api/v3/series/{id}?deleteFiles={deleteFiles}",
                cancellationToken);

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
            var folders = await _httpClient.GetFromJsonAsync<List<SonarrRootFolder>>(
                "/api/v3/rootfolder",
                _jsonOptions,
                cancellationToken);

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
            var profiles = await _httpClient.GetFromJsonAsync<List<SonarrQualityProfile>>(
                "/api/v3/qualityprofile",
                _jsonOptions,
                cancellationToken);

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
            var episodes = await _httpClient.GetFromJsonAsync<List<SonarrEpisode>>(
                $"/api/v3/episode?seriesId={seriesId}",
                _jsonOptions,
                cancellationToken);

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

            var response = await _httpClient.PostAsJsonAsync("/api/v3/command", payload, _jsonOptions, cancellationToken);
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

            var response = await _httpClient.PostAsJsonAsync("/api/v3/command", payload, _jsonOptions, cancellationToken);
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
