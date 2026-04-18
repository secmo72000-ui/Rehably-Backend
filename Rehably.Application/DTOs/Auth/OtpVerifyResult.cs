namespace Rehably.Application.DTOs.Auth;

public record OtpVerifyResult
{
    public bool IsValid { get; init; }
    public int AttemptsRemaining { get; init; }
    public string? UserId { get; init; }
}
