using Lanflix.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace Lanflix.WebApi.Extensions;

/// <summary>
/// Extension methods for controllers to support legacy response formats
/// </summary>
public static class ControllerExtensions
{
    /// <summary>
    /// Returns a legacy-formatted success response
    /// </summary>
    public static ActionResult<LegacyApiResponse<T>> LegacyOk<T>(
        this ControllerBase controller,
        T data,
        string message = "Success")
    {
        return controller.Ok(LegacyApiResponse<T>.SuccessResponse(data, message));
    }

    /// <summary>
    /// Returns a legacy-formatted error response with NotFound status
    /// </summary>
    public static ActionResult<LegacyApiResponse<T>> LegacyNotFound<T>(
        this ControllerBase controller,
        string message)
    {
        return controller.NotFound(LegacyApiResponse<T>.ErrorResponse(message));
    }

    /// <summary>
    /// Returns a legacy-formatted error response with BadRequest status
    /// </summary>
    public static ActionResult<LegacyApiResponse<T>> LegacyBadRequest<T>(
        this ControllerBase controller,
        string message)
    {
        return controller.BadRequest(LegacyApiResponse<T>.ErrorResponse(message));
    }

    /// <summary>
    /// Checks if the current request is from a legacy client
    /// </summary>
    public static bool IsLegacyClient(this ControllerBase controller)
    {
        return controller.HttpContext.Items["IsLegacyClient"] as bool? ?? false;
    }

    /// <summary>
    /// Gets the API version from the current request
    /// </summary>
    public static Version GetApiVersion(this ControllerBase controller)
    {
        return controller.HttpContext.Items["ApiVersion"] as Version ?? new Version(2, 0);
    }

    /// <summary>
    /// Returns either a legacy-formatted or standard response based on client version
    /// </summary>
    public static ActionResult<object> AdaptiveOk<T>(
        this ControllerBase controller,
        T data,
        string message = "Success")
    {
        if (controller.IsLegacyClient())
        {
            return controller.Ok(LegacyApiResponse<T>.SuccessResponse(data, message));
        }

        return controller.Ok(data);
    }
}
