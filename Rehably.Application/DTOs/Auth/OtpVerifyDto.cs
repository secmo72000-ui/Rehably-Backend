namespace Rehably.Application.DTOs.Auth;

public record OtpVerifyDto
{
    public string Email { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string Purpose { get; init; } = string.Empty;
}
