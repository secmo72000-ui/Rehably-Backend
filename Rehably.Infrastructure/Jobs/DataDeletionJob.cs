using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rehably.Application.Interfaces;
using Rehably.Application.Repositories;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Jobs;

public class DataDeletionJob
{
    private readonly ApplicationDbContext _context;
    private readonly IClinicRepository _clinicRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<DataDeletionJob> _logger;

    public DataDeletionJob(
        ApplicationDbContext context,
        IClinicRepository clinicRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<DataDeletionJob> logger)
    {
        _context = context;
        _clinicRepository = clinicRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task DeleteClinicData(Guid clinicId)
    {
        var clinic = await _clinicRepository.GetByIdAsync(clinicId);

        if (clinic == null)
        {
            _logger.LogWarning("DataDeletionJob: Clinic {ClinicId} not found", clinicId);
            return;
        }

        if (!clinic.IsSuspended)
        {
            _logger.LogWarning("DataDeletionJob: Clinic {ClinicId} is not suspended — skipping", clinicId);
            return;
        }

        var now = _clock.UtcNow;
        if (clinic.DataDeletionDate == null || clinic.DataDeletionDate > now)
        {
            _logger.LogInformation("DataDeletionJob: Clinic {ClinicId} deletion date not reached ({Date}) — skipping",
                clinicId, clinic.DataDeletionDate);
            return;
        }

        _logger.LogInformation("DataDeletionJob: Starting data deletion for clinic {ClinicId}", clinicId);

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await DeleteUsageDataAsync(clinicId);
            clinic.DeletionStage = DeletionStage.UsageDataDeleted;
            await _unitOfWork.SaveChangesAsync();

            await DeleteSubscriptionDataAsync(clinicId);
            clinic.DeletionStage = DeletionStage.SubscriptionDataDeleted;
            await _unitOfWork.SaveChangesAsync();

            await DeleteBillingDataAsync(clinicId);
            clinic.DeletionStage = DeletionStage.BillingDataDeleted;
            await _unitOfWork.SaveChangesAsync();

            await DeleteDocumentsAsync(clinicId);
            clinic.DeletionStage = DeletionStage.DocumentsDeleted;
            await _unitOfWork.SaveChangesAsync();

            ReleaseSlug(clinic);
            clinic.DeletionStage = DeletionStage.SlugReleased;
            await _unitOfWork.SaveChangesAsync();

            clinic.DeletionStage = DeletionStage.Completed;
            clinic.UpdatedAt = now;
            await _unitOfWork.SaveChangesAsync();

            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("DataDeletionJob: Completed data deletion for clinic {ClinicId}", clinicId);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "DataDeletionJob: Failed to delete data for clinic {ClinicId}", clinicId);
            throw;
        }
    }

    private async Task DeleteUsageDataAsync(Guid clinicId)
    {
        var usageHistories = await _context.UsageHistories
            .IgnoreQueryFilters()
            .Where(u => u.ClinicId == clinicId)
            .ToListAsync();
        _context.UsageHistories.RemoveRange(usageHistories);

        var usageRecords = await _context.UsageRecords
            .IgnoreQueryFilters()
            .Where(u => u.ClinicId == clinicId)
            .ToListAsync();
        _context.UsageRecords.RemoveRange(usageRecords);

        _logger.LogInformation("Deleted usage data for clinic {ClinicId}", clinicId);
    }

    private async Task DeleteSubscriptionDataAsync(Guid clinicId)
    {
        var subscriptionIds = await _context.Subscriptions
            .IgnoreQueryFilters()
            .Where(s => s.ClinicId == clinicId)
            .Select(s => s.Id)
            .ToListAsync();

        var featureUsages = await _context.SubscriptionFeatureUsages
            .Where(u => subscriptionIds.Contains(u.SubscriptionId))
            .ToListAsync();
        _context.SubscriptionFeatureUsages.RemoveRange(featureUsages);

        var addOns = await _context.SubscriptionAddOns
            .Where(a => subscriptionIds.Contains(a.SubscriptionId))
            .ToListAsync();
        _context.SubscriptionAddOns.RemoveRange(addOns);

        var subscriptions = await _context.Subscriptions
            .IgnoreQueryFilters()
            .Where(s => s.ClinicId == clinicId)
            .ToListAsync();
        _context.Subscriptions.RemoveRange(subscriptions);

        _logger.LogInformation("Deleted subscription data for clinic {ClinicId}", clinicId);
    }

    private async Task DeleteBillingDataAsync(Guid clinicId)
    {
        var invoiceIds = await _context.Invoices
            .IgnoreQueryFilters()
            .Where(i => i.ClinicId == clinicId)
            .Select(i => i.Id)
            .ToListAsync();

        var lineItems = await _context.InvoiceLineItems
            .IgnoreQueryFilters()
            .Where(l => invoiceIds.Contains(l.InvoiceId))
            .ToListAsync();
        _context.InvoiceLineItems.RemoveRange(lineItems);

        var payments = await _context.Payments
            .IgnoreQueryFilters()
            .Where(p => p.ClinicId == clinicId)
            .ToListAsync();
        _context.Payments.RemoveRange(payments);

        var invoices = await _context.Invoices
            .IgnoreQueryFilters()
            .Where(i => i.ClinicId == clinicId)
            .ToListAsync();
        _context.Invoices.RemoveRange(invoices);

        _logger.LogInformation("Deleted billing data for clinic {ClinicId}", clinicId);
    }

    private async Task DeleteDocumentsAsync(Guid clinicId)
    {
        var documents = await _context.ClinicDocuments
            .IgnoreQueryFilters()
            .Where(d => d.ClinicId == clinicId)
            .ToListAsync();
        _context.ClinicDocuments.RemoveRange(documents);

        _logger.LogInformation("Deleted documents for clinic {ClinicId}", clinicId);
    }

    private static void ReleaseSlug(Rehably.Domain.Entities.Tenant.Clinic clinic)
    {
        clinic.OriginalSlug = clinic.Slug;
        clinic.Slug = $"deleted_{clinic.Id}";
    }
}

public static class DataDeletionJobRegistration
{
    public static void RegisterDataDeletionJob(this IServiceCollection services)
    {
        services.AddScoped<DataDeletionJob>();
    }
}
