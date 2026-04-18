namespace Rehably.Application.DTOs.Auth;

public record PasswordResetTokenResult
{
    public string ResetToken { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public int ExpiresInSeconds => (int)(ExpiresAt - DateTime.UtcNow).TotalSeconds;
}
