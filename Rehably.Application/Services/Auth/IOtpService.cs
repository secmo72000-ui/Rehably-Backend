using Rehably.Application.Common;
using Rehably.Application.DTOs.Auth;
using Rehably.Domain.Enums;

namespace Rehably.Application.Services.Auth;

public interface IOtpService
{
    /// <summary>
    /// Generate and send a new OTP for the specified purpose
    /// </summary>
    Task<Result<string>> GenerateOtpAsync(string contact, OtpPurpose purpose);

    /// <summary>
    /// Verify an OTP code for the specified purpose
    /// </summary>
    Task<Result<OtpVerifyResult>> VerifyOtpAsync(string contact, string code, OtpPurpose purpose);

    /// <summary>
    /// Resend OTP (generates new code, enforces cooldown)
    /// </summary>
    Task<Result> ResendOtpAsync(string contact, OtpPurpose purpose);

    /// <summary>
    /// Invalidate all previous OTPs for a contact and purpose
    /// </summary>
    Task<Result> InvalidatePreviousOtpsAsync(string contact, OtpPurpose purpose);
}
