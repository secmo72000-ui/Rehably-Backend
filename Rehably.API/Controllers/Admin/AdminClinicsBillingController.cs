using Microsoft.AspNetCore.Mvc;
using Rehably.API.Authorization;
using Rehably.Application.DTOs.Platform;
using Rehably.Application.DTOs.Clinic;
using Rehably.Application.DTOs.Subscription;
using Rehably.Application.Services.Platform;
using Rehably.Application.Services.Auth;
using Rehably.Application.Services.Payment;
using Rehably.Application.Services.Clinic;
using Rehably.Domain.Enums;

namespace Rehably.API.Controllers.Admin;

/// <summary>
/// Manages clinic billing, subscriptions, and payment operations.
/// </summary>
[ApiController]
[Route("api/admin/clinics")]
[RequirePermission("platform.manage_clinics")]
[Produces("application/json")]
[Tags("Admin - Clinics")]
public class AdminClinicsBillingController : BaseController
{
    private readonly IClinicService _clinicService;
    private readonly IPaymentService _paymentService;
    private readonly IPlatformSubscriptionService _subscriptionService;
    private readonly ISubscriptionLifecycleService _lifecycleService;
    private readonly ISubscriptionModificationService _modificationService;
    private readonly IAuthService _authService;
    private readonly IAuthPasswordService _authPasswordService;

    public AdminClinicsBillingController(IClinicService clinicService, IPaymentService paymentService, IPlatformSubscriptionService subscriptionService, ISubscriptionLifecycleService lifecycleService, ISubscriptionModificationService modificationService, IAuthService authService, IAuthPasswordService authPasswordService)
    {
        _clinicService = clinicService;
        _paymentService = paymentService;
        _subscriptionService = subscriptionService;
        _lifecycleService = lifecycleService;
        _modificationService = modificationService;
        _authService = authService;
        _authPasswordService = authPasswordService;
    }

    /// <summary>
    /// Update an active clinic subscription to a new package.
    /// </summary>
    /// <param name="id">The clinic ID.</param>
    /// <param name="request">The subscription update request containing the new package ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Subscription updated successfully.</response>
    /// <response code="400">Clinic has no active subscription or invalid package.</response>
    [HttpPut("{id:guid}/subscription")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdateSubscription(Guid id, [FromBody] UpdateSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        var subscriptionsResult = await _subscriptionService.GetSubscriptionsAsync(id);
        if (!subscriptionsResult.IsSuccess || subscriptionsResult.Value == null)
            return ValidationError("No subscriptions found for this clinic");

        var activeSubscription = subscriptionsResult.Value.FirstOrDefault(s => s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial);
        if (activeSubscription == null)
            return ValidationError("No active subscription found for this clinic");

        var upgradeResult = await _modificationService.UpgradeSubscriptionAsync(activeSubscription.Id, new UpgradeSubscriptionRequestDto { NewPackageId = request.NewPackageId });
        if (!upgradeResult.IsSuccess || upgradeResult.Value == null)
            return ValidationError(upgradeResult.Error ?? "Failed to upgrade subscription");

        return Success(new
        {
            message = "Subscription updated successfully",
            subscription = new
            {
                id = upgradeResult.Value.Id,
                status = upgradeResult.Value.Status.ToString(),
                packageId = upgradeResult.Value.PackageId,
                packageName = upgradeResult.Value.PackageName,
                priceSnapshot = upgradeResult.Value.PriceSnapshot
            }
        });
    }

    /// <summary>
    /// Activate a clinic with a cash payment subscription.
    /// </summary>
    /// <remarks>
    /// Orchestrates clinic activation with cash payment: creates subscription, records cash payment, activates clinic, and sends welcome email.
    /// </remarks>
    /// <param name="id">The clinic ID.</param>
    /// <param name="request">The cash activation request containing the package ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Clinic activated successfully with subscription and payment recorded.</response>
    /// <response code="400">Failed to create subscription, record payment, or activate clinic.</response>
    [HttpPost("{id:guid}/activate-cash")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ActivateWithCashPayment(Guid id, [FromBody] ActivateCashRequest request, CancellationToken cancellationToken = default)
    {
        var subscriptionResult = await _lifecycleService.CreateSubscriptionAsync(new CreateSubscriptionRequestDto { ClinicId = id, PackageId = request.PackageId, AutoRenew = true, PaymentProvider = "cash" });
        if (!subscriptionResult.IsSuccess || subscriptionResult.Value == null)
            return ValidationError(subscriptionResult.Error ?? "Failed to create subscription");

        var price = subscriptionResult.Value.PriceSnapshot?.MonthlyPrice ?? 0m;
        var cashCurrency = _paymentService.GetProvider("cash").Currency;
        var paymentResult = await _paymentService.RecordCashPaymentAsync(price, cashCurrency, $"Cash payment activation for clinic {id}", id);
        if (paymentResult.IsFailure)
            return ValidationError(paymentResult.Error ?? "Failed to record cash payment");

        var activateResult = await _clinicService.ActivateClinicAsync(id);
        if (activateResult.IsFailure)
            return ValidationError(activateResult.Error ?? "Failed to activate clinic");

        var clinicResult = await _clinicService.GetClinicByIdAsync(id);
        if (clinicResult.IsSuccess && clinicResult.Data != null)
        {
            var token = await _authPasswordService.GeneratePasswordResetTokenAsync(clinicResult.Data.Email ?? string.Empty);
            await _authService.SendWelcomeEmailAsync(clinicResult.Data.Email ?? string.Empty, token, clinicResult.Data.Name, "Clinic Owner");
        }

        return Success(new
        {
            message = "Clinic activated successfully with cash payment",
            subscription = FormatSubscription(subscriptionResult.Value),
            paymentTransactionId = paymentResult.Value.TransactionId
        });
    }

    private static object FormatSubscription(SubscriptionDetailDto sub) => new { id = sub.Id, status = sub.Status.ToString(), startDate = sub.StartDate, endDate = sub.EndDate, trialEndsAt = sub.TrialEndsAt, packageId = sub.PackageId, packageName = sub.PackageName, priceSnapshot = sub.PriceSnapshot, featureUsage = sub.FeatureUsage };
}
