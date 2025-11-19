using System.Net.Http.Json;
using System.Text.Json;
using Lanflix.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.ExternalApis;

public class BazarrClient : IBazarrClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BazarrClient> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly JsonSerializerOptions _jsonOptions;

    public BazarrClient(
        HttpClient httpClient,
        IServiceProvider serviceProvider,
        ILogger<BazarrClient> logger)
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
        return (settings.ExternalApis.Subtitles.Bazarr.Url, settings.ExternalApis.Subtitles.Bazarr.ApiKey);
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(
        HttpMethod method,
        string requestUri,
        CancellationToken cancellationToken = default)
    {
        var (url, apiKey) = await GetConfigAsync(cancellationToken);
        
        if (string.IsNullOrEmpty(url))
        {
            throw new InvalidOperationException("Bazarr URL is not configured. Please configure Bazarr in the admin settings.");
        }

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
            var request = await CreateRequestAsync(HttpMethod.Get, "/api/system/status", cancellationToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Bazarr");
            return false;
        }
    }

    public async Task SearchAndDownloadSubtitlesAsync(string path, string language, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Searching for subtitles via Bazarr: Path={Path}, Language={Language}", path, language);

            // Bazarr API endpoint to search and download subtitles for a specific file
            var payload = new
            {
                path,
                language,
                forced = false,
                hi = false
            };

            var httpRequest = await CreateRequestAsync(HttpMethod.Post, "/api/subtitles", cancellationToken);
            httpRequest.Content = JsonContent.Create(payload, options: _jsonOptions);
            
            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Subtitle search triggered for: {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search/download subtitles via Bazarr for: {Path}", path);
            throw;
        }
    }

    public async Task SearchAndDownloadMovieSubtitlesAsync(int radarrId, string language, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Searching for movie subtitles via Bazarr: RadarrId={RadarrId}, Language={Language}", radarrId, language);

            // Bazarr API endpoint to search and download subtitles for a Radarr movie
            var payload = new
            {
                radarrId,
                language,
                forced = false,
                hi = false
            };

            var httpRequest = await CreateRequestAsync(HttpMethod.Post, "/api/movies/subtitles", cancellationToken);
            httpRequest.Content = JsonContent.Create(payload, options: _jsonOptions);
            
            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Movie subtitle search triggered for Radarr ID: {RadarrId}", radarrId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search/download movie subtitles via Bazarr for Radarr ID: {RadarrId}", radarrId);
            throw;
        }
    }

    public async Task SearchAndDownloadSeriesSubtitlesAsync(int sonarrId, string language, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Searching for series subtitles via Bazarr: SonarrId={SonarrId}, Language={Language}", sonarrId, language);

            // Bazarr API endpoint to search and download subtitles for a Sonarr series
            var payload = new
            {
                sonarrId,
                language,
                forced = false,
                hi = false
            };

            var httpRequest = await CreateRequestAsync(HttpMethod.Post, "/api/series/subtitles", cancellationToken);
            httpRequest.Content = JsonContent.Create(payload, options: _jsonOptions);
            
            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Series subtitle search triggered for Sonarr ID: {SonarrId}", sonarrId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search/download series subtitles via Bazarr for Sonarr ID: {SonarrId}", sonarrId);
            throw;
        }
    }
}
