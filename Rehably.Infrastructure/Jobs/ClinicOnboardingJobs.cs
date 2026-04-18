using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rehably.Application.Contexts;
using Rehably.Application.Interfaces;
using Rehably.Application.Services.Platform;
using Rehably.Application.Repositories;
using Rehably.Application.Services;
using Rehably.Application.Services.Communication;
using Rehably.Application.Services.Storage;
using Rehably.Application.Services.Clinic;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;
using Rehably.Infrastructure.Services.Platform;
using Rehably.Infrastructure.Settings;

namespace Rehably.Infrastructure.Jobs;

public class ClinicOnboardingJobs
{
    private readonly ApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly IClinicUsageTrackingService _usageTrackingService;
    private readonly IDocumentService _documentService;
    private readonly AppSettings _settings;
    private readonly ILogger<ClinicOnboardingJobs> _logger;
    private readonly IClock _clock;

    public ClinicOnboardingJobs(
        ApplicationDbContext context,
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        IClinicUsageTrackingService usageTrackingService,
        IDocumentService documentService,
        IOptions<AppSettings> settings,
        ILogger<ClinicOnboardingJobs> logger,
        IClock clock)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _usageTrackingService = usageTrackingService;
        _documentService = documentService;
        _settings = settings.Value;
        _logger = logger;
        _clock = clock;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteCleanupAbandonedRegistrationsAsync()
    {
        var cutoffDate = _clock.UtcNow.AddDays(-7);

        var abandonedOnboardings = await _context.ClinicOnboardings
            .Include(o => o.Clinic)
            .Where(o =>
                o.CurrentStep == OnboardingStep.PendingEmailVerification ||
                o.CurrentStep == OnboardingStep.PendingDocumentUpload ||
                o.CurrentStep == OnboardingStep.PendingApproval)
            .Where(o => o.CreatedAt < cutoffDate)
            .ToListAsync();

        foreach (var onboarding in abandonedOnboardings)
        {
            try
            {
                await DeleteOnboardingDataAsync(onboarding);
                _logger.LogInformation("Cleaned up abandoned registration {OnboardingId} for clinic {ClinicId}",
                    onboarding.Id, onboarding.ClinicId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clean up onboarding {OnboardingId}", onboarding.Id);
            }
        }

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Cleanup job completed: {Count} registrations removed", abandonedOnboardings.Count);
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecutePaymentReminderJobAsync()
    {
        var cutoffDate = _clock.UtcNow.AddHours(-24);

        var pendingPaymentOnboardings = await _context.ClinicOnboardings
            .Include(o => o.Clinic!)
            .ThenInclude(c => c.CurrentSubscription!)
                .ThenInclude(s => s.Package!)
            .Where(o => o.CurrentStep == OnboardingStep.PendingPayment)
            .Where(o => o.ApprovedAt.HasValue && o.ApprovedAt.Value < cutoffDate)
            .Where(o => !o.PaymentCompletedAt.HasValue)
            .ToListAsync();

        foreach (var onboarding in pendingPaymentOnboardings)
        {
            if (onboarding.Clinic?.Email == null)
            {
                continue;
            }

            try
            {
                var subscription = onboarding.Clinic.CurrentSubscription;
                if (subscription?.Package == null)
                {
                    continue;
                }

                var paymentUrl = $"{_settings.FrontendUrl}/registration/{onboarding.Id}/payment";

                var emailBody = ClinicOnboardingEmailTemplates.PaymentPending(
                    onboarding.Clinic.Name,
                    onboarding.Clinic.Name.Split(' ').FirstOrDefault() ?? "Clinic Owner",
                    subscription.Package.Name,
                    subscription.Package.MonthlyPrice,
                    "USD",
                    paymentUrl,
                    _clock.UtcNow.AddDays(3));

                await _emailService.SendWithDefaultProviderAsync(new()
                {
                    To = onboarding.Clinic.Email,
                    Subject = "Complete Your Payment to Activate Your Account",
                    Body = emailBody,
                    IsHtml = true
                });

                _logger.LogInformation("Payment reminder sent for onboarding {OnboardingId}", onboarding.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send payment reminder for onboarding {OnboardingId}", onboarding.Id);
            }
        }

        _logger.LogInformation("Payment reminder job completed: {Count} reminders sent", pendingPaymentOnboardings.Count);
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteMonthlyUsageResetJobAsync()
    {
        var now = _clock.UtcNow;
        if (now.Day != 1)
        {
            _logger.LogInformation("Monthly usage reset skipped - not the first day of the month");
            return;
        }

        var currentPeriod = new DateTime(now.Year, now.Month, 1);

        var clinics = await _context.Clinics
            .Include(c => c.CurrentSubscription)
            .Where(c => c.Status == ClinicStatus.Active)
            .ToListAsync();

        var resetCount = 0;

        foreach (var clinic in clinics)
        {
            try
            {
                var activeSubscription = clinic.CurrentSubscription;
                if (activeSubscription == null ||
                    (activeSubscription.Status != SubscriptionStatus.Active && activeSubscription.Status != SubscriptionStatus.Trial))
                {
                    continue;
                }

                var metricsToReset = new[]
                {
                    UsageMetric.SmsSent,
                    UsageMetric.WhatsappSent,
                    UsageMetric.EmailSent
                };

                foreach (var metric in metricsToReset)
                {
                    var result = await _usageTrackingService.ResetMonthlyUsageAsync(clinic.Id);
                    if (result.IsSuccess)
                    {
                        resetCount++;
                    }
                }

                _logger.LogInformation("Reset monthly usage for clinic {ClinicId}", clinic.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset usage for clinic {ClinicId}", clinic.Id);
            }
        }

        _logger.LogInformation("Monthly usage reset job completed: {Count} clinics processed", clinics.Count);
    }

    private async Task DeleteOnboardingDataAsync(ClinicOnboarding onboarding)
    {
        var documents = await _context.ClinicDocuments
            .Where(d => d.ClinicId == onboarding.ClinicId)
            .ToListAsync();

        foreach (var document in documents)
        {
            var deleteResult = await _documentService.DeleteDocumentAsync(onboarding.ClinicId, document.Id);
            if (!deleteResult.IsSuccess)
            {
                _logger.LogWarning("Failed to delete document {DocumentId} for clinic {ClinicId}", document.Id, onboarding.ClinicId);
            }
        }

        _context.ClinicDocuments.RemoveRange(documents);

        if (onboarding.Clinic != null)
        {
            var subscriptions = await _context.Subscriptions
                .Where(s => s.ClinicId == onboarding.ClinicId)
                .ToListAsync();

            _context.Subscriptions.RemoveRange(subscriptions);
            _context.Clinics.Remove(onboarding.Clinic);
        }

        _context.ClinicOnboardings.Remove(onboarding);
    }
}

public static class ClinicOnboardingJobsRegistration
{
    public static void RegisterClinicOnboardingJobs(this IServiceCollection services)
    {
        services.AddScoped<ClinicOnboardingJobs>();
    }

    public static void ScheduleClinicOnboardingJobs(this IRecurringJobManager recurringJobs)
    {
        recurringJobs.AddOrUpdate<ClinicOnboardingJobs>(
            "cleanup-abandoned-registrations",
            x => x.ExecuteCleanupAbandonedRegistrationsAsync(),
            Cron.Daily(1));

        recurringJobs.AddOrUpdate<ClinicOnboardingJobs>(
            "payment-reminder-job",
            x => x.ExecutePaymentReminderJobAsync(),
            Cron.Hourly);

        recurringJobs.AddOrUpdate<ClinicOnboardingJobs>(
            "monthly-usage-reset",
            x => x.ExecuteMonthlyUsageResetJobAsync(),
            Cron.Monthly(1, 0));
    }
}
