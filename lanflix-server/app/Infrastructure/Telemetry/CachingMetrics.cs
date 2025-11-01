using System.Diagnostics.Metrics;

namespace Lanflix.Infrastructure.Telemetry;

/// <summary>
/// Provides custom metrics for caching operations
/// </summary>
public class CachingMetrics
{
    private readonly Meter _meter;
    private readonly Counter<long> _cacheHits;
    private readonly Counter<long> _cacheMisses;
    private readonly Histogram<double> _cacheOperationDuration;
    
    private long _totalHits;
    private long _totalMisses;
    
    public CachingMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create("Lanflix.Caching");
        
        _cacheHits = _meter.CreateCounter<long>(
            name: "cache.hits",
            unit: "{hit}",
            description: "Number of cache hits");
        
        _cacheMisses = _meter.CreateCounter<long>(
            name: "cache.misses",
            unit: "{miss}",
            description: "Number of cache misses");
        
        _cacheOperationDuration = _meter.CreateHistogram<double>(
            name: "cache.operation.duration",
            unit: "ms",
            description: "Duration of cache operations");
        
        // Observable gauge for cache hit ratio
        _meter.CreateObservableGauge<double>(
            name: "cache.hit_ratio",
            observeValue: GetCacheHitRatio,
            unit: "{ratio}",
            description: "Cache hit ratio (hits / total requests)");
    }
    
    /// <summary>
    /// Records a cache hit
    /// </summary>
    public void RecordCacheHit(string cacheType)
    {
        Interlocked.Increment(ref _totalHits);
        _cacheHits.Add(1, new KeyValuePair<string, object?>("cache_type", cacheType));
    }
    
    /// <summary>
    /// Records a cache miss
    /// </summary>
    public void RecordCacheMiss(string cacheType)
    {
        Interlocked.Increment(ref _totalMisses);
        _cacheMisses.Add(1, new KeyValuePair<string, object?>("cache_type", cacheType));
    }
    
    /// <summary>
    /// Records the duration of a cache operation
    /// </summary>
    public void RecordOperationDuration(double durationMs, string operation, string cacheType)
    {
        _cacheOperationDuration.Record(durationMs,
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("cache_type", cacheType));
    }
    
    private double GetCacheHitRatio()
    {
        var hits = Interlocked.Read(ref _totalHits);
        var misses = Interlocked.Read(ref _totalMisses);
        var total = hits + misses;
        
        return total > 0 ? (double)hits / total : 0.0;
    }
}
