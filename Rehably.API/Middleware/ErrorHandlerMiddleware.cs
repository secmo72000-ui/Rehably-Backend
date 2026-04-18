using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Rehably.Application.Common;

namespace Rehably.API.Middleware;

/// <summary>
/// Global error handling middleware that standardizes all exception responses.
/// </summary>
public class ErrorHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlerMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public ErrorHandlerMiddleware(RequestDelegate next, ILogger<ErrorHandlerMiddleware> logger, IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)GetStatusCode(exception);

        var isDevelopment = _env.IsDevelopment();
        var (code, message) = GetErrorInfo(exception);

        var details = new List<string>();
        if (isDevelopment)
        {
            details.Add($"Exception: {exception.GetType().Name}");
            details.Add($"Message: {exception.Message}");
            if (exception.StackTrace != null)
            {
                details.Add($"StackTrace: {exception.StackTrace}");
            }
        }

        var response = new ApiResponse
        {
            Success = false,
            Error = new ApiError(code, message, details.Count > 0 ? details : null)
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }

    private static HttpStatusCode GetStatusCode(Exception exception)
    {
        return exception switch
        {
            ArgumentNullException => HttpStatusCode.BadRequest,
            ArgumentException => HttpStatusCode.BadRequest,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            NotImplementedException => HttpStatusCode.NotImplemented,
            InvalidOperationException => HttpStatusCode.BadRequest,
            TimeoutException => HttpStatusCode.RequestTimeout,
            _ => HttpStatusCode.InternalServerError
        };
    }

    private static (string Code, string Message) GetErrorInfo(Exception exception)
    {
        return exception switch
        {
            ArgumentNullException => (ErrorCodes.InvalidInput, "Required parameter was not provided"),
            ArgumentException => (ErrorCodes.ValidationFailed, "Invalid request parameters"),
            UnauthorizedAccessException => (ErrorCodes.Unauthorized, "You are not authorized to perform this action"),
            NotImplementedException => (ErrorCodes.InternalError, "This feature is not yet implemented"),
            InvalidOperationException => (ErrorCodes.InvalidOperation, "This operation is not valid in the current state"),
            TimeoutException => (ErrorCodes.ServiceUnavailable, "The request timed out"),
            _ => (ErrorCodes.InternalError, "An error occurred while processing your request")
        };
    }
}
