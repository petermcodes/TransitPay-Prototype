namespace TransitPay.API.DTOs.Common;

/// <summary>
/// Generic envelope used for consistent API responses.
/// </summary>
/// <typeparam name="T">The payload type carried in <see cref="Data"/>.</typeparam>
public class ApiResponse<T>
{
    /// <summary>Whether the operation succeeded.</summary>
    public bool Success { get; set; }

    /// <summary>A human-readable result message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>The operation payload. Null on failure.</summary>
    public T? Data { get; set; }

    /// <summary>Builds a success response envelope.</summary>
    public static ApiResponse<T> SuccessResponse(T data, string message = "Operation successful")
    {
        return new ApiResponse<T> { Success = true, Message = message, Data = data };
    }

    /// <summary>Builds an error response envelope (no payload).</summary>
    public static ApiResponse<T> ErrorResponse(string message)
    {
        return new ApiResponse<T> { Success = false, Message = message };
    }
}