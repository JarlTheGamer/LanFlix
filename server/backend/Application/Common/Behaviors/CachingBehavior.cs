using Lanflix.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Lanflix.Application.Common.Behaviors;

/// <summary>
/// Marker interface for queries that should be cached
/// </summary>
public interface ICacheableQuery
{
    string CacheKey { get; }
    TimeSpan? CacheExpiration { get; }
}

public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(
        ICacheService cacheService,
        ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only cache queries that implement ICacheableQuery
        if (request is not ICacheableQuery cacheableQuery)
        {
            return await next();
        }

        var cacheKey = cacheableQuery.CacheKey;

        // Try to get from cache
        var cachedResponse = await _cacheService.GetAsync<TResponse>(cacheKey, cancellationToken);
        if (cachedResponse != null)
        {
            _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
            return cachedResponse;
        }

        _logger.LogDebug("Cache miss for {CacheKey}", cacheKey);

        // Execute the request
        var response = await next();

        // Cache the response
        await _cacheService.SetAsync(
            cacheKey,
            response,
            cacheableQuery.CacheExpiration,
            cancellationToken);

        return response;
    }
}
