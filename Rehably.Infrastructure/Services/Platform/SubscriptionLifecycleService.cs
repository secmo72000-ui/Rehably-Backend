using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Subscription;
using Rehably.Application.Interfaces;
using Rehably.Application.Services.Platform;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Jobs;
using System.Text.Json;

namespace Rehably.Infrastructure.Services.Platform;

public class SubscriptionLifecycleService : ISubscriptionLifecycleService
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IPackageRepository _packageRepository;
    private readonly IClinicRepository _clinicRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPricingService _pricingService;
    private readonly IPlatformSubscriptionService _platformSubscriptionService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IInvoiceService _invoiceService;
    private readonly ILogger<SubscriptionLifecycleService> _logger;
    private readonly IClock _clock;

    public SubscriptionLifecycleService(
        ISubscriptionRepository subscriptionRepository,
        IPackageRepository packageRepository,
        IClinicRepository clinicRepository,
        IUnitOfWork unitOfWork,
        IPricingService pricingService,
        IPlatformSubscriptionService platformSubscriptionService,
        IBackgroundJobClient backgroundJobClient,
        IInvoiceService invoiceService,
        ILogger<SubscriptionLifecycleService> logger,
        IClock clock)
    {
        _subscriptionRepository = subscriptionRepository;
        _packageRepository = packageRepository;
        _clinicRepository = clinicRepository;
        _unitOfWork = unitOfWork;
        _pricingService = pricingService;
        _platformSubscriptionService = platformSubscriptionService;
        _backgroundJobClient = backgroundJobClient;
        _invoiceService = invoiceService;
        _logger = logger;
        _clock = clock;
    }

    public async Task<Result<SubscriptionDetailDto>> CreateSubscriptionAsync(CreateSubscriptionRequestDto request)
    {
        var tenantExists = await _clinicRepository.ExistsAsync(request.ClinicId);

        if (!tenantExists)
            return Result<SubscriptionDetailDto>.Failure("Tenant not found");

        var package = await _packageRepository.GetWithFeaturesAsync(request.PackageId);

        if (package == null || package.IsDeleted || package.Status != PackageStatus.Active)
            return Result<SubscriptionDetailDto>.Failure("Package not found or inactive");

        var existingSubscription = await _subscriptionRepository.GetActiveByClinicIdAsync(request.ClinicId);

        if (existingSubscription != null)
            return Result<SubscriptionDetailDto>.Failure("Tenant already has an active subscription");

        var priceSnapshotResult = await _pricingService.CreatePackageSnapshotAsync(request.PackageId);
        if (!priceSnapshotResult.IsSuccess)
            return Result<SubscriptionDetailDto>.Failure("Failed to create price snapshot");

        var priceSnapshotJson = JsonSerializer.Serialize(priceSnapshotResult.Value);

        var startDate = request.StartDate ?? _clock.UtcNow;
        DateTime? trialEndDate = package.TrialDays > 0 ? startDate.AddDays(package.TrialDays) : null;
        var endDate = request.EndDate ?? (request.BillingCycle == BillingCycle.Yearly
            ? startDate.AddYears(1)
            : startDate.AddDays(30));

        var subscription = new Subscription
        {
            ClinicId = request.ClinicId,
            PackageId = request.PackageId,
            Status = package.TrialDays > 0 ? SubscriptionStatus.Trial : SubscriptionStatus.Active,
            StartDate = startDate,
            EndDate = endDate,
            TrialEndsAt = trialEndDate,
            PriceSnapshot = priceSnapshotJson,
            PaymentProvider = request.PaymentProvider,
            PaymentType = request.PaymentType,
            AutoRenew = request.AutoRenew,
            CreatedAt = _clock.UtcNow
        };

        await _subscriptionRepository.AddAsync(subscription);

        foreach (var packageFeature in package.Features)
        {
            if (packageFeature.Feature == null)
                continue;

            var limit = packageFeature.Quantity ?? packageFeature.Limit ?? int.MaxValue;

            subscription.FeatureUsage.Add(new SubscriptionFeatureUsage
            {
                FeatureId = packageFeature.FeatureId,
                Limit = limit,
                Used = 0,
                LastResetAt = _clock.UtcNow,
                CreatedAt = _clock.UtcNow,
                Feature = packageFeature.Feature
            });
        }

        await _unitOfWork.SaveChangesAsync();

        var clinic = await _clinicRepository.GetByIdAsync(request.ClinicId);
        if (clinic != null)
        {
            clinic.CurrentSubscriptionId = subscription.Id;

            foreach (var pf in package.Features.Where(pf => pf.Feature != null && pf.IsIncluded))
            {
                var featureLimit = pf.Quantity ?? pf.Limit;
                var code = pf.Feature.Code.ToLowerInvariant();
                if (code.StartsWith("patient") || code == "max_patients")
                    clinic.PatientsLimit = featureLimit;
                else if (code.StartsWith("user") || code == "max_users")
                    clinic.UsersLimit = featureLimit;
                else if (code.StartsWith("storage"))
                    clinic.StorageLimitBytes = (featureLimit ?? 0) * 1024L * 1024 * 1024;
            }

            await _unitOfWork.SaveChangesAsync();
        }

        if (subscription.Status == SubscriptionStatus.Active && request.PaymentType != PaymentType.Free)
        {
            var invoiceResult = await _invoiceService.GenerateInvoiceAsync(subscription.Id);

            if (invoiceResult.IsSuccess && request.PaymentType == PaymentType.Cash)
            {
                await _invoiceService.MarkAsPaidAsync(invoiceResult.Value.Id, "Cash", $"CASH-{subscription.Id:N}");
            }
        }

        return await _platformSubscriptionService.GetSubscriptionWithDetailsAsync(subscription.Id);
    }

    public async Task<Result<SubscriptionDetailDto>> CancelSubscriptionAsync(Guid id, CancelSubscriptionRequestDto request)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(id);

        if (subscription == null)
            return Result<SubscriptionDetailDto>.Failure("Subscription not found");

        if (subscription.Status == SubscriptionStatus.Cancelled || subscription.Status == SubscriptionStatus.Expired)
            return Result<SubscriptionDetailDto>.Failure("Subscription is already cancelled or expired");

        subscription.Status = SubscriptionStatus.Cancelled;
        subscription.CancelledAt = _clock.UtcNow;
        subscription.CancelReason = request.Reason;
        subscription.AutoRenew = false;
        subscription.UpdatedAt = _clock.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        return await _platformSubscriptionService.GetSubscriptionWithDetailsAsync(id);
    }

    public async Task<Result<SubscriptionDetailDto>> RenewSubscriptionAsync(Guid id, RenewSubscriptionRequestDto request)
    {
        var subscription = await _subscriptionRepository.GetWithPackageAsync(id);

        if (subscription == null)
            return Result<SubscriptionDetailDto>.Failure("Subscription not found");

        if (subscription.Status == SubscriptionStatus.Active)
            return Result<SubscriptionDetailDto>.Failure("Subscription is already active");

        var package = await _packageRepository.GetWithFeaturesAsync(request.PackageId);

        if (package == null || package.IsDeleted || package.Status != PackageStatus.Active)
            return Result<SubscriptionDetailDto>.Failure("Package not found or inactive");

        subscription.Status = SubscriptionStatus.Active;
        subscription.StartDate = _clock.UtcNow;
        subscription.EndDate = _clock.UtcNow.AddDays(30);
        subscription.TrialEndsAt = null;
        subscription.PackageId = request.PackageId;
        subscription.UpdatedAt = _clock.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        return await _platformSubscriptionService.GetSubscriptionWithDetailsAsync(id);
    }

    public async Task<Result> CheckSubscriptionStatusAsync(Guid clinicId)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var subscription = await _subscriptionRepository.GetActiveSubscriptionByClinicIdAsync(clinicId);

            if (subscription == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Result.Failure("No active subscription found");
            }

            if (subscription.EndDate < _clock.UtcNow && subscription.Status == SubscriptionStatus.Active)
            {
                subscription.Status = SubscriptionStatus.Expired;
                subscription.UpdatedAt = _clock.UtcNow;
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                return Result.Failure("Subscription has expired");
            }

            if (subscription.TrialEndsAt.HasValue && subscription.TrialEndsAt < _clock.UtcNow && subscription.Status == SubscriptionStatus.Trial)
            {
                subscription.Status = SubscriptionStatus.Active;
                subscription.UpdatedAt = _clock.UtcNow;
                await _unitOfWork.SaveChangesAsync();
            }

            await _unitOfWork.CommitTransactionAsync();
            return Result.Success();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<Result> ResetUsageAsync(Guid subscriptionId, Guid featureId)
    {
        var usageRepo = _unitOfWork.Repository<SubscriptionFeatureUsage>();
        var usage = (await usageRepo.Query()
            .Where(sf => sf.SubscriptionId == subscriptionId && sf.FeatureId == featureId)
            .FirstOrDefaultAsync());

        if (usage == null)
            return Result.Failure("Feature usage not found");

        usage.Used = 0;
        usage.LastResetAt = _clock.UtcNow;
        usage.UpdatedAt = _clock.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> RenewSubscriptionForCycleAsync(Guid subscriptionId)
    {
        var subscription = await _subscriptionRepository.GetWithPackageAsync(subscriptionId);

        if (subscription == null)
            return Result.Failure("Subscription not found");

        if (!subscription.AutoRenew)
            return Result.Failure("Subscription does not have auto-renew enabled");

        if (subscription.NextPackageId.HasValue)
        {
            var nextPackage = await _packageRepository.GetWithFeaturesAsync(subscription.NextPackageId.Value);

            if (nextPackage != null && nextPackage.Status == PackageStatus.Active)
            {
                subscription.PackageId = nextPackage.Id;
                _logger.LogInformation("Applied scheduled package change for subscription {SubscriptionId} to package {PackageId}",
                    subscriptionId, nextPackage.Id);
            }

            subscription.NextPackageId = null;
        }

        subscription.RenewForNextCycle();

        var usageRepo = _unitOfWork.Repository<SubscriptionFeatureUsage>();
        var usages = await usageRepo.Query()
            .Where(u => u.SubscriptionId == subscriptionId)
            .ToListAsync();

        foreach (var usage in usages)
        {
            usage.Used = 0;
            usage.LastResetAt = _clock.UtcNow;
            usage.UpdatedAt = _clock.UtcNow;
        }

        subscription.UpdatedAt = _clock.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        await _invoiceService.GenerateInvoiceAsync(subscriptionId);

        _logger.LogInformation("Renewed subscription {SubscriptionId} for next cycle", subscriptionId);
        return Result.Success();
    }

    public async Task<Result> SuspendClinicAsync(Guid clinicId, string? reason)
    {
        var clinic = await _clinicRepository.GetWithSubscriptionAsync(clinicId);

        if (clinic == null)
            return Result.Failure("Clinic not found");

        if (clinic.IsSuspended)
            return Result.Failure("Clinic is already suspended");

        clinic.Suspend();

        if (clinic.CurrentSubscription != null)
        {
            clinic.CurrentSubscription.Suspend();
        }

        var deletionJobId = _backgroundJobClient.Schedule<DataDeletionJob>(
            job => job.DeleteClinicData(clinicId),
            TimeSpan.FromDays(30));

        clinic.DeletionJobId = deletionJobId;
        clinic.UpdatedAt = _clock.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Clinic {ClinicId} suspended. Reason: {Reason}. DeletionJobId: {JobId}",
            clinicId, reason ?? "No reason provided", deletionJobId);

        return Result.Success();
    }

    public async Task<Result> ReactivateClinicAsync(Guid clinicId, PaymentType paymentType)
    {
        var clinic = await _clinicRepository.GetWithSubscriptionAsync(clinicId);

        if (clinic == null)
            return Result.Failure("Clinic not found");

        if (!clinic.IsSuspended)
            return Result.Failure("Clinic is not suspended");

        if (!string.IsNullOrEmpty(clinic.DeletionJobId))
        {
            _backgroundJobClient.Delete(clinic.DeletionJobId);
        }

        clinic.Reactivate();

        if (clinic.CurrentSubscription != null)
        {
            clinic.CurrentSubscription.Status = SubscriptionStatus.Active;
            clinic.CurrentSubscription.SuspendedAt = null;
            clinic.CurrentSubscription.UpdatedAt = _clock.UtcNow;
        }

        clinic.UpdatedAt = _clock.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        if (clinic.CurrentSubscription != null && paymentType != PaymentType.Free)
        {
            await _invoiceService.GenerateInvoiceAsync(clinic.CurrentSubscription.Id);
        }

        _logger.LogInformation("Clinic {ClinicId} reactivated with payment type {PaymentType}", clinicId, paymentType);
        return Result.Success();
    }

    public async Task<Result> ConvertTrialAsync(Guid subscriptionId, PaymentType paymentType)
    {
        var subscription = await _subscriptionRepository.GetWithPackageAsync(subscriptionId);

        if (subscription == null)
            return Result.Failure("Subscription not found");

        if (subscription.Status != SubscriptionStatus.Trial)
            return Result.Failure("Subscription is not in trial status");

        subscription.Status = SubscriptionStatus.Active;
        subscription.TrialEndsAt = null;
        subscription.PaymentType = paymentType;
        subscription.UpdatedAt = _clock.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        if (paymentType != PaymentType.Free)
        {
            await _invoiceService.GenerateInvoiceAsync(subscriptionId);
        }

        _logger.LogInformation("Trial subscription {SubscriptionId} converted to active with payment type {PaymentType}",
            subscriptionId, paymentType);

        return Result.Success();
    }
}
