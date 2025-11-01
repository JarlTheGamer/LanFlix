# Caching Layer Implementation

This directory contains the caching layer implementation for the Lanflix backend, providing a two-tier hybrid caching strategy for optimal performance.

## Architecture

The caching layer implements a **two-tier hybrid cache** strategy:

1. **L1 Cache (Memory)**: Fast in-memory cache using `IMemoryCache` for hot data
2. **L2 Cache (Redis)**: Distributed cache using Redis for shared data across instances

## Components

### ICacheService Interface

The core interface that all cache implementations must follow:

```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default);
}
```

### MemoryCacheService (L1 Cache)

- **Purpose**: Fast in-memory caching for frequently accessed data
- **Default Expiration**: 5 minutes
- **Features**:
  - Tag-based cache invalidation using in-memory tag index
  - Automatic cleanup on eviction
  - Thread-safe operations using `ConcurrentDictionary`

### RedisCacheService (L2 Cache)

- **Purpose**: Distributed caching for shared data across multiple server instances
- **Default Expiration**: 1 hour
- **Features**:
  - JSON serialization for complex objects
  - Tag-based cache invalidation using Redis Sets
  - Graceful error handling (logs errors but doesn't throw)
  - Connection pooling via `IConnectionMultiplexer`

### HybridCacheService (Primary Implementation)

- **Purpose**: Combines L1 and L2 caches for optimal performance
- **Strategy**: Cache-aside pattern with automatic L1 population
- **Features**:
  - Tries L1 first, then L2
  - Populates L1 on L2 hit (with shorter expiration)
  - Invalidates both caches on remove operations
  - Helper methods: `GetOrSetAsync`, `GetOrSetWithTagsAsync`

## Cache Flow

```
┌─────────────┐
│   Request   │
└──────┬──────┘
       │
       ▼
┌─────────────┐
│  L1 Cache   │  ◄── Memory (Fast)
│  (Memory)   │
└──────┬──────┘
       │ Miss
       ▼
┌─────────────┐
│  L2 Cache   │  ◄── Redis (Distributed)
│   (Redis)   │
└──────┬──────┘
       │ Miss
       ▼
┌─────────────┐
│  Database   │  ◄── Source of Truth
└─────────────┘
```

## Configuration

### appsettings.json

```json
{
  "Lanflix": {
    "Cache": {
      "Redis": {
        "Enabled": false,
        "ConnectionString": "localhost:6379",
        "InstanceName": "lanflix:"
      },
      "Memory": {
        "SizeLimit": 512
      }
    }
  }
}
```

### Dependency Injection

The caching services are automatically registered in `Infrastructure/DependencyInjection.cs`:

- If Redis is enabled: `HybridCacheService` is registered as `ICacheService`
- If Redis is disabled: `MemoryCacheService` is registered as `ICacheService`

## Usage

### In Query Handlers (Automatic via CachingBehavior)

Queries that implement `ICacheableQuery` are automatically cached:

```csharp
public class GetLibraryItemsQuery : IRequest<PaginatedList<ContentDto>>, ICacheableQuery
{
    public string CacheKey => $"library:items:{Type}:{PageNumber}:{PageSize}";
    public TimeSpan? CacheExpiration => TimeSpan.FromMinutes(10);
}
```

### In Command Handlers (Manual Cache Invalidation)

Commands should invalidate relevant cache entries:

```csharp
public class AddContentCommandHandler : IRequestHandler<AddContentCommand, int>
{
    private readonly ICacheService _cacheService;

    public async Task<int> Handle(AddContentCommand request, CancellationToken ct)
    {
        // ... add content logic ...
        
        // Invalidate cache
        await _cacheService.RemoveByTagAsync("library", ct);
        await _cacheService.RemoveAsync($"content:{contentId}", ct);
        
        return contentId;
    }
}
```

### Direct Usage

```csharp
// Get or set with automatic caching
var content = await _cacheService.GetOrSetAsync(
    key: $"content:{id}",
    factory: async ct => await _context.Contents.FindAsync(id, ct),
    expiration: TimeSpan.FromHours(1),
    cancellationToken: ct);

// Set with tags for group invalidation
await _cacheService.SetWithTagsAsync(
    key: $"content:{id}",
    value: content,
    tags: new[] { "library", "content" },
    expiration: TimeSpan.FromHours(1),
    cancellationToken: ct);

// Invalidate by tag
await _cacheService.RemoveByTagAsync("library", ct);
```

## Cache Keys Strategy

### Library Items
- Pattern: `library:{type}:{page}:{pageSize}:{searchTerm}:{genre}:{sortBy}:{sortDescending}`
- Expiration: 10 minutes
- Tags: `library`

### Content Details
- Pattern: `content:{id}`
- Expiration: 1 hour
- Tags: `content`, `library`

### Profiles
- Pattern: `profiles:all`
- Expiration: 10 minutes
- Tags: `profiles`

### Watch History
- Pattern: `profile:{profileId}:history:{limit}`
- Expiration: 5 minutes
- Tags: `history`

### Stream Sessions
- Pattern: `session:{sessionId}:info`
- Expiration: 30 seconds
- Tags: `sessions`

## Cache Invalidation Strategy

### On Content Changes
- Invalidate: `library` tag (affects all library queries)
- Invalidate: `content:{id}` (specific content details)

### On Profile Changes
- Invalidate: `profiles:all` (all profiles list)
- Invalidate: `profile:{id}:prefs` (specific profile preferences)

### On Watch History Updates
- Invalidate: `profile:{profileId}:history:{limit}` (specific profile history)

## Performance Considerations

1. **L1 Cache (Memory)**:
   - Very fast access (microseconds)
   - Limited by available memory
   - Not shared across instances

2. **L2 Cache (Redis)**:
   - Fast access (milliseconds)
   - Shared across instances
   - Requires network round-trip

3. **Cache-Aside Pattern**:
   - Application controls cache population
   - Lazy loading on cache miss
   - Explicit invalidation on data changes

## Monitoring

Cache performance can be monitored through:
- Log traces for cache hits/misses
- Redis INFO command for L2 cache statistics
- Memory usage metrics for L1 cache

## Best Practices

1. **Use appropriate expiration times**:
   - Frequently changing data: 5-10 minutes
   - Stable data: 1 hour
   - Session data: 30 seconds

2. **Implement tag-based invalidation**:
   - Group related cache entries with tags
   - Invalidate entire groups when needed

3. **Handle cache failures gracefully**:
   - Redis failures should not break the application
   - Fall back to database on cache errors

4. **Monitor cache hit ratios**:
   - Target: >70% hit ratio for metadata
   - Adjust expiration times based on metrics

## Testing

The caching layer can be tested by:
1. Running with Redis disabled (memory-only mode)
2. Running with Redis enabled (hybrid mode)
3. Verifying cache invalidation on data changes
4. Monitoring cache hit/miss ratios in logs
