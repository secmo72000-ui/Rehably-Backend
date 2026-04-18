namespace Rehably.Application.DTOs.Auth;

public record VerifyOtpLoginDto
{
    public string Email { get; init; } = string.Empty;
    public string OtpCode { get; init; } = string.Empty;
}
