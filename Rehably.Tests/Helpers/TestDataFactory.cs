using Rehably.Domain.Entities.Identity;

namespace Rehably.Tests.Helpers;

public static class TestDataFactory
{
    public static ApplicationUser CreateTestUser(
        string id = "test-user-id",
        string email = "test@example.com",
        bool mustChangePassword = false,
        bool isActive = true,
        bool emailVerified = true,
        int accessFailedCount = 0,
        DateTime? lockoutEnd = null)
    {
        return new ApplicationUser
        {
            Id = id,
            Email = email,
            UserName = email,
            NormalizedEmail = email.ToUpper(),
            MustChangePassword = mustChangePassword,
            EmailVerified = emailVerified,
            IsActive = isActive,
            AccessFailedCount = accessFailedCount,
            LockoutEnd = lockoutEnd,
            TenantId = Guid.NewGuid(),
            ClinicId = null,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };
    }

    public static RefreshToken CreateRefreshToken(
        string userId,
        string token = "test-refresh-token",
        bool isRevoked = false,
        DateTime? revokedAt = null,
        int expiresDays = 7)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            IsRevoked = isRevoked,
            RevokedAt = revokedAt,
            ExpiresAt = DateTime.UtcNow.AddDays(expiresDays),
            CreatedAt = DateTime.UtcNow
        };
    }
}
