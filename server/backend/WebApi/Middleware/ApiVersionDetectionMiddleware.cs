namespace Lanflix.WebApi.Middleware;

/// <summary>
/// Middleware that detects API version from request headers and adds it to HttpContext
/// </summary>
public class ApiVersionDetectionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiVersionDetectionMiddleware> _logger;

    public ApiVersionDetectionMiddleware(
        RequestDelegate next,
        ILogger<ApiVersionDetectionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Detect API version from various sources
        var apiVersion = DetectApiVersion(context.Request);

        // Store in HttpContext for use by controllers and filters
        context.Items["ApiVersion"] = apiVersion;
        context.Items["IsLegacyClient"] = apiVersion.Major == 1;

        // Add version to response headers
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Api-Version"] = apiVersion.ToString();
            return Task.CompletedTask;
        });

        _logger.LogDebug("Detected API version: {Version} (IsLegacy: {IsLegacy})",
            apiVersion, apiVersion.Major == 1);

        await _next(context);
    }

    private static Version DetectApiVersion(HttpRequest request)
    {
        // 1. Check explicit version header
        if (request.Headers.TryGetValue("X-Api-Version", out var versionHeader))
        {
            if (Version.TryParse(versionHeader.ToString(), out var version))
            {
                return version;
            }
        }

        // 2. Check query parameter
        if (request.Query.TryGetValue("api-version", out var versionQuery))
        {
            if (Version.TryParse(versionQuery.ToString(), out var version))
            {
                return version;
            }
        }

        // 3. Check User-Agent for legacy client detection
        if (request.Headers.TryGetValue("User-Agent", out var userAgent))
        {
            var ua = userAgent.ToString();
            
            // Legacy Node.js backend clients
            if (ua.Contains("Lanflix/1.") || ua.Contains("LanflixClient/1."))
            {
                return new Version(1, 0);
            }
        }

        // 4. Check if using legacy endpoints
        var path = request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        if (path.StartsWith("/api/content") ||
            path.StartsWith("/api/watchhistory") ||
            (path.StartsWith("/api/stream/") && !path.Contains("/start") && !path.Contains("/progress") && !path.Contains("/stop")))
        {
            return new Version(1, 0);
        }

        // 5. Default to current version
        return new Version(2, 0);
    }
}

/// <summary>
/// Extension methods for registering the API version detection middleware
/// </summary>
public static class ApiVersionDetectionMiddlewareExtensions
{
    public static IApplicationBuilder UseApiVersionDetection(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiVersionDetectionMiddleware>();
    }
}
