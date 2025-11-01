# Monitoring and Observability

This directory contains the monitoring and observability implementation for Lanflix, including OpenTelemetry tracing, custom metrics, health checks, and structured logging.

## Components

### 1. OpenTelemetry Integration

OpenTelemetry provides distributed tracing and metrics collection for the application.

#### Activity Sources

- **LanflixActivitySource.cs**: Defines custom activity sources for different parts of the application
  - `Lanflix.Streaming`: Traces streaming operations
  - `Lanflix.Transcoding`: Traces transcoding operations
  - `Lanflix.Library`: Traces library operations

#### Configuration

OpenTelemetry is configured in `Program.cs` with:
- ASP.NET Core instrumentation (HTTP requests)
- HTTP client instrumentation (external API calls)
- Entity Framework Core instrumentation (database queries)
- Custom activity sources for domain-specific operations

#### Usage Example

```csharp
using Lanflix.Infrastructure.Telemetry;
using System.Diagnostics;

public class MyService
{
    public async Task DoWorkAsync()
    {
        using var activity = LanflixActivitySource.Streaming.StartActivity("ProcessStream");
        activity?.SetTag("contentId", 123);
        activity?.SetTag("streamingMode", "DirectPlay");
        
        try
        {
            // Do work
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
```

### 2. Custom Metrics

#### StreamingMetrics

Tracks streaming-related metrics:
- **streams.started**: Counter for stream start events (tagged by streaming mode and content type)
- **stream.duration**: Histogram of stream durations (tagged by streaming mode and completion status)
- **streams.active**: Gauge showing current active stream count
- **transcoding.queue_depth**: Gauge showing transcoding queue depth

#### CachingMetrics

Tracks caching performance:
- **cache.hits**: Counter for cache hits (tagged by cache type: L1, L2, hybrid)
- **cache.misses**: Counter for cache misses
- **cache.operation.duration**: Histogram of cache operation durations
- **cache.hit_ratio**: Gauge showing cache hit ratio

#### Usage Example

```csharp
public class StreamingController
{
    private readonly StreamingMetrics _metrics;
    
    public async Task<IActionResult> StartStream(int contentId)
    {
        _metrics.RecordStreamStart("DirectPlay", "Movie");
        
        // ... streaming logic ...
        
        return Ok();
    }
}
```

### 3. Health Checks

Health checks are available at the following endpoints:

#### Endpoints

- **GET /health**: Detailed health check with all components
- **GET /health/ready**: Readiness check (for Kubernetes readiness probes)
- **GET /health/live**: Liveness check (for Kubernetes liveness probes)

#### Health Check Components

1. **Database Health Check**: Verifies EF Core DbContext connectivity
2. **Redis Health Check**: Verifies Redis cache connectivity (if enabled)
3. **FFmpeg Health Check**: Verifies FFmpeg is installed and accessible
4. **Disk Space Health Check**: Monitors disk space on media paths

#### Health Check Response Example

```json
{
  "status": "Healthy",
  "timestamp": "2024-01-15T10:30:00Z",
  "duration": "00:00:00.1234567",
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "description": "Database is accessible",
      "duration": "00:00:00.0123456"
    },
    {
      "name": "ffmpeg",
      "status": "Healthy",
      "description": "FFmpeg is available: ffmpeg version 6.0",
      "duration": "00:00:00.0234567",
      "data": {
        "version": "ffmpeg version 6.0",
        "exitCode": 0
      }
    },
    {
      "name": "disk-space",
      "status": "Healthy",
      "description": "Sufficient disk space available",
      "duration": "00:00:00.0012345",
      "data": {
        "disk_0_path": "D:/Media/Movies",
        "disk_0_drive": "D:\\",
        "disk_0_free_gb": 250.5,
        "disk_0_total_gb": 500.0,
        "disk_0_used_percent": 49.9
      }
    }
  ]
}
```

### 4. Structured Logging

Serilog is configured with:
- **Console sink**: For development and container logs
- **File sink**: Rolling daily logs with 30-day retention
- **Error file sink**: Separate error logs with 90-day retention
- **Enrichers**: Machine name, thread ID, environment, application name
- **Sensitive data redaction**: Automatically redacts passwords, tokens, API keys

#### Log Configuration

Logs are written to:
- `logs/lanflix-YYYYMMDD.log`: General application logs
- `logs/lanflix-errors-YYYYMMDD.log`: Error logs only

#### Log Rotation

- Daily rotation
- 100MB file size limit per log file
- Automatic rollover when size limit is reached
- Retention: 30 days for general logs, 90 days for error logs

#### Sensitive Data Redaction

The following property names are automatically redacted:
- password, pwd
- secret
- apikey, api_key
- token
- authorization, auth
- jwt, bearer
- connectionstring, connection_string

#### Logging Best Practices

```csharp
// Good: Structured logging with properties
_logger.LogInformation(
    "Stream started for content {ContentId} by profile {ProfileId} using {StreamingMode}",
    contentId, profileId, streamingMode);

// Bad: String interpolation (not structured)
_logger.LogInformation($"Stream started for content {contentId}");

// Good: Log levels
_logger.LogTrace("Detailed trace information");
_logger.LogDebug("Debug information for development");
_logger.LogInformation("General informational messages");
_logger.LogWarning("Warning messages for potential issues");
_logger.LogError(ex, "Error occurred during operation");
_logger.LogCritical(ex, "Critical error requiring immediate attention");
```

## Monitoring Dashboard Integration

### Prometheus/Grafana

To export metrics to Prometheus, uncomment the OTLP exporter in `Program.cs` and configure the endpoint:

```csharp
.AddOtlpExporter(options =>
{
    options.Endpoint = new Uri("http://localhost:4317");
})
```

### Application Insights

For Azure Application Insights integration, add the package and configure:

```bash
dotnet add package Microsoft.ApplicationInsights.AspNetCore
```

```csharp
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
});
```

### Jaeger

For Jaeger tracing, configure the OTLP exporter to point to your Jaeger instance:

```csharp
.AddOtlpExporter(options =>
{
    options.Endpoint = new Uri("http://jaeger:4317");
})
```

## Performance Considerations

- OpenTelemetry tracing has minimal overhead (<1% CPU)
- Metrics collection is lightweight and uses observable gauges where possible
- Health checks are cached and run on-demand
- Structured logging uses efficient serialization

## Troubleshooting

### High Memory Usage

If memory usage is high, check:
1. Log file retention settings
2. Number of active traces being collected
3. Cache size limits

### Missing Metrics

If metrics are not appearing:
1. Verify OpenTelemetry exporters are configured
2. Check that custom meters are registered in DI
3. Ensure metrics are being recorded in the code

### Health Check Failures

Common health check issues:
1. **Database**: Check connection string and database accessibility
2. **Redis**: Verify Redis is running and connection string is correct
3. **FFmpeg**: Ensure FFmpeg is installed and in PATH
4. **Disk Space**: Check media path configuration and permissions

## Configuration

### appsettings.json

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning"
      }
    }
  },
  "OpenTelemetry": {
    "OtlpEndpoint": "http://localhost:4317"
  }
}
```

## References

- [OpenTelemetry .NET Documentation](https://opentelemetry.io/docs/instrumentation/net/)
- [Serilog Documentation](https://serilog.net/)
- [ASP.NET Core Health Checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)
