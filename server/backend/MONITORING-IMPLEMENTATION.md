# Monitoring and Observability Implementation Summary

This document summarizes the monitoring and observability features implemented for the Lanflix C# backend.

## Overview

The monitoring implementation provides comprehensive observability through:
1. **OpenTelemetry** - Distributed tracing and metrics
2. **Custom Metrics** - Domain-specific performance metrics
3. **Health Checks** - System health monitoring
4. **Structured Logging** - Enhanced logging with Serilog

## Implementation Details

### 1. OpenTelemetry Integration

**Location**: `WebApi/Program.cs`

**Features**:
- ASP.NET Core instrumentation (HTTP requests, middleware)
- HTTP client instrumentation (external API calls)
- Entity Framework Core instrumentation (database queries)
- Custom activity sources for streaming, transcoding, and library operations
- Console exporter for development
- OTLP exporter support (commented out, ready for production)

**Custom Activity Sources**:
- `Lanflix.Streaming` - Tracks streaming operations
- `Lanflix.Transcoding` - Tracks transcoding operations
- `Lanflix.Library` - Tracks library operations

**Files**:
- `Infrastructure/Telemetry/LanflixActivitySource.cs`

### 2. Custom Metrics

#### Streaming Metrics

**Location**: `Infrastructure/Telemetry/StreamingMetrics.cs`

**Metrics**:
- `streams.started` (Counter) - Number of streams started, tagged by streaming mode and content type
- `stream.duration` (Histogram) - Stream duration in seconds, tagged by streaming mode and completion status
- `streams.active` (ObservableGauge) - Current number of active streams
- `transcoding.queue_depth` (ObservableGauge) - Number of sessions waiting for transcoding

**Integration**: Integrated into `StreamingController` to record metrics on stream start and stop

#### Caching Metrics

**Location**: `Infrastructure/Telemetry/CachingMetrics.cs`

**Metrics**:
- `cache.hits` (Counter) - Number of cache hits, tagged by cache type (L1, L2, hybrid)
- `cache.misses` (Counter) - Number of cache misses
- `cache.operation.duration` (Histogram) - Duration of cache operations in milliseconds
- `cache.hit_ratio` (ObservableGauge) - Cache hit ratio (hits / total requests)

**Integration**: Integrated into `HybridCacheService` to track cache performance

### 3. Health Checks

**Endpoints**:
- `GET /health` - Detailed health check with all components (JSON response)
- `GET /health/ready` - Readiness check for Kubernetes/load balancers
- `GET /health/live` - Liveness check (simple ping)

**Health Check Components**:

#### Database Health Check
- **Type**: Built-in EF Core health check
- **Checks**: Database connectivity and accessibility
- **Status**: Healthy/Unhealthy

#### Redis Health Check
- **Type**: AspNetCore.HealthChecks.Redis
- **Checks**: Redis connectivity (if enabled in configuration)
- **Status**: Healthy/Unhealthy
- **Note**: Only registered if Redis is enabled in configuration

#### FFmpeg Health Check
- **Location**: `Infrastructure/HealthChecks/FFmpegHealthCheck.cs`
- **Checks**: FFmpeg installation and version
- **Status**: Healthy/Degraded/Unhealthy
- **Data**: FFmpeg version information

#### Disk Space Health Check
- **Location**: `Infrastructure/HealthChecks/DiskSpaceHealthCheck.cs`
- **Checks**: Available disk space on media paths
- **Thresholds**:
  - Unhealthy: < 5GB free space
  - Degraded: > 90% disk usage
  - Healthy: Otherwise
- **Data**: Free space, total space, and usage percentage for each drive

### 4. Structured Logging

**Configuration**: `appsettings.json` and `WebApi/Program.cs`

**Features**:
- **Console Sink**: For development and container logs
- **File Sink**: Rolling daily logs with 30-day retention
  - `logs/lanflix-YYYYMMDD.log` - All logs
  - `logs/lanflix-errors-YYYYMMDD.log` - Error logs only (90-day retention)
- **Log Rotation**: Daily rotation with 100MB file size limit
- **Enrichers**:
  - FromLogContext
  - MachineName
  - ThreadId
  - Application name
  - Environment name
  - Custom sensitive data redaction

#### Sensitive Data Redaction

