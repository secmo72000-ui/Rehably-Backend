using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rehably.Application.Interfaces;
using Rehably.Application.Services.Platform;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Jobs;

public class SubscriptionExpiryCheckJob
{
    private readonly ApplicationDbContext _context;
    private readonly ISubscriptionLifecycleService _lifecycleService;
    private readonly IClock _clock;
    private readonly ILogger<SubscriptionExpiryCheckJob> _logger;

    public SubscriptionExpiryCheckJob(
        ApplicationDbContext context,
        ISubscriptionLifecycleService lifecycleService,
        IClock clock,
        ILogger<SubscriptionExpiryCheckJob> logger)
    {
        _context = context;
        _lifecycleService = lifecycleService;
        _clock = clock;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync()
    {
        var now = _clock.UtcNow;

        var expiredSubscriptions = await _context.Subscriptions
            .Where(s => !s.AutoRenew
                     && s.EndDate <= now
                     && s.Status == SubscriptionStatus.Active)
            .ToListAsync();

        var suspendedCount = 0;

        foreach (var subscription in expiredSubscriptions)
        {
            try
            {
                var result = await _lifecycleService.SuspendClinicAsync(
                    subscription.ClinicId,
                    "Subscription expired — auto-renew is off");

                if (result.IsSuccess)
                {
                    suspendedCount++;
                    _logger.LogInformation("Suspended clinic {ClinicId} — subscription {SubscriptionId} expired on {EndDate}",
                        subscription.ClinicId, subscription.Id, subscription.EndDate);
                }
                else
                {
                    _logger.LogError("Failed to suspend clinic {ClinicId} for expired subscription {SubscriptionId}: {Error}",
                        subscription.ClinicId, subscription.Id, result.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error suspending clinic {ClinicId} for subscription {SubscriptionId}",
                    subscription.ClinicId, subscription.Id);
            }
        }

        _logger.LogInformation("Subscription expiry check completed: {Count}/{Total} clinics suspended",
            suspendedCount, expiredSubscriptions.Count);
    }
}

public static class SubscriptionExpiryCheckJobRegistration
{
    public static void RegisterSubscriptionExpiryCheckJob(this IServiceCollection services)
    {
        services.AddScoped<SubscriptionExpiryCheckJob>();
    }

    public static void ScheduleSubscriptionExpiryCheckJob(this IRecurringJobManager recurringJobs)
    {
        recurringJobs.AddOrUpdate<SubscriptionExpiryCheckJob>(
            "subscription-expiry-check",
            x => x.ExecuteAsync(),
            Cron.Daily(3));
    }
}
