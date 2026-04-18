using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.BackgroundJobs;

public class RefreshTokenCleanupJob
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RefreshTokenCleanupJob> _logger;

    public RefreshTokenCleanupJob(ApplicationDbContext context, ILogger<RefreshTokenCleanupJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-7);

        var revokedTokens = await _context.RefreshTokens
            .Where(t => t.IsRevoked && t.RevokedAt < cutoffDate)
            .ToListAsync();

        var expiredTokens = await _context.RefreshTokens
            .Where(t => t.ExpiresAt < cutoffDate && !t.IsRevoked)
            .ToListAsync();

        if (revokedTokens.Count > 0 || expiredTokens.Count > 0)
        {
            _context.RefreshTokens.RemoveRange(revokedTokens);
            _context.RefreshTokens.RemoveRange(expiredTokens);
            await _context.SaveChangesAsync();

            _logger.LogInformation("RefreshTokenCleanupJob: Cleaned up {TotalCount} tokens (Revoked: {RevokedCount}, Expired: {ExpiredCount})",
                revokedTokens.Count + expiredTokens.Count, revokedTokens.Count, expiredTokens.Count);
        }
    }
}
