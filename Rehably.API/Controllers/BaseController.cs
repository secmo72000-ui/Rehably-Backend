using Microsoft.AspNetCore.Mvc;
using Rehably.Application.Common;
using System.Security.Claims;

namespace Rehably.API.Controllers;

[ApiController]
public abstract class BaseController : ControllerBase
{
    protected string? UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    protected Guid CurrentAdminId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    protected Guid? TenantId
    {
        get
        {
            var tenantIdClaim = User.FindFirst("TenantId")?.Value;
            return Guid.TryParse(tenantIdClaim, out var tenantId) ? tenantId : null;
        }
    }

    #region Success Responses

    protected ActionResult Success<T>(T data, string? message = null)
    {
        return Ok(ApiResponse<T>.SuccessResponse(data, message));
    }

    protected ActionResult Success(string? message = null)
    {
        return Ok(ApiResponse.SuccessResponse(message));
    }

    protected ActionResult Created<T>(T data, string? message = null)
    {
        return StatusCode(201, ApiResponse<T>.SuccessResponse(data, message));
    }

    protected ActionResult NoContentResult(string? message = null)
    {
        return NoContent();
    }

    #endregion

    #region Error Responses

    /// <summary>
    /// Returns a 400 Bad Request with validation error details.
    /// </summary>
    protected ActionResult ValidationError(string message, List<string>? details = null)
        => BadRequest(ApiResponse.FailureResponse(ErrorCodes.ValidationFailed, message, details));

    /// <summary>
    /// Returns a 400 Bad Request with invalid input error.
    /// </summary>
    protected ActionResult InvalidInputError(string message, List<string>? details = null)
        => BadRequest(ApiResponse.FailureResponse(ErrorCodes.InvalidInput, message, details));

    /// <summary>
    /// Returns a 401 Unauthorized response.
    /// </summary>
    protected ActionResult UnauthorizedError(string message = "Unauthorized access")
        => Unauthorized(ApiResponse.FailureResponse(ErrorCodes.Unauthorized, message));

    /// <summary>
    /// Returns a 403 Forbidden response.
    /// </summary>
    protected ActionResult ForbiddenError(string message = "Access denied")
        => StatusCode(403, ApiResponse.FailureResponse(ErrorCodes.Forbidden, message));

    /// <summary>
    /// Returns a 404 Not Found response.
    /// </summary>
    protected ActionResult NotFoundError(string message = "Resource not found", string? code = null)
        => NotFound(ApiResponse.FailureResponse(code ?? ErrorCodes.NotFound, message));

    /// <summary>
    /// Returns a 409 Conflict response.
    /// </summary>
    protected ActionResult ConflictError(string message, string? code = null)
        => Conflict(ApiResponse.FailureResponse(code ?? ErrorCodes.Conflict, message));

    /// <summary>
    /// Returns a 422 Unprocessable Entity response for business rule violations.
    /// </summary>
    protected ActionResult BusinessRuleError(string message, List<string>? details = null)
        => UnprocessableEntity(ApiResponse.FailureResponse(ErrorCodes.BusinessRuleViolation, message, details));

    /// <summary>
    /// Returns a 500 Internal Server Error response.
    /// </summary>
    protected ActionResult InternalError(string message = "An internal error occurred")
        => StatusCode(500, ApiResponse.FailureResponse(ErrorCodes.InternalError, message));

    /// <summary>
    /// Generic error response with custom status code (standard format).
    /// </summary>
    protected ActionResult Error(string code, string message, int statusCode = 400, List<string>? details = null)
        => StatusCode(statusCode, ApiResponse.FailureResponse(code, message, details));

    /// <summary>
    /// Generic error response with just a message (backward compatibility).
    /// </summary>
    protected ActionResult Error(string message, int statusCode = 400)
        => StatusCode(statusCode, ApiResponse.FailureResponse(message));

    #endregion

    #region Result Mapping

    /// <summary>
    /// Maps a Result{T} to the appropriate ActionResult based on success/failure.
    /// </summary>
    protected ActionResult<T> FromResult<T>(Result<T> result, int successStatusCode = 200)
    {
        if (result.IsSuccess)
        {
            return successStatusCode switch
            {
                201 => StatusCode(201, ApiResponse<T>.SuccessResponse(result.Value)),
                204 => NoContent(),
                _ => Ok(ApiResponse<T>.SuccessResponse(result.Value))
            };
        }

        var error = result.Error ?? "An error occurred";
        var (code, statusCode) = MapErrorToCodeAndStatus(error);
        return StatusCode(statusCode, ApiResponse<T>.FailureResponse(code, error));
    }

    /// <summary>
    /// Maps a Result to the appropriate ActionResult based on success/failure.
    /// </summary>
    protected ActionResult FromResult(Result result, int successStatusCode = 200)
    {
        if (result.IsSuccess)
        {
            return successStatusCode switch
            {
                201 => StatusCode(201, ApiResponse.SuccessResponse()),
                204 => NoContent(),
                _ => Ok(ApiResponse.SuccessResponse())
            };
        }

        var error = result.Error ?? "An error occurred";
        var (code, statusCode) = MapErrorToCodeAndStatus(error);
        return StatusCode(statusCode, ApiResponse.FailureResponse(code, error));
    }

    private static (string Code, int StatusCode) MapErrorToCodeAndStatus(string error)
    {
        var lowerError = error.ToLowerInvariant();

        if (lowerError.Contains("not found"))
            return (ErrorCodes.NotFound, 404);

        if (lowerError.Contains("unauthorized") || lowerError.Contains("authentication"))
            return (ErrorCodes.Unauthorized, 401);

        if (lowerError.Contains("forbidden") || lowerError.Contains("permission"))
            return (ErrorCodes.Forbidden, 403);

        if (lowerError.Contains("conflict") || lowerError.Contains("already exists") || lowerError.Contains("duplicate"))
            return (ErrorCodes.Conflict, 409);

        if (lowerError.Contains("validation") || lowerError.Contains("invalid"))
            return (ErrorCodes.ValidationFailed, 400);

        if (lowerError.Contains("cannot") || lowerError.Contains("failed to"))
            return (ErrorCodes.BusinessRuleViolation, 400);

        return (ErrorCodes.InternalError, 500);
    }

    #endregion
}
