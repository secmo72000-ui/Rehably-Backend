using Rehably.Application.Common;
using Rehably.Application.DTOs.Auth;

namespace Rehably.Application.Services.Auth;

public interface IAuthPasswordService
{
    Task<string> GeneratePasswordResetTokenAsync(string email);
    Task<Result> ResetPasswordAsync(string token, string newPassword);
    Task<Result> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    Task<bool> MustChangePasswordAsync(string userId);
    Task<Result> RequestOtpPasswordResetAsync(string email, string? locale = "en");
    Task<Result<PasswordResetTokenResult>> VerifyOtpPasswordResetAsync(string email, string code);
    Task<Result> ResetPasswordWithTokenAsync(string resetToken, string newPassword);
}
