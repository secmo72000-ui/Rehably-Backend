using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rehably.Application.DTOs.Package;
using Rehably.Application.DTOs.Subscription;
using Rehably.Application.Interfaces;
using Rehably.Application.Repositories;
using Rehably.Application.Services.Platform;
using Rehably.Application.Services.Communication;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;
using Rehably.Infrastructure.Services.Platform;
using System.Text.Json;

namespace Rehably.Infrastructure.Jobs;

public class SubscriptionUsageJobs
{
    private readonly ApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUsageService _usageService;
    private readonly IUsageAuditService _usageAuditService;
    private readonly IEmailService _emailService;
    private readonly ILogger<SubscriptionUsageJobs> _logger;
    private readonly IClock _clock;

    public SubscriptionUsageJobs(
        ApplicationDbContext context,
        IUnitOfWork unitOfWork,
        IUsageService usageService,
        IUsageAuditService usageAuditService,
        IEmailService emailService,
        ILogger<SubscriptionUsageJobs> logger,
        IClock clock)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _usageService = usageService;
        _usageAuditService = usageAuditService;
        _emailService = emailService;
        _logger = logger;
        _clock = clock;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteMonthlyResetJobAsync()
    {
        var now = _clock.UtcNow;
        if (now.Day != 1)
        {
            _logger.LogInformation("Monthly usage reset skipped - not the first day of the month");
            return;
        }

        var activeSubscriptions = await _context.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial)
            .Where(s => s.EndDate >= _clock.UtcNow)
            .Include(s => s.Package)
                .ThenInclude(p => p.Features)
                    .ThenInclude(pf => pf.Feature)
            .Include(s => s.FeatureUsage)
            .ToListAsync();

        var resetCount = 0;

