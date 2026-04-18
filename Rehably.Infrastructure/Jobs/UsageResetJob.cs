using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rehably.Application.Interfaces;
using Rehably.Application.Repositories;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Jobs;

public class UsageResetJob
{
    private readonly ApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<UsageResetJob> _logger;

    public UsageResetJob(
        ApplicationDbContext context,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<UsageResetJob> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ResetUsageCounters(Guid subscriptionId)
    {
        var now = _clock.UtcNow;

        var usages = await _context.SubscriptionFeatureUsages
            .Where(u => u.SubscriptionId == subscriptionId)
            .ToListAsync();

        if (usages.Count == 0)
        {
            _logger.LogInformation("UsageResetJob: No feature usages found for subscription {SubscriptionId}", subscriptionId);
            return;
        }

        foreach (var usage in usages)
        {
            usage.Used = 0;
            usage.LastResetAt = now;
            usage.UpdatedAt = now;
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("UsageResetJob: Reset {Count} usage counters for subscription {SubscriptionId}",
            usages.Count, subscriptionId);
    }
}

public static class UsageResetJobRegistration
{
    public static void RegisterUsageResetJob(this IServiceCollection services)
    {
        services.AddScoped<UsageResetJob>();
    }
}
