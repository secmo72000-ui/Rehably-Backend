using Rehably.Application.Common;
using Rehably.Application.DTOs.Auth;

namespace Rehably.Application.Services.Auth;

public interface IAuthService
{
    Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken);
    Task SaveRefreshTokenAsync(string userId, string refreshToken);
    Task<Result<LoginResponseDto>> RefreshTokenAsync(string refreshToken);
    Task<Result<LoginResponseDto>> LoginAsync(string email, string password);
    Task<Result> LogoutAsync(string userId);
    Task<UserDto?> GetCurrentUserAsync(string userId);
    Task<string> GenerateEmailVerificationTokenAsync(string userId, string email);
    Task<Result> VerifyEmailAsync(string userId, string token);
    Task SendVerificationEmailAsync(string email, string token);
    Task SendAccountLockedEmailAsync(string email, DateTime lockoutEnd);
    Task SendWelcomeEmailAsync(string email, string token, string clinicName, string userName);
    Task<Result> UnlockUserAsync(string userId);
}
