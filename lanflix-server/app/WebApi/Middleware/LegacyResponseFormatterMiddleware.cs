using System.Text;
using System.Text.Json;
using Lanflix.Application.Common.Models;

namespace Lanflix.WebApi.Middleware;

/// <summary>
/// Middleware that wraps API responses in legacy format for backward compatibility
/// </summary>
public class LegacyResponseFormatterMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LegacyResponseFormatterMiddleware> _logger;

    public LegacyResponseFormatterMiddleware(
        RequestDelegate next,
        ILogger<LegacyResponseFormatterMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if this is a legacy client
        var isLegacyClient = context.Items["IsLegacyClient"] as bool? ?? false;

        // Skip wrapping for non-legacy clients or non-API endpoints
        if (!isLegacyClient || !context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        // Skip wrapping for streaming endpoints (binary data)
        if (context.Request.Path.Value?.Contains("/stream") == true &&
            context.Request.Method == HttpMethods.Get)
        {
            await _next(context);
            return;
        }

        // Capture the original response
        var originalBodyStream = context.Response.Body;

        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);

            // Only wrap JSON responses
            var contentType = context.Response.ContentType ?? string.Empty;
            if (!contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
            {
                await CopyResponseAsync(responseBody, originalBodyStream);
                return;
            }

            // Read the response
            responseBody.Seek(0, SeekOrigin.Begin);
            var responseText = await new StreamReader(responseBody).ReadToEndAsync();

            // Wrap the response in legacy format
            var statusCode = context.Response.StatusCode;
            var wrappedResponse = WrapResponse(responseText, statusCode);

            // Write the wrapped response
            context.Response.Body = originalBodyStream;
            context.Response.ContentType = "application/json; charset=utf-8";
            
            var wrappedJson = JsonSerializer.Serialize(wrappedResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(wrappedJson, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in legacy response formatter middleware");
            context.Response.Body = originalBodyStream;
            await CopyResponseAsync(responseBody, originalBodyStream);
        }
    }

    private static object WrapResponse(string responseText, int statusCode)
    {
        var isSuccess = statusCode >= 200 && statusCode < 300;

        if (string.IsNullOrWhiteSpace(responseText))
        {
            return new
            {
                success = isSuccess,
                data = (object?)null,
                message = isSuccess ? "Success" : "An error occurred",
                version = "2.0.0"
            };
        }

        try
        {
            // Try to parse the response as JSON
            var data = JsonSerializer.Deserialize<object>(responseText);

            return new
            {
                success = isSuccess,
                data,
                message = isSuccess ? "Success" : GetErrorMessage(responseText),
                version = "2.0.0"
            };
        }
        catch
        {
            // If parsing fails, treat as plain text
            return new
            {
                success = isSuccess,
                data = isSuccess ? responseText : (object?)null,
                message = isSuccess ? "Success" : responseText,
                version = "2.0.0"
            };
        }
    }

    private static string GetErrorMessage(string responseText)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseText);
            
            // Try to extract message from common error response formats
            if (doc.RootElement.TryGetProperty("message", out var messageElement))
            {
                return messageElement.GetString() ?? "An error occurred";
            }

            if (doc.RootElement.TryGetProperty("error", out var errorElement))
            {
                return errorElement.GetString() ?? "An error occurred";
            }

            if (doc.RootElement.TryGetProperty("title", out var titleElement))
            {
                return titleElement.GetString() ?? "An error occurred";
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return "An error occurred";
    }

    private static async Task CopyResponseAsync(Stream source, Stream destination)
    {
        source.Seek(0, SeekOrigin.Begin);
        await source.CopyToAsync(destination);
    }
}

/// <summary>
/// Extension methods for registering the legacy response formatter middleware
/// </summary>
public static class LegacyResponseFormatterMiddlewareExtensions
{
    public static IApplicationBuilder UseLegacyResponseFormatter(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<LegacyResponseFormatterMiddleware>();
    }
}
