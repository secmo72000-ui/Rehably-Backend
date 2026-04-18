using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rehably.Application.Interfaces;
using Rehably.Application.Repositories;
using Rehably.Application.Services.Platform;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Jobs;

public class AddOnExpiryJob
{
    private readonly ApplicationDbContext _context;
    private readonly IAddOnService _addOnService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<AddOnExpiryJob> _logger;

    public AddOnExpiryJob(
        ApplicationDbContext context,
        IAddOnService addOnService,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<AddOnExpiryJob> logger)
    {
        _context = context;
        _addOnService = addOnService;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync()
    {
        var now = _clock.UtcNow;

        var expiredAddOns = await _context.SubscriptionAddOns
            .Where(a => a.Status == AddOnStatus.Active && a.EndDate <= now)
            .ToListAsync();

        if (expiredAddOns.Count == 0)
        {
            _logger.LogInformation("Add-on expiry job: no expired add-ons found");
            return;
        }

        var affectedClinicIds = new HashSet<Guid>();

        // Fix N+1: batch-load all subscription clinic IDs in ONE query instead of one FindAsync per add-on
        var subscriptionIds = expiredAddOns.Select(a => a.SubscriptionId).Distinct().ToList();
        var clinicIdBySubscription = await _context.Subscriptions
            .Where(s => subscriptionIds.Contains(s.Id))
            .Select(s => new { s.Id, s.ClinicId })
            .ToDictionaryAsync(s => s.Id, s => s.ClinicId);

        foreach (var addOn in expiredAddOns)
        {
            try
            {
                addOn.Status = AddOnStatus.Expired;
                addOn.UpdatedAt = now;

                if (clinicIdBySubscription.TryGetValue(addOn.SubscriptionId, out var clinicId))
                    affectedClinicIds.Add(clinicId);

                _logger.LogInformation("Expired add-on {AddOnId} for subscription {SubscriptionId}",
                    addOn.Id, addOn.SubscriptionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to expire add-on {AddOnId}", addOn.Id);
            }
        }

        await _unitOfWork.SaveChangesAsync();

        foreach (var clinicId in affectedClinicIds)
        {
            try
            {
                await _addOnService.RecalculateClinicLimitsAsync(clinicId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to recalculate limits for clinic {ClinicId}", clinicId);
            }
        }

        _logger.LogInformation("Add-on expiry job completed: {Count} add-ons expired, {Clinics} clinics recalculated",
            expiredAddOns.Count, affectedClinicIds.Count);
    }
}

public static class AddOnExpiryJobRegistration
{
    public static void RegisterAddOnExpiryJob(this IServiceCollection services)
    {
        services.AddScoped<AddOnExpiryJob>();
    }

    public static void ScheduleAddOnExpiryJob(this IRecurringJobManager recurringJobs)
    {
        recurringJobs.AddOrUpdate<AddOnExpiryJob>(
            "addon-expiry-check",
            x => x.ExecuteAsync(),
            Cron.Daily(4));
    }
}
