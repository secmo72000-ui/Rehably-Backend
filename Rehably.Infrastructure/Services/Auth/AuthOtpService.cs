using Microsoft.AspNetCore.Identity;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Auth;
using Rehably.Application.Services;
using Rehably.Application.Services.Auth;
using Rehably.Domain.Entities.Identity;
using Rehably.Domain.Enums;

namespace Rehably.Infrastructure.Services.Auth;

public class AuthOtpService : IAuthOtpService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IOtpService _otpService;

    public AuthOtpService(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IOtpService otpService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _otpService = otpService;
    }

    public async Task<Result> RequestOtpLoginAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return Result.Failure("User not found");
        }

        if (!user.IsActive)
        {
            return Result.Failure("Account is disabled");
        }

        var result = await _otpService.GenerateOtpAsync(email, OtpPurpose.Login);
        if (!result.IsSuccess)
        {
            return Result.Failure(result.Error ?? "Failed to send OTP");
        }

        return Result.Success();
    }

    public async Task<Result<LoginResponseDto>> VerifyOtpLoginAsync(string email, string otpCode)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return Result<LoginResponseDto>.Failure("User not found");
        }

        if (!user.IsActive)
        {
            return Result<LoginResponseDto>.Failure("Account is disabled");
        }

        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
            return Result<LoginResponseDto>.Failure("Account is locked. Please try again later.");

        var otpResult = await _otpService.VerifyOtpAsync(email, otpCode, OtpPurpose.Login);
        if (!otpResult.IsSuccess || !otpResult.Value!.IsValid)
        {
            var errorMessage = otpResult.Value?.AttemptsRemaining > 0
                ? $"Invalid OTP. {otpResult.Value.AttemptsRemaining} attempts remaining"
                : "Invalid or expired OTP";
            return Result<LoginResponseDto>.Failure(errorMessage);
        }

        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
        }

        var roles = await _userManager.GetRolesAsync(user);

        var permissions = await _tokenService.GetPermissionsForRolesAsync(roles);

        if (user.MustChangePassword)
        {
            var token = await _tokenService.GenerateAccessTokenAsync(user.Id, user.TenantId, user.ClinicId, roles, permissions, mustChangePassword: true);

            user.LastLoginAt = DateTime.UtcNow;
            user.AccessFailedCount = 0;
            user.LockoutEnd = null;
            await _userManager.UpdateAsync(user);

            return Result<LoginResponseDto>.Success(new LoginResponseDto
            {
                AccessToken = token,
                RefreshToken = null,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                MustChangePassword = true,
                EmailVerified = user.EmailVerified
            });
        }

        var accessToken = await _tokenService.GenerateAccessTokenAsync(user.Id, user.TenantId, user.ClinicId, roles, permissions, mustChangePassword: false);
        var refreshToken = _tokenService.GenerateRefreshToken();
        await _tokenService.SaveRefreshTokenAsync(user.Id, refreshToken);

        user.LastLoginAt = DateTime.UtcNow;
        user.AccessFailedCount = 0;
        user.LockoutEnd = null;
        await _userManager.UpdateAsync(user);

        return Result<LoginResponseDto>.Success(new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            MustChangePassword = false,
            EmailVerified = user.EmailVerified
        });
    }
}
