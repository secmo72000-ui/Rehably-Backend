using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rehably.Application.Interfaces;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;
using Rehably.Application.Services.Platform;
using Rehably.Infrastructure.Services.Platform;

namespace Rehably.Infrastructure.Jobs;

public class TrialReminderJob
{
    private readonly ApplicationDbContext _context;
    private readonly ISubscriptionNotificationService _notificationService;
    private readonly IClock _clock;
    private readonly ILogger<TrialReminderJob> _logger;

    public TrialReminderJob(
        ApplicationDbContext context,
        ISubscriptionNotificationService notificationService,
        IClock clock,
        ILogger<TrialReminderJob> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _clock = clock;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync()
    {
        var now = _clock.UtcNow;
        var sevenDayThreshold = now.AddDays(7);
        var oneDayThreshold = now.AddDays(1);

        var trialSubscriptions = await _context.Subscriptions
            .IgnoreQueryFilters()
            .Where(s => s.Status == SubscriptionStatus.Trial && s.TrialEndsAt.HasValue)
            .ToListAsync();

        var reminderCount = 0;

        foreach (var subscription in trialSubscriptions)
        {
            try
            {
                var trialEndsAt = subscription.TrialEndsAt!.Value;
                var daysRemaining = (trialEndsAt.Date - now.Date).TotalDays;

                if (daysRemaining is >= 0.9 and <= 1.1)
                {
                    await _notificationService.SendTrialEndingReminderAsync(subscription.Id, 1);
                    _logger.LogInformation("Sent urgent 1-day trial reminder for subscription {SubscriptionId}", subscription.Id);
                    reminderCount++;
                }
                else if (daysRemaining is >= 6.9 and <= 7.1)
                {
                    await _notificationService.SendTrialEndingReminderAsync(subscription.Id, 7);
                    _logger.LogInformation("Sent 7-day trial reminder for subscription {SubscriptionId}", subscription.Id);
                    reminderCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send trial reminder for subscription {SubscriptionId}", subscription.Id);
            }
        }

        _logger.LogInformation("Trial reminder job completed: {Count} reminders sent", reminderCount);
    }
}

public static class TrialReminderJobRegistration
{
    public static void RegisterTrialReminderJob(this IServiceCollection services)
    {
        services.AddScoped<TrialReminderJob>();
    }

    public static void ScheduleTrialReminderJob(this IRecurringJobManager recurringJobs)
    {
        recurringJobs.AddOrUpdate<TrialReminderJob>(
            "trial-reminder",
            x => x.ExecuteAsync(),
            Cron.Daily(8));
    }
}
