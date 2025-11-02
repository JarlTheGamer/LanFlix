using System.Net.Http.Json;
using System.Text.Json;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.ExternalApis;

public class ProwlarrClient : IProwlarrClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProwlarrClient> _logger;
    private readonly string _apiKey;
    private readonly JsonSerializerOptions _jsonOptions;

    public ProwlarrClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ProwlarrClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["Lanflix:ExternalApis:Prowlarr:ApiKey"] ?? string.Empty;

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
            var response = await _httpClient.GetAsync("/api/v1/system/status", cancellationToken);
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

            var results = await _httpClient.GetFromJsonAsync<List<ProwlarrSearchResult>>(
                url,
                _jsonOptions,
                cancellationToken);

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
            var indexers = await _httpClient.GetFromJsonAsync<List<ProwlarrIndexer>>(
                "/api/v1/indexer",
                _jsonOptions,
                cancellationToken);

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
            var health = await _httpClient.GetFromJsonAsync<List<ProwlarrHealthCheck>>(
                "/api/v1/health",
                _jsonOptions,
                cancellationToken);

            return health ?? new List<ProwlarrHealthCheck>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get health status from Prowlarr");
            throw;
        }
    }
}
