using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Auth;
using Rehably.Application.DTOs.Communication;
using Rehably.Application.Repositories;
using Rehably.Application.Services;
using Rehably.Application.Services.Auth;
using Rehably.Application.Services.Communication;
using Rehably.Domain.Entities.Identity;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Services.Communication.Templates;
using System.Security.Cryptography;
using System.Text;

namespace Rehably.Infrastructure.Services.Auth;

public class AuthPasswordService : IAuthPasswordService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IOtpService _otpService;
    private readonly IUserRepository _userRepository;
    private readonly IOtpPasswordResetTokenRepository _otpPasswordResetTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public AuthPasswordService(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IOtpService otpService,
        IUserRepository userRepository,
        IOtpPasswordResetTokenRepository otpPasswordResetTokenRepository,
        IUnitOfWork unitOfWork,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _emailService = emailService;
        _otpService = otpService;
        _userRepository = userRepository;
        _otpPasswordResetTokenRepository = otpPasswordResetTokenRepository;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    public async Task<string> GeneratePasswordResetTokenAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null) return string.Empty;

        var selector = GenerateRandomToken(16);
        var token = GenerateRandomToken(32);
        var tokenHash = HashToken(token);

        var expiryHours = int.Parse(_configuration["JwtSettings:WelcomeTokenExpirationHours"] ?? "24");

        user.ResetTokenSelector = selector;
        user.ResetTokenHash = tokenHash;
        user.ResetTokenExpiry = DateTime.UtcNow.AddHours(expiryHours);
        await _userManager.UpdateAsync(user);

        return $"{selector}.{token}";
    }

    public async Task<Result> ResetPasswordAsync(string resetToken, string newPassword)
    {
        var parts = resetToken.Split('.');
        if (parts.Length != 2)
        {
            return Result.Failure("Invalid token format");
        }

        var selector = parts[0];
        var token = parts[1];

        var user = await _userRepository.GetByResetTokenSelectorAsync(selector);

        if (user == null)
        {
            return Result.Failure("Invalid or expired token");
        }

        if (!VerifyTokenHash(token, user.ResetTokenHash))
        {
            return Result.Failure("Invalid token");
        }

        var result = await _userManager.ResetPasswordAsync(user, await _userManager.GeneratePasswordResetTokenAsync(user), newPassword);
        if (!result.Succeeded)
        {
            return Result.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        user.ResetTokenSelector = null;
        user.ResetTokenHash = null;
        user.ResetTokenExpiry = null;
        user.MustChangePassword = false;
        await _userManager.UpdateAsync(user);

        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result.Failure("User not found");
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
        {
            return Result.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        user.MustChangePassword = false;
        await _userManager.UpdateAsync(user);

        return Result.Success();
    }

    public async Task<bool> MustChangePasswordAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user?.MustChangePassword ?? false;
    }

    public async Task<Result> RequestOtpPasswordResetAsync(string email, string? locale = "en")
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user != null && user.IsActive)
        {
            var otpResult = await _otpService.GenerateOtpAsync(email, OtpPurpose.PasswordReset);

            if (otpResult.IsSuccess)
            {
                var template = AuthEmailTemplates.PasswordResetOtp(otpResult.Value, 5, locale ?? "en");
                await _emailService.SendWithDefaultProviderAsync(new EmailMessage
                {
                    To = email,
                    Subject = locale == "ar" ? "رمز إعادة تعيين كلمة المرور" : "Password Reset Code",
                    Body = template,
                    IsHtml = true
                });
            }
        }

        return Result.Success();
    }

    public async Task<Result<PasswordResetTokenResult>> VerifyOtpPasswordResetAsync(string email, string code)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return Result<PasswordResetTokenResult>.Failure("Invalid or expired code");
        }

        var verifyResult = await _otpService.VerifyOtpAsync(email, code, OtpPurpose.PasswordReset);

        if (!verifyResult.IsSuccess || verifyResult.Value is null || !verifyResult.Value.IsValid)
        {
            if (verifyResult.Value?.AttemptsRemaining > 0)
            {
                return Result<PasswordResetTokenResult>.Failure(
                    $"Invalid code. {verifyResult.Value.AttemptsRemaining} attempts remaining");
            }
            return Result<PasswordResetTokenResult>.Failure("Invalid or expired code");
        }

        var resetToken = GenerateSecureToken();
        var tokenHash = ComputeSha256Hash(resetToken);
        var expiryMinutes = int.Parse(_configuration["JwtSettings:OtpResetTokenExpirationMinutes"] ?? "10");
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var tokenEntity = new OtpPasswordResetToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            IsUsed = false
        };

        await _otpPasswordResetTokenRepository.AddAsync(tokenEntity);
        await _unitOfWork.SaveChangesAsync();

        return Result<PasswordResetTokenResult>.Success(new PasswordResetTokenResult
        {
            ResetToken = resetToken,
            ExpiresAt = expiresAt
        });
    }

    public async Task<Result> ResetPasswordWithTokenAsync(string resetToken, string newPassword)
    {
        var tokenHash = ComputeSha256Hash(resetToken);

        var tokenEntity = await _otpPasswordResetTokenRepository.GetByTokenHashWithUserAsync(tokenHash);

        if (tokenEntity == null)
        {
            return Result.Failure("Invalid reset token");
        }

        if (tokenEntity.IsUsed)
        {
            return Result.Failure("Reset token has already been used");
        }

        if (tokenEntity.ExpiresAt < DateTime.UtcNow)
        {
            return Result.Failure("Reset token has expired");
        }

        var user = tokenEntity.User;
        var resetPasswordToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await _userManager.ResetPasswordAsync(user, resetPasswordToken, newPassword);

        if (!resetResult.Succeeded)
        {
            var errors = string.Join(", ", resetResult.Errors.Select(e => e.Description));
            return Result.Failure($"Password reset failed: {errors}");
        }

        tokenEntity.IsUsed = true;
        tokenEntity.UsedAt = DateTime.UtcNow;

        user.AccessFailedCount = 0;
        user.LockoutEnd = null;

        await _unitOfWork.SaveChangesAsync();

        await _emailService.SendWithDefaultProviderAsync(new EmailMessage
        {
            To = user.Email!,
            Subject = "Password Changed Successfully",
            Body = AuthEmailTemplates.PasswordChanged(user.Email!, DateTime.UtcNow),
            IsHtml = true
        });

        return Result.Success();
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string ComputeSha256Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private static bool VerifyTokenHash(string token, string? hash)
    {
        if (string.IsNullOrEmpty(hash)) return false;
        var computedHash = HashToken(token);

        var hashBytes = Encoding.UTF8.GetBytes(hash);
        var computedBytes = Encoding.UTF8.GetBytes(computedHash);
        return CryptographicOperations.FixedTimeEquals(hashBytes, computedBytes);
    }

    private static string GenerateRandomToken(int byteLength)
    {
        var bytes = new byte[byteLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "")
            .Substring(0, byteLength);
    }
}
