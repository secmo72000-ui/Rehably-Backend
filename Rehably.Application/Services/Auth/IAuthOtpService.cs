using Rehably.Application.Common;
using Rehably.Application.DTOs.Auth;

namespace Rehably.Application.Services.Auth;

public interface IAuthOtpService
{
    Task<Result> RequestOtpLoginAsync(string email);
    Task<Result<LoginResponseDto>> VerifyOtpLoginAsync(string email, string otpCode);
}
