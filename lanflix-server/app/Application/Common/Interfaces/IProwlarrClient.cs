using Lanflix.Application.Common.Models;

namespace Lanflix.Application.Common.Interfaces;

/// <summary>
/// Interface for Prowlarr API client (indexer aggregator)
/// </summary>
public interface IProwlarrClient
{
    /// <summary>
    /// Test connection to Prowlarr
    /// </summary>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Search across all indexers
    /// </summary>
    Task<List<ProwlarrSearchResult>> SearchAsync(string query, string? type = null, int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all configured indexers
    /// </summary>
    Task<List<ProwlarrIndexer>> GetIndexersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get health status
    /// </summary>
    Task<List<ProwlarrHealthCheck>> GetHealthAsync(CancellationToken cancellationToken = default);
}