        foreach (var subscription in activeSubscriptions)
        {
            try
            {
                var monthlyFeatures = subscription.Package.Features
                    .Where(pf => pf.IsIncluded && pf.Feature.PricingType == PricingType.PerUnit)
                    .Where(pf => pf.Feature.Code is "sms" or "whatsapp" or "email")
                    .ToList();

                foreach (var feature in monthlyFeatures)
                {
                    var usage = subscription.FeatureUsage
                        .FirstOrDefault(fu => fu.FeatureId == feature.FeatureId);

                    if (usage != null)
                    {
                        var previousValue = usage.Used;
                        await _usageService.ResetFeatureUsageAsync(subscription.Id, feature.FeatureId);
                        await _usageAuditService.LogUsageResetAsync(subscription.ClinicId, feature.Feature.Code, previousValue);
                        resetCount++;
                    }
                }

                _logger.LogInformation("Reset monthly usage for subscription {SubscriptionId}, clinic {ClinicId}", subscription.Id, subscription.ClinicId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset usage for subscription {SubscriptionId}", subscription.Id);
            }
        }

        _logger.LogInformation("Monthly usage reset job completed: {Count} features reset", resetCount);
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteWeeklyLimitWarningsJobAsync()
    {
        var today = _clock.UtcNow.DayOfWeek;
        if (today != DayOfWeek.Monday)
        {
            _logger.LogInformation("Weekly limit warning check skipped - not Monday");
            return;
        }

        var activeSubscriptions = await _context.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial)
            .Where(s => s.EndDate >= _clock.UtcNow)
            .Include(s => s.Package)
                .ThenInclude(p => p.Features)
                    .ThenInclude(pf => pf.Feature)
            .Include(s => s.FeatureUsage)
            .ToListAsync();

        // Batch-load all clinic data upfront to avoid N+1 queries
        var warningClinicIds = activeSubscriptions.Select(s => s.ClinicId).Distinct().ToList();
        var warningClinicMap = await _context.Clinics
            .Where(c => warningClinicIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id);

        var warningCount = 0;

        foreach (var subscription in activeSubscriptions)
        {
            try
            {
                warningClinicMap.TryGetValue(subscription.ClinicId, out var clinic);
                if (clinic?.Email == null)
                {
                    continue;
                }

                var warningFeatures = new List<string>();

                foreach (var usage in subscription.FeatureUsage)
                {
                    var feature = subscription.Package.Features
                        .FirstOrDefault(pf => pf.FeatureId == usage.FeatureId && pf.IsIncluded);

                    if (feature == null || !feature.Quantity.HasValue || feature.Quantity.Value <= 0)
                    {
                        continue;
                    }

                    var limit = feature.Quantity.Value;
                    var percentage = (double)usage.Used / limit * 100;

                    if (percentage >= 80 && percentage < 100)
                    {
                        await _usageAuditService.LogLimitWarningAsync(
                            subscription.ClinicId,
                            feature.Feature.Code,
                            usage.Used,
                            limit);

                        warningFeatures.Add($"{feature.Feature.Name}: {usage.Used}/{limit} ({percentage:F0}%)");
                    }
                }

                if (warningFeatures.Count > 0)
                {
                    await SendWarningEmailAsync(clinic.Email, clinic.Name, warningFeatures);
                    warningCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send limit warnings for subscription {SubscriptionId}", subscription.Id);
            }
        }

        _logger.LogInformation("Weekly limit warning job completed: {Count} warnings sent", warningCount);
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteMonthlyUsageReportJobAsync()
    {
        var now = _clock.UtcNow;
        if (now.Day != 1)
        {
            _logger.LogInformation("Monthly usage report skipped - not the first day of the month");
            return;
        }

        var lastMonthStart = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
        var lastMonthEnd = new DateTime(now.Year, now.Month, 1).AddDays(-1);

        var clinics = await _context.Clinics
            .Where(c => c.Status == ClinicStatus.Active)
            .ToListAsync();

        var reportCount = 0;

        foreach (var clinic in clinics)
        {
            try
            {
                if (string.IsNullOrEmpty(clinic.Email))
                {
                    continue;
                }

                var monthlyHistoryQuery = _context.UsageHistories
                    .Where(h => h.ClinicId == clinic.Id && h.RecordedAt >= lastMonthStart && h.RecordedAt <= lastMonthEnd)
                    .GroupBy(h => h.MetricType)
                    .Select(g => new { Metric = g.Key, Total = g.Sum(h => h.Value) })
                    .ToList();

                var monthlyHistory = monthlyHistoryQuery.Select(h => (Metric: h.Metric, Total: h.Total)).ToList();

                await SendUsageReportEmailAsync(clinic.Email, clinic.Name, lastMonthStart, monthlyHistory);
                reportCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send usage report for clinic {ClinicId}", clinic.Id);
            }
        }

        _logger.LogInformation("Monthly usage report job completed: {Count} reports sent", reportCount);
    }

    private async Task SendWarningEmailAsync(string email, string clinicName, List<string> warnings)
    {
        var body = $@"
            <h2>Usage Limit Warning - {clinicName}</h2>
            <p>You are approaching your usage limits for the following features:</p>
            <ul>
                {string.Join("\n", warnings.Select(w => $"<li>{w}</li>"))}
            </ul>
            <p>Consider upgrading your plan to avoid service interruption.</p>
            <p><a href='/dashboard/settings/subscription'>Manage Subscription</a></p>
        ";

        await _emailService.SendWithDefaultProviderAsync(new()
        {
            To = email,
            Subject = $"Usage Limit Warning - {clinicName}",
            Body = body,
            IsHtml = true
        });
    }

    private async Task SendUsageReportEmailAsync(string email, string clinicName, DateTime reportMonth, List<(MetricType Metric, long Total)> monthlyHistory)
    {
        var body = $@"
            <h2>Monthly Usage Report - {clinicName}</h2>
            <p>Here's your usage summary for {reportMonth:MMMM yyyy}:</p>
            <ul>
                {string.Join("\n", monthlyHistory.Select(h => $"<li>{h.Metric}: {FormatMetricValue(h.Metric, h.Total)}</li>"))}
            </ul>
            <p><a href='/dashboard/analytics'>View Detailed Analytics</a></p>
        ";

        await _emailService.SendWithDefaultProviderAsync(new()
        {
            To = email,
            Subject = $"Monthly Usage Report - {clinicName}",
            Body = body,
            IsHtml = true
        });
    }

    private static string FormatMetricValue(MetricType metric, long value)
    {
        return metric switch
        {
            MetricType.Storage => $"{value / (1024.0 * 1024 * 1024):F2} GB",
            MetricType.ApiCalls => $"{value:N0} calls",
            _ => $"{value:N0}"
        };
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteTrialExpirationJobAsync()
    {
        var expiredTrials = await _context.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Trial)
            .Where(s => s.TrialEndsAt.HasValue && s.TrialEndsAt.Value <= _clock.UtcNow)
            .Include(s => s.Package)
            .ToListAsync();

        var expiredCount = 0;

        foreach (var subscription in expiredTrials)
        {
            try
            {
                if (subscription.AutoRenew)
                {
                    // Trial auto-renew: set to Expired — payment must succeed before activating
                    subscription.Status = SubscriptionStatus.Expired;
                    subscription.TrialEndsAt = null;
                    _logger.LogInformation(
                        "Trial expired for subscription {SubscriptionId} — awaiting payment before activation",
                        subscription.Id);
                }
                else
                {
                    subscription.Status = SubscriptionStatus.Cancelled;
                    subscription.CancelReason = "Trial expired without auto-renewal";
                    _logger.LogInformation("Cancelled expired trial subscription {SubscriptionId}", subscription.Id);
                }

                subscription.UpdatedAt = _clock.UtcNow;
                expiredCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process trial expiration for subscription {SubscriptionId}", subscription.Id);
            }
        }

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Trial expiration job completed: {Count} subscriptions processed", expiredCount);
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteSubscriptionRenewalJobAsync()
    {
        var dueForRenewal = await _context.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active)
            .Where(s => s.AutoRenew)
            .Where(s => s.EndDate <= _clock.UtcNow.AddDays(3))
            .Where(s => !_context.Invoices.Any(i => i.SubscriptionId == s.Id && i.BillingPeriodEnd >= s.EndDate))
            .Include(s => s.Package)
            .Include(s => s.FeatureUsage)
            .ToListAsync();

        var renewalCount = 0;

        foreach (var subscription in dueForRenewal)
        {
            try
            {
                var extensionDays = subscription.BillingCycle == BillingCycle.Yearly ? 365 : 30;
                subscription.EndDate = subscription.EndDate.AddDays(extensionDays);
                subscription.UpdatedAt = _clock.UtcNow;

                _logger.LogInformation("Extended subscription {SubscriptionId} by {Days} days", subscription.Id, extensionDays);
                renewalCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to renew subscription {SubscriptionId}", subscription.Id);
            }
        }

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Subscription renewal job completed: {Count} subscriptions renewed", renewalCount);
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteOverdueInvoiceJobAsync()
    {
        var overdueThreshold = _clock.UtcNow.AddDays(-7);

        var overdueInvoices = await _context.Invoices
            .Where(i => i.PaidAt == null)
            .Where(i => i.DueDate < overdueThreshold)
            .Include(i => i.Subscription)
            .Include(i => i.Payments)
            .ToListAsync();

        // Batch-load all clinics for overdue invoices upfront to avoid N+1 queries
        var overdueClinicIds = overdueInvoices
            .Where(i => i.Subscription != null)
            .Select(i => i.Subscription!.ClinicId)
            .Distinct()
            .ToList();
        var overdueClinicMap = await _context.Clinics
            .Where(c => overdueClinicIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id);

        var suspendedCount = 0;

        foreach (var invoice in overdueInvoices)
        {
            try
            {
                var subscription = invoice.Subscription;
                if (subscription != null && subscription.Status == SubscriptionStatus.Active)
                {
                    subscription.Status = SubscriptionStatus.Suspended;
                    subscription.UpdatedAt = _clock.UtcNow;
                    suspendedCount++;

                    _logger.LogInformation("Suspended subscription {SubscriptionId} due to overdue invoice {InvoiceId}",
                        subscription.Id, invoice.Id);

                    overdueClinicMap.TryGetValue(subscription.ClinicId, out var clinic);
                    if (clinic != null && !string.IsNullOrEmpty(clinic.Email))
                    {
                        await SendOverdueInvoiceEmailAsync(clinic.Email, clinic.Name, invoice);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process overdue invoice {InvoiceId}", invoice.Id);
            }
        }

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Overdue invoice job completed: {Count} subscriptions suspended", suspendedCount);
    }

    private async Task SendOverdueInvoiceEmailAsync(string email, string clinicName, Invoice invoice)
    {
        var body = $@"
            <h2>Overdue Invoice Notice - {clinicName}</h2>
            <p>Your invoice <strong>{invoice.InvoiceNumber}</strong> is overdue.</p>
            <p><strong>Amount Due:</strong> {invoice.TotalAmount:C} EGP</p>
            <p><strong>Due Date:</strong> {invoice.DueDate:yyyy-MM-dd}</p>
            <p>Your subscription has been suspended due to non-payment. Please pay the outstanding amount to reactivate your service.</p>
            <p><a href='/dashboard/billing/invoices/{invoice.Id}'>View Invoice</a></p>
        ";

        await _emailService.SendWithDefaultProviderAsync(new()
        {
            To = email,
            Subject = $"Overdue Invoice - {invoice.InvoiceNumber}",
            Body = body,
            IsHtml = true
        });
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteScheduledUpgradeJobAsync()
    {
        var pendingUpgrades = await _context.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active)
            .Where(s => s.NextPackageId.HasValue)
            .Where(s => s.EndDate <= _clock.UtcNow.AddDays(1))
            .Include(s => s.Package)
            .ToListAsync();

        // Batch-load all packages and clinics for upgrades upfront to avoid N+1 queries
        var nextPackageIds = pendingUpgrades
            .Where(s => s.NextPackageId.HasValue)
            .Select(s => s.NextPackageId!.Value)
            .Distinct()
            .ToList();
        var upgradePackageMap = await _context.Packages
            .Where(p => nextPackageIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var upgradeClinicIds = pendingUpgrades.Select(s => s.ClinicId).Distinct().ToList();
        var upgradeClinicMap = await _context.Clinics
            .Where(c => upgradeClinicIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id);

        var upgradeCount = 0;

        foreach (var subscription in pendingUpgrades)
        {
            try
            {
                if (!subscription.NextPackageId.HasValue)
                {
                    continue;
                }

                upgradePackageMap.TryGetValue(subscription.NextPackageId.Value, out var newPackage);
                if (newPackage == null || newPackage.Status != PackageStatus.Active)
                {
                    _logger.LogWarning("Target package {PackageId} not found or inactive for subscription {SubscriptionId}",
                        subscription.NextPackageId, subscription.Id);
                    continue;
                }

                var oldPackageName = subscription.Package.Name;
                var oldPriceSnapshot = subscription.PriceSnapshot;

                subscription.PackageId = newPackage.Id;
                subscription.PriceSnapshot = await CreatePriceSnapshotAsync(newPackage.Id);
                subscription.NextPackageId = null;
                subscription.UpdatedAt = _clock.UtcNow;

                _logger.LogInformation("Upgraded subscription {SubscriptionId} from {OldPackage} to {NewPackage}",
                    subscription.Id, oldPackageName, newPackage.Name);

                upgradeClinicMap.TryGetValue(subscription.ClinicId, out var clinic);
                if (clinic != null && !string.IsNullOrEmpty(clinic.Email))
                {
                    await SendUpgradeEmailAsync(clinic.Email, clinic.Name, oldPackageName, newPackage.Name);
                }

                upgradeCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process scheduled upgrade for subscription {SubscriptionId}", subscription.Id);
            }
        }

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Scheduled upgrade job completed: {Count} subscriptions upgraded", upgradeCount);
    }

    private async Task<string> CreatePriceSnapshotAsync(Guid packageId)
    {
        var package = await _context.Packages
            .Include(p => p.Features)
                .ThenInclude(pf => pf.Feature)
            .FirstOrDefaultAsync(p => p.Id == packageId);

        if (package == null)
        {
            return string.Empty;
        }

        var snapshot = new PackageSnapshotDto
        {
            PackageId = package.Id,
            PackageName = package.Name,
            PackageCode = package.Code,
            MonthlyPrice = package.MonthlyPrice,
            YearlyPrice = package.YearlyPrice,
            CalculatedMonthlyPrice = package.CalculatedMonthlyPrice,
            CalculatedYearlyPrice = package.CalculatedYearlyPrice,
            Features = package.Features.Select(f => new PackageFeatureSnapshotDto
            {
                FeatureId = f.FeatureId,
                FeatureName = f.Feature.Name,
                FeatureCode = f.Feature.Code,
                Limit = f.Quantity,
                IsIncluded = f.IsIncluded,
                CalculatedPrice = f.CalculatedPrice
            }).ToList()
        };

        return System.Text.Json.JsonSerializer.Serialize(snapshot);
    }

    private async Task SendUpgradeEmailAsync(string email, string clinicName, string oldPackage, string newPackage)
    {
        var body = $@"
            <h2>Subscription Upgraded - {clinicName}</h2>
            <p>Your subscription has been successfully upgraded.</p>
            <p><strong>Previous Package:</strong> {oldPackage}</p>
            <p><strong>New Package:</strong> {newPackage}</p>
            <p>You now have access to all the features included in your new package.</p>
            <p><a href='/dashboard/settings/subscription'>Manage Subscription</a></p>
        ";

        await _emailService.SendWithDefaultProviderAsync(new()
        {
            To = email,
            Subject = $"Subscription Upgraded - {oldPackage} to {newPackage}",
            Body = body,
            IsHtml = true
        });
    }
}

public static class SubscriptionUsageJobsRegistration
{
    public static void RegisterSubscriptionUsageJobs(this IServiceCollection services)
    {
        services.AddScoped<SubscriptionUsageJobs>();
    }

    public static void ScheduleSubscriptionUsageJobs(this IRecurringJobManager recurringJobs)
    {
        recurringJobs.AddOrUpdate<SubscriptionUsageJobs>(
            "subscription-monthly-reset",
            x => x.ExecuteMonthlyResetJobAsync(),
            Cron.Monthly(1, 0));

        recurringJobs.AddOrUpdate<SubscriptionUsageJobs>(
            "subscription-weekly-warnings",
            x => x.ExecuteWeeklyLimitWarningsJobAsync(),
            Cron.Weekly(DayOfWeek.Monday, 9));

        recurringJobs.AddOrUpdate<SubscriptionUsageJobs>(
            "subscription-monthly-report",
            x => x.ExecuteMonthlyUsageReportJobAsync(),
            Cron.Monthly(1, 1));

        recurringJobs.AddOrUpdate<SubscriptionUsageJobs>(
            "subscription-trial-expiration",
            x => x.ExecuteTrialExpirationJobAsync(),
            Cron.Daily(0));

        // DISABLED: Renewal extends dates without charging — will rebuild with self-registration
        // recurringJobs.AddOrUpdate<SubscriptionUsageJobs>(
        //     "subscription-renewal",
        //     x => x.ExecuteSubscriptionRenewalJobAsync(),
        //     Cron.Daily(1));

        recurringJobs.AddOrUpdate<SubscriptionUsageJobs>(
            "subscription-overdue-invoices",
            x => x.ExecuteOverdueInvoiceJobAsync(),
            Cron.Daily(2));

        recurringJobs.AddOrUpdate<SubscriptionUsageJobs>(
            "subscription-scheduled-upgrade",
            x => x.ExecuteScheduledUpgradeJobAsync(),
            Cron.Daily(3));
    }
}
