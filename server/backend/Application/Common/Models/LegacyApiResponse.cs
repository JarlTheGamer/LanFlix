namespace Lanflix.Application.Common.Models;

/// <summary>
/// Legacy API response wrapper for backward compatibility
/// </summary>
/// <typeparam name="T">The type of data being returned</typeparam>
public class LegacyApiResponse<T>
{
    /// <summary>
    /// Indicates if the request was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The response data
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// A message describing the result
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// API version (optional, for new clients)
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Create a successful response
    /// </summary>
    public static LegacyApiResponse<T> SuccessResponse(T data, string message = "Success")
    {
        return new LegacyApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            Version = "2.0.0"
        };
    }

    /// <summary>
    /// Create an error response
    /// </summary>
    public static LegacyApiResponse<T> ErrorResponse(string message)
    {
        return new LegacyApiResponse<T>
        {
            Success = false,
            Data = default,
            Message = message,
            Version = "2.0.0"
        };
    }
}