**Location**: `Infrastructure/Logging/SensitiveDataRedactionEnricher.cs`

**Redacted Properties**:
- password, pwd
- secret
- apikey, api_key
- token
- authorization, auth
- jwt, bearer
- connectionstring, connection_string

**Behavior**: Automatically replaces sensitive values with `***REDACTED***`

## Configuration

### appsettings.json

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/lanflix-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "fileSizeLimitBytes": 104857600,
          "rollOnFileSizeLimit": true
        }
      }
    ]
  }
}
```

### OpenTelemetry Exporters

For production, uncomment the OTLP exporter in `Program.cs`:

```csharp
.AddOtlpExporter(options =>
{
    options.Endpoint = new Uri(builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317");
})
```

Then add to `appsettings.json`:

```json
{
  "OpenTelemetry": {
    "OtlpEndpoint": "http://your-collector:4317"
  }
}
```

## Usage Examples

### Recording Custom Traces

```csharp
using Lanflix.Infrastructure.Telemetry;
using System.Diagnostics;

public class MyService
{
    public async Task ProcessStreamAsync(int contentId)
    {
        using var activity = LanflixActivitySource.Streaming.StartActivity("ProcessStream");
        activity?.SetTag("contentId", contentId);
        activity?.SetTag("streamingMode", "DirectPlay");
        
        try
        {
            // Do work
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
}
```

### Recording Custom Metrics

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
    
    public async Task<IActionResult> StopStream(string sessionId)
    {
        var duration = CalculateDuration();
        _metrics.RecordStreamDuration(duration, "DirectPlay", completed: true);
        
        return NoContent();
    }
}
```

### Structured Logging

```csharp
// Good: Structured logging with properties
_logger.LogInformation(
    "Stream started for content {ContentId} by profile {ProfileId} using {StreamingMode}",
    contentId, profileId, streamingMode);

// Good: Error logging with exception
_logger.LogError(ex, 
    "Failed to start stream for content {ContentId}", 
    contentId);
```

## Monitoring Dashboards

### Prometheus + Grafana

1. Configure OTLP exporter to send to Prometheus
2. Import Grafana dashboards for:
   - ASP.NET Core metrics
   - Custom streaming metrics
   - Cache performance metrics

### Application Insights (Azure)

1. Add package: `Microsoft.ApplicationInsights.AspNetCore`
2. Configure in `Program.cs`:

```csharp
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
});
```

### Jaeger (Distributed Tracing)

1. Configure OTLP exporter endpoint to Jaeger
2. View traces at `http://jaeger-ui:16686`

## Health Check Monitoring

### Kubernetes Integration

```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 5000
  initialDelaySeconds: 30
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /health/ready
    port: 5000
  initialDelaySeconds: 10
  periodSeconds: 5
```

### Load Balancer Health Checks

Configure your load balancer to use `/health/ready` endpoint for health checks.

## Performance Impact

- **OpenTelemetry**: < 1% CPU overhead
- **Custom Metrics**: Negligible (uses observable gauges)
- **Health Checks**: On-demand, no continuous overhead
- **Structured Logging**: Efficient binary serialization

## Troubleshooting

### No Metrics Appearing

1. Verify OpenTelemetry exporters are configured
2. Check that custom meters are registered in DI
3. Ensure metrics are being recorded in code

### Health Check Failures

- **Database**: Check connection string
- **Redis**: Verify Redis is running and connection string is correct
- **FFmpeg**: Ensure FFmpeg is in PATH
- **Disk Space**: Check media path configuration

### High Log Volume

Adjust log levels in `appsettings.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Warning"
    }
  }
}
```

## Next Steps

1. Set up Prometheus/Grafana for metrics visualization
2. Configure alerting rules for critical metrics
3. Create custom dashboards for streaming performance
4. Integrate with centralized logging (e.g., ELK stack)
5. Set up distributed tracing visualization (Jaeger/Zipkin)

## References

- [OpenTelemetry .NET Documentation](https://opentelemetry.io/docs/instrumentation/net/)
- [Serilog Documentation](https://serilog.net/)
- [ASP.NET Core Health Checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)
- [Infrastructure/Telemetry/README.md](Infrastructure/Telemetry/README.md) - Detailed implementation guide
