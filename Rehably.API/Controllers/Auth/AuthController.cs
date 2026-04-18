using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Rehably.Application.Services.Auth;
using Rehably.Application.DTOs.Auth;

namespace Rehably.API.Controllers.Auth;

/// <summary>
/// Authentication endpoints for login, logout, token refresh, and password management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Auth - Login")]
public class AuthController : BaseController
{
    private readonly IAuthService _authService;
    private readonly IAuthPasswordService _authPasswordService;

    public AuthController(
        IAuthService authService,
        IAuthPasswordService authPasswordService)
    {
        _authService = authService;
        _authPasswordService = authPasswordService;
    }

    /// <summary>
    /// Authenticate with email and password.
    /// </summary>
    /// <param name="request">Login credentials.</param>
    /// <returns>Access token and refresh token on success.</returns>
    /// <response code="200">Returns JWT access token and refresh token.</response>
    /// <response code="400">Invalid credentials or request payload.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var result = await _authService.LoginAsync(request.Email, request.Password);
        return result.IsFailure
            ? Error(result.Error)
            : Success(result.Data);
    }

    /// <summary>
    /// Revoke all refresh tokens for the current user.
    /// </summary>
    /// <returns>Confirmation message on success.</returns>
    /// <response code="200">User logged out and tokens revoked.</response>
    /// <response code="401">User is not authenticated.</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        if (UserId == null)
            return UnauthorizedError();

        var result = await _authService.LogoutAsync(UserId);
        return result.IsFailure
            ? Error(result.Error ?? "Logout failed")
            : Success("Logged out successfully");
    }

    /// <summary>
    /// Exchange a valid refresh token for new access and refresh tokens.
    /// </summary>
    /// <param name="request">The refresh token to exchange.</param>
    /// <returns>New access token and refresh token pair.</returns>
    /// <response code="200">Returns new token pair.</response>
    /// <response code="401">Refresh token is invalid or expired.</response>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto request)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken);
        return result.IsFailure
            ? UnauthorizedError(result.Error ?? "Refresh failed")
            : Success(result.Value);
    }

    /// <summary>
    /// Change password for the currently authenticated user.
    /// </summary>
    /// <param name="request">Current password and new password.</param>
    /// <returns>Confirmation message on success.</returns>
    /// <response code="200">Password changed successfully.</response>
    /// <response code="400">Current password is incorrect or new password fails validation.</response>
    /// <response code="401">User is not authenticated.</response>
    [HttpPost("change-password")]
    [Authorize]
    [EnableRateLimiting("auth-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
    {
        if (UserId == null)
            return UnauthorizedError();

        var result = await _authPasswordService.ChangePasswordAsync(UserId, request.CurrentPassword, request.NewPassword);
        return result.IsFailure
            ? Error(result.Error ?? "Password change failed")
            : Success("Password changed successfully");
    }

    /// <summary>
    /// Get the current authenticated user's profile.
    /// </summary>
    /// <returns>Current user profile details.</returns>
    /// <response code="200">Returns user profile.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">User record not found.</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentUser()
    {
        if (UserId == null)
            return UnauthorizedError();

        var user = await _authService.GetCurrentUserAsync(UserId);
        return user == null
            ? NotFoundError("User not found")
            : Success(user);
    }

    /// <summary>
    /// Reset password using an OTP-verified reset token.
    /// </summary>
    /// <param name="request">OTP reset token and new password.</param>
    /// <returns>Confirmation message on success.</returns>
    /// <response code="200">Password reset successfully.</response>
    /// <response code="400">Reset token is invalid or expired.</response>
    [HttpPost("password/reset")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-otp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPasswordWithToken([FromBody] PasswordResetWithTokenDto request)
    {
        var result = await _authPasswordService.ResetPasswordWithTokenAsync(request.ResetToken, request.NewPassword);
        if (result.IsFailure)
        {
            var errorCode = result.Error?.Contains("expired") == true ? "TOKEN_EXPIRED" : "INVALID_TOKEN";
            return Error(errorCode, result.Error ?? "Password reset failed");
        }

        return Success("Password reset successfully");
    }
}
