using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Rehably.Application.DTOs.Auth;
using Rehably.Application.Services.Auth;
using Rehably.Domain.Enums;

namespace Rehably.API.Controllers.Auth;

/// <summary>
/// OTP-based authentication endpoints for passwordless login and password reset flows.
/// </summary>
[ApiController]
[Route("api/otp")]
[Produces("application/json")]
[Tags("Auth - OTP")]
public class OtpController : BaseController
{
    private readonly IOtpService _otpService;
    private readonly IAuthOtpService _authOtpService;
    private readonly IAuthPasswordService _authPasswordService;

    public OtpController(
        IOtpService otpService,
        IAuthOtpService authOtpService,
        IAuthPasswordService authPasswordService)
    {
        _otpService = otpService;
        _authOtpService = authOtpService;
        _authPasswordService = authPasswordService;
    }

    /// <summary>
    /// Request OTP for passwordless login.
    /// </summary>
    /// <param name="request">Email address to send the OTP to.</param>
    /// <returns>Confirmation that OTP was sent.</returns>
    /// <response code="200">OTP sent to the provided email.</response>
    /// <response code="400">Failed to send OTP.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-otp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RequestOtpLogin([FromBody] RequestOtpLoginDto request)
    {
        var result = await _authOtpService.RequestOtpLoginAsync(request.Email);
        return result.IsFailure
            ? Error(result.Error ?? "Failed to send OTP")
            : Success("OTP sent to your email");
    }

    /// <summary>
    /// Verify OTP and complete passwordless login.
    /// </summary>
    /// <param name="request">Email and OTP code to verify.</param>
    /// <returns>Access token and refresh token on success.</returns>
    /// <response code="200">Returns JWT access token and refresh token.</response>
    /// <response code="400">OTP is invalid or expired.</response>
    [HttpPost("verify-login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-otp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyOtpLogin([FromBody] VerifyOtpLoginDto request)
    {
        var result = await _authOtpService.VerifyOtpLoginAsync(request.Email, request.OtpCode);
        return result.IsFailure
            ? Error(result.Error)
            : Success(result.Data);
    }

    /// <summary>
    /// Request a new OTP for the specified purpose (login or password_reset).
    /// </summary>
    /// <param name="request">Email and purpose (login or password_reset).</param>
    /// <returns>Confirmation that code was sent if email exists.</returns>
    /// <response code="200">Code sent if the email exists in the system.</response>
    /// <response code="400">Invalid purpose value.</response>
    [HttpPost("request")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-otp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RequestOtp([FromBody] OtpRequestDto request)
    {
        var purpose = ParseOtpPurpose(request.Purpose);
        if (purpose == null)
            return ValidationError("Invalid purpose. Must be 'login' or 'password_reset'.");

        if (purpose == OtpPurpose.PasswordReset)
        {
            var locale = Request.Headers.AcceptLanguage.FirstOrDefault()?.Split(',').FirstOrDefault() ?? "en";
            await _authPasswordService.RequestOtpPasswordResetAsync(request.Email, locale);
        }
        else
        {
            await _otpService.GenerateOtpAsync(request.Email, purpose.Value);
        }

        return Success("If email exists, code was sent");
    }

    /// <summary>
    /// Verify an OTP code. For password_reset, returns a short-lived reset token.
    /// </summary>
    /// <param name="request">Email, OTP code, and purpose.</param>
    /// <returns>Verification confirmation, or a reset token for password_reset purpose.</returns>
    /// <response code="200">OTP verified. Returns reset token when purpose is password_reset.</response>
    /// <response code="400">OTP is invalid, expired, or purpose is unrecognised.</response>
    [HttpPost("verify")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-otp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyOtp([FromBody] OtpVerifyDto request)
    {
        var purpose = ParseOtpPurpose(request.Purpose);
        if (purpose == null)
            return ValidationError("Invalid purpose. Must be 'login' or 'password_reset'.");

        if (purpose == OtpPurpose.PasswordReset)
        {
            var result = await _authPasswordService.VerifyOtpPasswordResetAsync(request.Email, request.Code);
            if (result.IsFailure)
                return Error("INVALID_OTP", result.Error ?? "Invalid verification code");

            return Success(new OtpVerifyResponseDto
            {
                ResetToken = result.Value.ResetToken,
                ExpiresAt = result.Value.ExpiresAt,
                ExpiresInSeconds = result.Value.ExpiresInSeconds
            });
        }

        var verifyResult = await _otpService.VerifyOtpAsync(request.Email, request.Code, purpose.Value);

        if (!verifyResult.IsSuccess || !verifyResult.Value.IsValid)
        {
            return Error("INVALID_OTP", "Invalid verification code");
        }

        return Success("Code verified");
    }

    /// <summary>
    /// Resend OTP (invalidates previous code). Subject to 60-second cooldown.
    /// </summary>
    /// <param name="request">Email and purpose for the OTP to resend.</param>
    /// <returns>Confirmation that a new code was sent.</returns>
    /// <response code="200">New OTP sent; previous code invalidated.</response>
    /// <response code="400">Invalid purpose value.</response>
    /// <response code="429">Cooldown period not elapsed.</response>
    [HttpPost("resend")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-otp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ResendOtp([FromBody] OtpResendDto request)
    {
        var purpose = ParseOtpPurpose(request.Purpose);
        if (purpose == null)
            return ValidationError("Invalid purpose. Must be 'login' or 'password_reset'.");

        var result = await _otpService.ResendOtpAsync(request.Email, purpose.Value);
        return result.IsFailure
            ? Error("RATE_LIMITED", result.Error ?? "Too many requests", 429)
            : Success("New code sent. Previous code invalidated.");
    }

    private static OtpPurpose? ParseOtpPurpose(string purpose)
    {
        return purpose.ToLowerInvariant() switch
        {
            "password_reset" => OtpPurpose.PasswordReset,
            "login" => OtpPurpose.Login,
            _ => null
        };
    }
}
