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

public class SubscriptionReminderJob
{
    private readonly ApplicationDbContext _context;
    private readonly ISubscriptionNotificationService _notificationService;
    private readonly ILogger<SubscriptionReminderJob> _logger;
    private readonly IClock _clock;

    public SubscriptionReminderJob(
        ApplicationDbContext context,
        ISubscriptionNotificationService notificationService,
        ILogger<SubscriptionReminderJob> logger,
        IClock clock)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
        _clock = clock;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync()
    {
        var now = _clock.UtcNow;
        var sevenDayWindow = now.AddDays(7);
        var oneDayWindow = now.AddDays(1);

        var expiringSubscriptions = await _context.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial)
            .Where(s => s.EndDate <= sevenDayWindow && s.EndDate > now)
            .ToListAsync();

        var reminderCount = 0;

        foreach (var subscription in expiringSubscriptions)
        {
            try
            {
                var daysUntilExpiry = (int)Math.Ceiling((subscription.EndDate - now).TotalDays);

                var isSevenDayReminder = subscription.EndDate <= sevenDayWindow && subscription.EndDate > oneDayWindow;
                var isOneDayReminder = subscription.EndDate <= oneDayWindow;

                if (isOneDayReminder)
                {
                    await _notificationService.SendSubscriptionExpiringEmailAsync(subscription.Id, 1);
                    reminderCount++;
                    _logger.LogInformation("Sent 1-day expiry reminder for subscription {SubscriptionId}", subscription.Id);
                }
                else if (isSevenDayReminder)
                {
                    await _notificationService.SendSubscriptionExpiringEmailAsync(subscription.Id, daysUntilExpiry);
                    reminderCount++;
                    _logger.LogInformation("Sent {Days}-day expiry reminder for subscription {SubscriptionId}", daysUntilExpiry, subscription.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send reminder for subscription {SubscriptionId}", subscription.Id);
            }
        }

        _logger.LogInformation("Subscription reminder job completed: {Count} reminders sent", reminderCount);
    }
}

public static class SubscriptionReminderJobRegistration
{
    public static void RegisterSubscriptionReminderJob(this IServiceCollection services)
    {
        services.AddScoped<SubscriptionReminderJob>();
    }

    public static void ScheduleSubscriptionReminderJob(this IRecurringJobManager recurringJobs)
    {
        recurringJobs.AddOrUpdate<SubscriptionReminderJob>(
            "subscription-expiry-reminders",
            x => x.ExecuteAsync(),
            Cron.Daily(8));
    }
}
