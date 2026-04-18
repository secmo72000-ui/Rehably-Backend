using Rehably.Application.Common;
using Rehably.Application.DTOs.Package;
using Rehably.Application.DTOs.Subscription;
using Rehably.Application.Services.Platform;
using Rehably.Application.Repositories;
using Rehably.Domain.Enums;
using System.Text.Json;

namespace Rehably.Infrastructure.Services.Platform;

public class PlatformSubscriptionService : IPlatformSubscriptionService
{
    private readonly ISubscriptionRepository _subscriptionRepository;

    public PlatformSubscriptionService(ISubscriptionRepository subscriptionRepository)
    {
        _subscriptionRepository = subscriptionRepository;
    }

    public async Task<Result<SubscriptionDto>> GetSubscriptionByIdAsync(Guid id)
    {
        var subscription = await _subscriptionRepository.GetWithPackageAndClinicAsync(id);

        if (subscription == null)
            return Result<SubscriptionDto>.Failure("Subscription not found");

        var dto = new SubscriptionDto
        {
            Id = subscription.Id,
            ClinicId = subscription.ClinicId,
            PackageId = subscription.PackageId,
            PackageName = subscription.Package?.Name ?? string.Empty,
            PackageCode = subscription.Package?.Code ?? string.Empty,
            Status = subscription.Status,
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            TrialEndsAt = subscription.TrialEndsAt,
            PaymentProvider = subscription.PaymentProvider,
            AutoRenew = subscription.AutoRenew,
            CreatedAt = subscription.CreatedAt,
            UpdatedAt = subscription.UpdatedAt,
            CancelledAt = subscription.CancelledAt,
            CancelReason = subscription.CancelReason,
            CustomerName = subscription.Clinic?.Name ?? string.Empty,
            CustomerEmail = subscription.Clinic?.Email ?? string.Empty,
            PhoneNumber = subscription.Clinic?.Phone ?? string.Empty
        };

        return Result<SubscriptionDto>.Success(dto);
    }

    public async Task<Result<SubscriptionDetailDto>> GetSubscriptionWithDetailsAsync(Guid id)
    {
        var subscription = await _subscriptionRepository.GetWithFeatureUsageAsync(id);

        if (subscription == null)
            return Result<SubscriptionDetailDto>.Failure("Subscription not found");

        PackageSnapshotDto? priceSnapshot = null;
        if (!string.IsNullOrEmpty(subscription.PriceSnapshot))
        {
            try
            {
                priceSnapshot = JsonSerializer.Deserialize<PackageSnapshotDto>(subscription.PriceSnapshot);
            }
            catch
            {
            }
        }

        var detail = new SubscriptionDetailDto
        {
            Id = subscription.Id,
            ClinicId = subscription.ClinicId,
            PackageId = subscription.PackageId,
            PackageName = subscription.Package?.Name ?? string.Empty,
            PackageCode = subscription.Package?.Code ?? string.Empty,
            Status = subscription.Status,
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            TrialEndsAt = subscription.TrialEndsAt,
            PriceSnapshot = priceSnapshot,
            PaymentProvider = subscription.PaymentProvider,
            ProviderSubscriptionId = subscription.ProviderSubscriptionId,
            AutoRenew = subscription.AutoRenew,
            CreatedAt = subscription.CreatedAt,
            UpdatedAt = subscription.UpdatedAt,
            CancelledAt = subscription.CancelledAt,
            CancelReason = subscription.CancelReason,
            FeatureUsage = subscription.FeatureUsage.Select(sf => new SubscriptionFeatureUsageDto
            {
                Id = sf.Id,
                FeatureId = sf.FeatureId,
                FeatureName = sf.Feature?.Name ?? string.Empty,
                FeatureCode = sf.Feature?.Code ?? string.Empty,
                Limit = sf.Limit,
                Used = sf.Used,
                LastResetAt = sf.LastResetAt
            }).ToList()
        };

        return Result<SubscriptionDetailDto>.Success(detail);
    }

    public async Task<Result<List<SubscriptionDto>>> GetSubscriptionsAsync(Guid? clinicId = null)
    {
        var subscriptions = await _subscriptionRepository.GetAllWithPackageAndClinicAsync(clinicId);

        var dtos = subscriptions.Select(x => new SubscriptionDto
        {
            Id = x.Id,
            ClinicId = x.ClinicId,
            PackageId = x.PackageId,
            PackageName = x.Package?.Name ?? string.Empty,
            PackageCode = x.Package?.Code ?? string.Empty,
            Status = x.Status,
            StartDate = x.StartDate,
            EndDate = x.EndDate,
            TrialEndsAt = x.TrialEndsAt,
            PaymentProvider = x.PaymentProvider,
            AutoRenew = x.AutoRenew,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            CancelledAt = x.CancelledAt,
            CancelReason = x.CancelReason,
            CustomerName = x.Clinic?.Name ?? string.Empty,
            CustomerEmail = x.Clinic?.Email ?? string.Empty,
            PhoneNumber = x.Clinic?.Phone ?? string.Empty
        }).ToList();

        return Result<List<SubscriptionDto>>.Success(dtos);
    }

    public async Task<Result<PagedResult<SubscriptionDto>>> GetSubscriptionsPagedAsync(int page, int pageSize, SubscriptionStatus? status = null, Guid? clinicId = null)
    {
        var (subscriptions, totalCount) = await _subscriptionRepository.GetPagedWithPackageAndClinicAsync(page, pageSize, status, clinicId);

        var dtos = subscriptions.Select(x => new SubscriptionDto
        {
            Id = x.Id,
            ClinicId = x.ClinicId,
            PackageId = x.PackageId,
            PackageName = x.Package?.Name ?? string.Empty,
            PackageCode = x.Package?.Code ?? string.Empty,
            Status = x.Status,
            StartDate = x.StartDate,
            EndDate = x.EndDate,
            TrialEndsAt = x.TrialEndsAt,
            PaymentProvider = x.PaymentProvider,
            AutoRenew = x.AutoRenew,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            CancelledAt = x.CancelledAt,
            CancelReason = x.CancelReason,
            CustomerName = x.Clinic?.Name ?? string.Empty,
            CustomerEmail = x.Clinic?.Email ?? string.Empty,
            PhoneNumber = x.Clinic?.Phone ?? string.Empty
        }).ToList();

        var pagedResult = new PagedResult<SubscriptionDto>(dtos, totalCount, page, pageSize);
        return Result<PagedResult<SubscriptionDto>>.Success(pagedResult);
    }
}
