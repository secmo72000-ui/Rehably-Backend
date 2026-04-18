using Rehably.Domain.Enums;

namespace Rehably.Domain.Entities.Identity;

public class OtpCode
{
    public Guid Id { get; set; }
    public string Contact { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public OtpPurpose Purpose { get; set; } = OtpPurpose.Login;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public int AttemptCount { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
}
