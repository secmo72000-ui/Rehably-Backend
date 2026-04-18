using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rehably.Application.Interfaces;
using Rehably.Application.Repositories;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Jobs;

public class RegistrationCleanupJob
{
    private readonly ApplicationDbContext _context;
    private readonly IClinicRepository _clinicRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<RegistrationCleanupJob> _logger;

    public RegistrationCleanupJob(
        ApplicationDbContext context,
        IClinicRepository clinicRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<RegistrationCleanupJob> logger)
    {
        _context = context;
        _clinicRepository = clinicRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync()
    {
        var now = _clock.UtcNow;
        var cutoffDate = now.AddDays(-30);

        var incompleteClinicIds = await _context.Clinics
            .IgnoreQueryFilters()
            .Where(c =>
                (c.Status == ClinicStatus.PendingEmailVerification ||
                 c.Status == ClinicStatus.PendingDocumentsAndPackage) &&
                c.CreatedAt < cutoffDate &&
                !c.IsDeleted)
            .Select(c => c.Id)
            .ToListAsync();

        if (incompleteClinicIds.Count == 0)
        {
            _logger.LogInformation("RegistrationCleanupJob: No stale registrations found");
            return;
        }

        var deletedCount = 0;

        foreach (var clinicId in incompleteClinicIds)
        {
            try
            {
                await DeleteRegistrationAsync(clinicId);
                deletedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RegistrationCleanupJob: Failed to delete clinic {ClinicId}", clinicId);
            }
        }

        _logger.LogInformation("RegistrationCleanupJob completed: {Count} stale registrations removed", deletedCount);
    }

    private async Task DeleteRegistrationAsync(Guid clinicId)
    {
        var onboarding = await _context.ClinicOnboardings
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.ClinicId == clinicId);

        if (onboarding != null)
        {
            _context.ClinicOnboardings.Remove(onboarding);
        }

        var subscriptions = await _context.Subscriptions
            .IgnoreQueryFilters()
            .Where(s => s.ClinicId == clinicId)
            .ToListAsync();
        _context.Subscriptions.RemoveRange(subscriptions);

        var documents = await _context.ClinicDocuments
            .IgnoreQueryFilters()
            .Where(d => d.ClinicId == clinicId)
            .ToListAsync();
        _context.ClinicDocuments.RemoveRange(documents);

        var clinic = await _context.Clinics
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == clinicId);
        if (clinic != null)
        {
            _context.Clinics.Remove(clinic);
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("RegistrationCleanupJob: Deleted stale registration for clinic {ClinicId}", clinicId);
    }
}

public static class RegistrationCleanupJobRegistration
{
    public static void RegisterRegistrationCleanupJob(this IServiceCollection services)
    {
        services.AddScoped<RegistrationCleanupJob>();
    }

    public static void ScheduleRegistrationCleanupJob(this IRecurringJobManager recurringJobs)
    {
        recurringJobs.AddOrUpdate<RegistrationCleanupJob>(
            "registration-cleanup",
            x => x.ExecuteAsync(),
            Cron.Daily(3));
    }
}
