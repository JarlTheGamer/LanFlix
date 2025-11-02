using System.Net.Http.Json;
using System.Text.Json;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.ExternalApis;

public class ProwlarrClient : IProwlarrClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProwlarrClient> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly JsonSerializerOptions _jsonOptions;

    public ProwlarrClient(
        HttpClient httpClient,
        IServiceProvider serviceProvider,
        ILogger<ProwlarrClient> logger)
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
        return (settings.ExternalApis.Prowlarr.Url, settings.ExternalApis.Prowlarr.ApiKey);
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(
        HttpMethod method,
        string requestUri,
        CancellationToken cancellationToken = default)
    {
        var (url, apiKey) = await GetConfigAsync(cancellationToken);
        
        if (string.IsNullOrEmpty(url))
        {
            throw new InvalidOperationException("Prowlarr URL is not configured. Please configure Prowlarr in the admin settings.");
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
            var request = await CreateRequestAsync(HttpMethod.Get, "/api/v1/system/status", cancellationToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Prowlarr");
            return false;
        }
    }

    public async Task<List<ProwlarrSearchResult>> SearchAsync(
        string query,
        string? type = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"/api/v1/search?query={Uri.EscapeDataString(query)}&limit={limit}";

            if (!string.IsNullOrEmpty(type))
            {
                // Map type to Prowlarr categories
                if (type.Equals("movie", StringComparison.OrdinalIgnoreCase))
                {
                    url += "&categories=2000"; // Movies category
                }
                else if (type.Equals("tv", StringComparison.OrdinalIgnoreCase))
                {
                    url += "&categories=5000"; // TV category
                }
            }

            var request = await CreateRequestAsync(HttpMethod.Get, url, cancellationToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var results = await response.Content.ReadFromJsonAsync<List<ProwlarrSearchResult>>(_jsonOptions, cancellationToken);

            _logger.LogInformation("Prowlarr search completed: {Query}, Results: {Count}", query, results?.Count ?? 0);

            return results ?? new List<ProwlarrSearchResult>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search in Prowlarr: {Query}", query);
            throw;
        }
    }

    public async Task<List<ProwlarrIndexer>> GetIndexersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await CreateRequestAsync(HttpMethod.Get, "/api/v1/indexer", cancellationToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var indexers = await response.Content.ReadFromJsonAsync<List<ProwlarrIndexer>>(_jsonOptions, cancellationToken);

            return indexers ?? new List<ProwlarrIndexer>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get indexers from Prowlarr");
            throw;
        }
    }

    public async Task<List<ProwlarrHealthCheck>> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await CreateRequestAsync(HttpMethod.Get, "/api/v1/health", cancellationToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var health = await response.Content.ReadFromJsonAsync<List<ProwlarrHealthCheck>>(_jsonOptions, cancellationToken);

            return health ?? new List<ProwlarrHealthCheck>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get health status from Prowlarr");
            throw;
        }
    }
}
