namespace Rehably.Application.DTOs.Auth;

public record ResetPasswordRequestDto
{
    public required string Token { get; init; }
    public required string NewPassword { get; init; }
}
