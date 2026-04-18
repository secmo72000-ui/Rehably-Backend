namespace Rehably.Application.Common;

/// <summary>
/// Standard API response wrapper for all API endpoints.
/// </summary>
/// <typeparam name="T">The type of data being returned.</typeparam>
public record ApiResponse<T>
{
    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The data returned by the operation (when successful).
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// A human-readable message about the operation result.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Error details (when the operation failed).
    /// </summary>
    public ApiError? Error { get; init; }

    /// <summary>
    /// Creates a successful response with data.
    /// </summary>
    public static ApiResponse<T> SuccessResponse(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    /// <summary>
    /// Creates a failure response with error details (standard format).
    /// </summary>
    public static ApiResponse<T> FailureResponse(string code, string message, List<string>? details = null) =>
        new() { Success = false, Error = new ApiError(code, message, details) };

    /// <summary>
    /// Creates a failure response with just a message (backward compatibility).
    /// Uses INTERNAL_ERROR as the default code.
    /// </summary>
    public static ApiResponse<T> FailureResponse(string message) =>
        new() { Success = false, Error = new ApiError(ErrorCodes.InternalError, message) };
}

/// <summary>
/// Non-generic API response for operations that don't return data.
/// </summary>
public record ApiResponse
{
    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// A human-readable message about the operation result.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Error details (when the operation failed).
    /// </summary>
    public ApiError? Error { get; init; }

    /// <summary>
    /// Creates a successful response.
    /// </summary>
    public static ApiResponse SuccessResponse(string? message = null) =>
        new() { Success = true, Message = message };

    /// <summary>
    /// Creates a failure response with error details (standard format).
    /// </summary>
    public static ApiResponse FailureResponse(string code, string message, List<string>? details = null) =>
        new() { Success = false, Error = new ApiError(code, message, details) };

    /// <summary>
    /// Creates a failure response with just a message (backward compatibility).
    /// Uses INTERNAL_ERROR as the default code.
    /// </summary>
    public static ApiResponse FailureResponse(string message) =>
        new() { Success = false, Error = new ApiError(ErrorCodes.InternalError, message) };
}

/// <summary>
/// Structured error information for API responses.
/// </summary>
public record ApiError
{
    /// <summary>
    /// Machine-readable error code (e.g., "VALIDATION_ERROR", "NOT_FOUND").
    /// </summary>
    public string Code { get; init; }

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public string Message { get; init; }

    /// <summary>
    /// Additional details about the error (e.g., validation errors).
    /// </summary>
    public List<string>? Details { get; init; }

    public ApiError(string code, string message, List<string>? details = null)
    {
        Code = code;
        Message = message;
        Details = details;
    }
}
