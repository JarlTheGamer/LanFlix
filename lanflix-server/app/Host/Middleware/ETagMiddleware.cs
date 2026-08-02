using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Lanflix.Host.Middleware;

/// <summary>
/// Lightweight ETag middleware that computes response hashes for API endpoints 
/// and returns 304 Not Modified when clients provide a matching If-None-Match header.
/// Follows Plex & Jellyfin HTTP caching standards.
/// </summary>
public class ETagMiddleware
{
    private readonly RequestDelegate _next;

    public ETagMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply ETags to GET requests on API endpoints
        if (context.Request.Method != HttpMethods.Get || !context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        var originalBodyStream = context.Response.Body;
        using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;

        await _next(context);

        if (context.Response.StatusCode == StatusCodes.Status200OK)
        {
            memoryStream.Position = 0;
            var responseBytes = memoryStream.ToArray();
            var etag = CalculateETag(responseBytes);

            context.Response.Headers.ETag = etag;
            context.Response.Headers.CacheControl = "private, no-cache, revalidate";

            if (context.Request.Headers.TryGetValue("If-None-Match", out var ifNoneMatch) && ifNoneMatch == etag)
            {
                context.Response.StatusCode = StatusCodes.Status304NotModified;
                context.Response.ContentLength = 0;
                context.Response.Body = originalBodyStream;
                return;
            }

            memoryStream.Position = 0;
            await memoryStream.CopyToAsync(originalBodyStream);
        }
        else
        {
            memoryStream.Position = 0;
            await memoryStream.CopyToAsync(originalBodyStream);
        }
    }

    private static string CalculateETag(byte[] content)
    {
        var hash = SHA256.HashData(content);
        return $"\"{Convert.ToBase64String(hash)}\"";
    }
}
