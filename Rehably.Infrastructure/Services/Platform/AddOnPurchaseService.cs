using Mapster;
using Microsoft.Extensions.Logging;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Subscription;
using Rehably.Application.DTOs.Platform;
using Rehably.Application.Interfaces;
using Rehably.Application.Services.Platform;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Enums;
using System.Text.Json;

namespace Rehably.Infrastructure.Services.Platform;

public class AddOnPurchaseService : IAddOnPurchaseService
{
    private readonly IFeatureRepository _featureRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ISubscriptionAddOnRepository _addOnRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddOnPurchaseService> _logger;
    private readonly IClock _clock;

    public AddOnPurchaseService(
        IFeatureRepository featureRepository,
        ISubscriptionRepository subscriptionRepository,
        ISubscriptionAddOnRepository addOnRepository,
        IUnitOfWork unitOfWork,
        ILogger<AddOnPurchaseService> logger,
        IClock clock)
    {
        _featureRepository = featureRepository;
        _subscriptionRepository = subscriptionRepository;
        _addOnRepository = addOnRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _clock = clock;
    }

    public async Task<Result<PurchaseAddOnResponseDto>> PurchaseAddOnAsync(Guid clinicId, PurchaseAddOnRequestDto request)
    {
        await _unitOfWork.BeginTransactionAsync();

        try
        {
            var activeSubscription = await _subscriptionRepository.GetActiveSubscriptionByClinicIdAsync(clinicId);
            if (activeSubscription == null)
            {
                return Result<PurchaseAddOnResponseDto>.Failure("No active subscription found for clinic");
            }

            var feature = await _featureRepository.GetAddOnWithPricingAsync(request.FeatureId);
            if (feature == null)
            {
                return Result<PurchaseAddOnResponseDto>.Failure("Feature not found or is not available as an add-on");
            }

            var existingAddOn = await _addOnRepository.GetBySubscriptionAndFeatureAsync(activeSubscription.Id, request.FeatureId);
            if (existingAddOn != null)
            {
                return Result<PurchaseAddOnResponseDto>.Failure("This add-on is already active for your subscription");
            }

            var now = _clock.UtcNow;
            var currentPricing = feature.PricingHistory
                .Where(p => p.IsActive && p.EffectiveDate <= now && (p.ExpiryDate == null || p.ExpiryDate > now))
                .OrderByDescending(p => p.EffectiveDate)
                .FirstOrDefault();

            if (currentPricing == null)
            {
                return Result<PurchaseAddOnResponseDto>.Failure("No active pricing found for this feature");
            }

            var calculatedPrice = CalculatePrice(currentPricing, feature.PricingType, request.Quantity);

            var priceSnapshot = JsonSerializer.Serialize(new
            {
                basePrice = currentPricing.BasePrice,
                perUnitPrice = currentPricing.PerUnitPrice,
                pricingType = feature.PricingType.ToString(),
                quantity = request.Quantity,
                calculatedAt = now
            });

            var addOn = new SubscriptionAddOn
            {
                SubscriptionId = activeSubscription.Id,
                FeatureId = request.FeatureId,
                Quantity = request.Quantity,
                CalculatedPrice = calculatedPrice,
                PriceSnapshot = priceSnapshot,
                Status = AddOnStatus.Active,
                StartDate = now,
                EndDate = activeSubscription.EndDate,
                CreatedAt = now
            };

            await _addOnRepository.AddAsync(addOn);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Add-on {FeatureId} purchased for clinic {ClinicId} with quantity {Quantity}",
                request.FeatureId, clinicId, request.Quantity);

            addOn.Feature = feature;
            var response = new PurchaseAddOnResponseDto
            {
                AddOn = addOn.Adapt<SubscriptionAddOnDto>(),
                Payment = new PaymentInfoDto
                {
                    Amount = calculatedPrice,
                    Description = $"Add-on: {feature.Name} x {request.Quantity}",
                    TransactionId = null
                }
            };

            return Result<PurchaseAddOnResponseDto>.Success(response);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Failed to purchase add-on {FeatureId} for clinic {ClinicId}", request.FeatureId, clinicId);
            return Result<PurchaseAddOnResponseDto>.Failure("Failed to purchase add-on");
        }
    }

    public async Task<Result<SubscriptionAddOnDto>> UpgradeAddOnAsync(Guid clinicId, Guid addOnId, int newQuantity)
    {
        await _unitOfWork.BeginTransactionAsync();

        try
        {
            var addOn = await _addOnRepository.GetWithSubscriptionAndFeatureAsync(addOnId);
            if (addOn == null || addOn.Subscription.ClinicId != clinicId)
            {
                return Result<SubscriptionAddOnDto>.Failure("Add-on not found");
            }

            if (addOn.Status != AddOnStatus.Active)
            {
                return Result<SubscriptionAddOnDto>.Failure("Can only upgrade active add-ons");
            }

            if (newQuantity <= addOn.Quantity)
            {
                return Result<SubscriptionAddOnDto>.Failure("New quantity must be greater than current quantity");
            }

            var now = _clock.UtcNow;
            var currentPricing = addOn.Feature.PricingHistory
                .Where(p => p.IsActive && p.EffectiveDate <= now && (p.ExpiryDate == null || p.ExpiryDate > now))
                .OrderByDescending(p => p.EffectiveDate)
                .FirstOrDefault();

            if (currentPricing == null)
            {
                return Result<SubscriptionAddOnDto>.Failure("No active pricing found for this feature");
            }

            var newPrice = CalculatePrice(currentPricing, addOn.Feature.PricingType, newQuantity);

            var priceSnapshot = JsonSerializer.Serialize(new
            {
                basePrice = currentPricing.BasePrice,
                perUnitPrice = currentPricing.PerUnitPrice,
                pricingType = addOn.Feature.PricingType.ToString(),
                quantity = newQuantity,
                previousQuantity = addOn.Quantity,
                calculatedAt = now
            });

            var oldQuantity = addOn.Quantity;
            addOn.Quantity = newQuantity;
            addOn.CalculatedPrice = newPrice;
            addOn.PriceSnapshot = priceSnapshot;
            addOn.UpdatedAt = now;

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

       _logger.LogInformation("Add-on {AddOnId} upgraded from {OldQty} to {NewQty} for clinic {ClinicId}",
    addOnId, oldQuantity, newQuantity, clinicId);


            return Result<SubscriptionAddOnDto>.Success(addOn.Adapt<SubscriptionAddOnDto>());
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Failed to upgrade add-on {AddOnId} for clinic {ClinicId}", addOnId, clinicId);
            return Result<SubscriptionAddOnDto>.Failure("Failed to upgrade add-on");
        }
    }

    public async Task<Result> CancelAddOnAsync(Guid clinicId, Guid addOnId)
    {
        await _unitOfWork.BeginTransactionAsync();

        try
        {
            var addOn = await _addOnRepository.GetWithSubscriptionAndFeatureAsync(addOnId);
            if (addOn == null || addOn.Subscription.ClinicId != clinicId)
            {
                return Result.Failure("Add-on not found");
            }

            if (addOn.Status == AddOnStatus.Cancelled)
            {
                return Result.Failure("Add-on is already cancelled");
            }

            addOn.Status = AddOnStatus.Cancelled;
            addOn.CancelledAt = _clock.UtcNow;
            addOn.UpdatedAt = _clock.UtcNow;

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Add-on {AddOnId} cancelled for clinic {ClinicId}", addOnId, clinicId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Failed to cancel add-on {AddOnId} for clinic {ClinicId}", addOnId, clinicId);
            return Result.Failure("Failed to cancel add-on");
        }
    }

    private static decimal CalculatePrice(FeaturePricing pricing, PricingType pricingType, int quantity)
    {
        return pricingType switch
        {
            PricingType.Fixed => pricing.BasePrice,
            PricingType.PerUser => pricing.PerUnitPrice * quantity,
            PricingType.PerStorageGB => pricing.PerUnitPrice * quantity,
            PricingType.PerUnit => pricing.PerUnitPrice * quantity,
            _ => pricing.BasePrice
        };
    }

}
