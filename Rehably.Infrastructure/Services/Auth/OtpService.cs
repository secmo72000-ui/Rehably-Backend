using System.Security.Cryptography;
using System.Text;
using Hangfire;
using Microsoft.Extensions.Logging;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Auth;
using Rehably.Application.Services.Auth;
using Rehably.Application.Repositories;
using Rehably.Application.Services.Auth;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.BackgroundJobs;

namespace Rehably.Infrastructure.Services.Auth;

public class OtpService : IOtpService
{
    private const int MaxOtpAttempts = 3;
    private const int OtpCooldownSeconds = 60;

    private readonly IOtpCodeRepository _otpCodeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<OtpService> _logger;

    public OtpService(
        IOtpCodeRepository otpCodeRepository,
        IUnitOfWork unitOfWork,
        IBackgroundJobClient backgroundJobClient,
        ILogger<OtpService> logger)
    {
        _otpCodeRepository = otpCodeRepository;
        _unitOfWork = unitOfWork;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    public async Task<Result<string>> GenerateOtpAsync(string contact, OtpPurpose purpose)
    {
        await InvalidatePreviousOtpsAsync(contact, purpose);

        var code = RandomNumberGenerator.GetInt32(0, 1000000).ToString("D6");
        var expiry = OtpConfig.GetExpiry(purpose);

        var otp = new Domain.Entities.Identity.OtpCode
        {
            Contact = contact,
            Code = code,
            Purpose = purpose,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(expiry),
            AttemptCount = 0,
            IsUsed = false
        };

        await _otpCodeRepository.AddAsync(otp);
        await _unitOfWork.SaveChangesAsync();

        _backgroundJobClient.Enqueue<SendOtpJob>(job =>
            job.ExecuteAsync(contact, code, (int)expiry.TotalSeconds));

        return Result<string>.Success(code);
    }

    public async Task<Result<OtpVerifyResult>> VerifyOtpAsync(string contact, string code, OtpPurpose purpose)
    {
        var otp = await _otpCodeRepository.GetLatestUnusedAsync(contact, purpose);

        if (otp == null)
        {
            return Result<OtpVerifyResult>.Success(new OtpVerifyResult
            {
                IsValid = false,
                AttemptsRemaining = 0
            });
        }

        if (otp.ExpiresAt < DateTime.UtcNow)
        {
            return Result<OtpVerifyResult>.Success(new OtpVerifyResult
            {
                IsValid = false,
                AttemptsRemaining = 0
            });
        }

        if (otp.AttemptCount >= MaxOtpAttempts)
        {
            return Result<OtpVerifyResult>.Success(new OtpVerifyResult
            {
                IsValid = false,
                AttemptsRemaining = 0
            });
        }

        otp.AttemptCount++;

        if (!CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(otp.Code),
            Encoding.UTF8.GetBytes(code)))
        {
            await _unitOfWork.SaveChangesAsync();
            return Result<OtpVerifyResult>.Success(new OtpVerifyResult
            {
                IsValid = false,
                AttemptsRemaining = MaxOtpAttempts - otp.AttemptCount
            });
        }

        otp.IsUsed = true;
        otp.UsedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        return Result<OtpVerifyResult>.Success(new OtpVerifyResult
        {
            IsValid = true,
            AttemptsRemaining = 0
        });
    }

    public async Task<Result> ResendOtpAsync(string contact, OtpPurpose purpose)
    {
        var recentOtp = await _otpCodeRepository.GetLatestAsync(contact, purpose);

        if (recentOtp != null && recentOtp.CreatedAt > DateTime.UtcNow.AddSeconds(-OtpCooldownSeconds))
        {
            var waitSeconds = OtpCooldownSeconds - (int)(DateTime.UtcNow - recentOtp.CreatedAt).TotalSeconds;
            return Result.Failure($"Please wait {waitSeconds} seconds before requesting another code");
        }

        await GenerateOtpAsync(contact, purpose);
        return Result.Success();
    }

    public async Task<Result> InvalidatePreviousOtpsAsync(string contact, OtpPurpose purpose)
    {
        var previousOtps = await _otpCodeRepository.GetUnusedByContactAsync(contact, purpose);

        foreach (var otp in previousOtps)
        {
            otp.IsUsed = true;
            otp.UsedAt = DateTime.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    private static class OtpConfig
    {
        public static TimeSpan GetExpiry(OtpPurpose purpose) => purpose switch
        {
            OtpPurpose.Login => TimeSpan.FromSeconds(60),
            OtpPurpose.PasswordReset => TimeSpan.FromMinutes(5),
            OtpPurpose.EmailVerification => TimeSpan.FromMinutes(10),
            _ => TimeSpan.FromSeconds(60)
        };
    }
}
