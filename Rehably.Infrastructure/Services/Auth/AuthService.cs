using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Rehably.Application.Common;
using Rehably.Application.Services;
using Rehably.Application.Services.Auth;
using Rehably.Application.Services.Communication;
using Rehably.Application.DTOs.Auth;
using Rehably.Application.DTOs.Communication;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Identity;
using Rehably.Infrastructure.BackgroundJobs;

namespace Rehably.Infrastructure.Services.Auth;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IConfiguration _configuration;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IEmailService emailService,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        IBackgroundJobClient backgroundJobClient,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _emailService = emailService;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _backgroundJobClient = backgroundJobClient;
        _configuration = configuration;
    }

    public async Task<Result<LoginResponseDto>> RefreshTokenAsync(string refreshToken)
    {
        var existingToken = await _refreshTokenRepository.GetValidTokenWithUserAsync(refreshToken);

        if (existingToken == null)
        {
            return Result<LoginResponseDto>.Failure("Invalid or expired refresh token");
        }

        var user = existingToken.User;
        if (!user.IsActive)
        {
            return Result<LoginResponseDto>.Failure("Account is disabled");
        }

        await _refreshTokenRepository.DeleteAsync(existingToken);

        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await _tokenService.GetPermissionsForRolesAsync(roles);

        var newAccessToken = await _tokenService.GenerateAccessTokenAsync(user.Id, user.TenantId, user.ClinicId, roles, permissions, mustChangePassword: user.MustChangePassword);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        await SaveRefreshTokenAsync(user.Id, newRefreshToken);
        await _unitOfWork.SaveChangesAsync();

        var jwtSettings = _configuration.GetSection("JwtSettings");
        var accessMinutes = double.Parse(jwtSettings["AccessTokenExpirationMinutes"] ?? "1440");
        var mustChangeMinutes = double.Parse(jwtSettings["MustChangePasswordTokenMinutes"] ?? "5");

        var response = new LoginResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = user.MustChangePassword ? DateTime.UtcNow.AddMinutes(mustChangeMinutes) : DateTime.UtcNow.AddMinutes(accessMinutes),
            MustChangePassword = user.MustChangePassword,
            EmailVerified = user.EmailVerified
        };

        return Result<LoginResponseDto>.Success(response);
    }

    public Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken)
    {
        return _tokenService.ValidateRefreshTokenAsync(userId, refreshToken);
    }

    public Task SaveRefreshTokenAsync(string userId, string refreshToken)
    {
        return _tokenService.SaveRefreshTokenAsync(userId, refreshToken);
    }

    public async Task<Result<LoginResponseDto>> LoginAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return Result<LoginResponseDto>.Failure("Invalid email or password");
        }

        if (!user.IsActive)
        {
            return Result<LoginResponseDto>.Failure("Account is disabled");
        }

        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
        {
            var remainingTime = (int)(user.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes;
            return Result<LoginResponseDto>.Failure($"Account is locked. Try again in {remainingTime} minutes");
        }

        var result = await _userManager.CheckPasswordAsync(user, password);
        if (!result)
        {
            user.AccessFailedCount++;

            if (user.AccessFailedCount >= 5)
            {
                user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(15);
                await _userManager.UpdateAsync(user);
                await SendAccountLockedEmailAsync(user.Email ?? string.Empty, user.LockoutEnd.Value.DateTime);
                return Result<LoginResponseDto>.Failure("Account locked due to too many failed attempts. Try again in 15 minutes");
            }

            await _userManager.UpdateAsync(user);
            var remainingAttempts = 5 - user.AccessFailedCount;
            return Result<LoginResponseDto>.Failure($"Invalid email or password. {remainingAttempts} attempts remaining");
        }

        var roles = await _userManager.GetRolesAsync(user);

        if (!user.EmailVerified)
        {
            return Result<LoginResponseDto>.Failure("Email verification required. Please verify your email to continue.");
        }

        var permissions = await _tokenService.GetPermissionsForRolesAsync(roles);

        var loginJwtSettings = _configuration.GetSection("JwtSettings");
        var loginAccessMinutes = double.Parse(loginJwtSettings["AccessTokenExpirationMinutes"] ?? "1440");
        var loginMustChangeMinutes = double.Parse(loginJwtSettings["MustChangePasswordTokenMinutes"] ?? "5");

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
                ExpiresAt = DateTime.UtcNow.AddMinutes(loginMustChangeMinutes),
                MustChangePassword = true,
                EmailVerified = user.EmailVerified
            });
        }

        var normalToken = await _tokenService.GenerateAccessTokenAsync(user.Id, user.TenantId, user.ClinicId, roles, permissions, mustChangePassword: false);
        var refreshToken = _tokenService.GenerateRefreshToken();
        await SaveRefreshTokenAsync(user.Id, refreshToken);

        user.LastLoginAt = DateTime.UtcNow;
        user.AccessFailedCount = 0;
        user.LockoutEnd = null;
        await _userManager.UpdateAsync(user);

        return Result<LoginResponseDto>.Success(new LoginResponseDto
        {
            AccessToken = normalToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(loginAccessMinutes),
            MustChangePassword = false,
            EmailVerified = true
        });
    }

    public async Task<Result> LogoutAsync(string userId)
    {
        await _unitOfWork.BeginTransactionAsync();

        try
        {
            var activeTokens = await _refreshTokenRepository.GetActiveByUserIdAsync(userId);
            await _refreshTokenRepository.DeleteRangeAsync(activeTokens);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return Result.Success();
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure("Logout failed");
        }
    }

    public async Task<string> GenerateEmailVerificationTokenAsync(string userId, string email)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) throw new InvalidOperationException("User not found");

        return await _userManager.GenerateEmailConfirmationTokenAsync(user);
    }

    public async Task<Result> VerifyEmailAsync(string userId, string token)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result.Failure("User not found");
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            return Result.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        user.EmailVerified = true;
        await _userManager.UpdateAsync(user);

        return Result.Success();
    }

    public async Task SendVerificationEmailAsync(string email, string token)
    {
        var message = new EmailMessage
        {
            To = email,
            Subject = "Verify your email address",
            Body = $"Please verify your email by using this token: {token}",
            IsHtml = false
        };

        await _emailService.SendWithDefaultProviderAsync(message);
    }

    public async Task SendAccountLockedEmailAsync(string email, DateTime lockoutEnd)
    {
        var message = new EmailMessage
        {
            To = email,
            Subject = "Your Rehably account has been locked",
            Body = $"Your account has been locked due to 5 failed login attempts.\n\nLock duration: 15 minutes\nLocked at: {DateTime.UtcNow}\n\nIf you didn't attempt to login, please contact support immediately.\n\nAfter 15 minutes, you can try logging in again.",
            IsHtml = false
        };

        await _emailService.SendWithDefaultProviderAsync(message);
    }

    public Task SendWelcomeEmailAsync(string email, string token, string clinicName, string userName)
    {
        _backgroundJobClient.Enqueue<SendWelcomeEmailJob>(job =>
            job.ExecuteAsync(email, token, clinicName, userName));

        return Task.CompletedTask;
    }

    public async Task<Result> UnlockUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result.Failure("User not found");
        }

        if (user.LockoutEnd == null || user.LockoutEnd <= DateTime.UtcNow)
        {
            return Result.Failure("Account is not currently locked");
        }

        user.LockoutEnd = null;
        user.AccessFailedCount = 0;
        await _userManager.UpdateAsync(user);

        var message = new EmailMessage
        {
            To = user.Email ?? string.Empty,
            Subject = "Your Rehably account has been unlocked",
            Body = $"Your account has been unlocked by a platform administrator.\n\nYou can now login with your existing credentials.\n\nIf you did not request this unlock, please contact support immediately.",
            IsHtml = false
        };

        await _emailService.SendWithDefaultProviderAsync(message);

        return Result.Success();
    }

    public async Task<UserDto?> GetCurrentUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return null;
        }

        var roles = (await _userManager.GetRolesAsync(user)).ToList();

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            MustChangePassword = user.MustChangePassword,
            EmailVerified = user.EmailVerified,
            TenantId = user.TenantId,
            ClinicId = user.ClinicId,
            Roles = roles,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            AccessFailedCount = user.AccessFailedCount,
            LockoutEnd = user.LockoutEnd?.DateTime
        };
    }
}
