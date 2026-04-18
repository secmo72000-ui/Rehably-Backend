namespace Rehably.Application.DTOs.Auth;

public record OtpVerifyResponseDto
{
    public string? ResetToken { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public int? ExpiresInSeconds { get; init; }
}
