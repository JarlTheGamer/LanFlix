using Lanflix.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Lanflix.WebApi.Filters;

/// <summary>
/// Action filter that wraps responses in legacy API format when requested
/// </summary>
public class LegacyResponseWrapperAttribute : ActionFilterAttribute
{
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        // Check if client requests legacy format via header or query parameter
        var useLegacyFormat = ShouldUseLegacyFormat(context.HttpContext.Request);

        if (!useLegacyFormat)
        {
            base.OnActionExecuted(context);
            return;
        }

        // Only wrap successful responses
        if (context.Result is ObjectResult objectResult && objectResult.StatusCode >= 200 && objectResult.StatusCode < 300)
        {
            var wrappedResponse = typeof(LegacyApiResponse<>)
                .MakeGenericType(objectResult.Value?.GetType() ?? typeof(object))
                .GetMethod(nameof(LegacyApiResponse<object>.SuccessResponse))
                ?.Invoke(null, new[] { objectResult.Value, "Success" });

            context.Result = new ObjectResult(wrappedResponse)
            {
                StatusCode = objectResult.StatusCode
            };
        }
        else if (context.Result is ObjectResult errorResult && errorResult.StatusCode >= 400)
        {
            // Wrap error responses
            var message = errorResult.Value?.ToString() ?? "An error occurred";
            var wrappedError = LegacyApiResponse<object>.ErrorResponse(message);

            context.Result = new ObjectResult(wrappedError)
            {
                StatusCode = errorResult.StatusCode
            };
        }

        base.OnActionExecuted(context);
    }

    private static bool ShouldUseLegacyFormat(HttpRequest request)
    {
        // Check for legacy format header
        if (request.Headers.TryGetValue("X-Api-Format", out var formatHeader))
        {
            return formatHeader.ToString().Equals("legacy", StringComparison.OrdinalIgnoreCase);
        }

        // Check for legacy format query parameter
        if (request.Query.TryGetValue("format", out var formatQuery))
        {
            return formatQuery.ToString().Equals("legacy", StringComparison.OrdinalIgnoreCase);
        }

        // Check for old API version header
        if (request.Headers.TryGetValue("X-Api-Version", out var versionHeader))
        {
            return versionHeader.ToString().StartsWith("1.", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}
