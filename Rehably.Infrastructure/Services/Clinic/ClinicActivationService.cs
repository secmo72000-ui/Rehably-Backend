using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Clinic;
using Rehably.Application.DTOs.Subscription;
using Rehably.Application.Services.Auth;
using Rehably.Application.Services.Clinic;
using Rehably.Application.Services.Payment;
using Rehably.Application.Services.Platform;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Settings;

namespace Rehably.Infrastructure.Services.Clinic;

/// <summary>
/// Orchestrates the clinic activation saga:
/// 1) RegisterClinicAsync
/// 2) CreateSubscriptionAsync
/// 3a) Online: Create Stripe checkout session → return PaymentUrl (clinic activated via webhook)
/// 3b) Cash: Record cash payment → ActivateClinicAsync → SendWelcomeEmailAsync
/// 3c) Free: ActivateClinicAsync → SendWelcomeEmailAsync
/// Fail-fast on any step — no rollback.
/// </summary>
public class ClinicActivationService : IClinicActivationService
{
    private readonly IClinicService _clinicService;
    private readonly ISubscriptionLifecycleService _lifecycleService;
    private readonly IPaymentService _paymentService;
    private readonly IAuthService _authService;
    private readonly IAuthPasswordService _authPasswordService;
    private readonly AppSettings _appSettings;
    private readonly ILogger<ClinicActivationService> _logger;

    public ClinicActivationService(
        IClinicService clinicService,
        ISubscriptionLifecycleService lifecycleService,
        IPaymentService paymentService,
        IAuthService authService,
        IAuthPasswordService authPasswordService,
        IOptions<AppSettings> appSettings,
        ILogger<ClinicActivationService> logger)
    {
        _clinicService = clinicService;
        _lifecycleService = lifecycleService;
        _paymentService = paymentService;
        _authService = authService;
        _authPasswordService = authPasswordService;
        _appSettings = appSettings.Value;
        _logger = logger;
    }

    public async Task<Result<ClinicCreatedDto>> ActivateNewClinicAsync(
        CreateClinicRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var clinicResult = await _clinicService.CreateClinicAsync(request);
            if (clinicResult.IsFailure || clinicResult.Value == null)
            {
                _logger.LogWarning("Clinic creation failed: {Error}", clinicResult.Error);
                return Result<ClinicCreatedDto>.Failure(clinicResult.Error ?? "Failed to create clinic");
            }

            var clinic = clinicResult.Value;
            var providerKey = request.PaymentType == PaymentType.Online ? "stripe" : "cash";

            var subscriptionResult = await _lifecycleService.CreateSubscriptionAsync(new CreateSubscriptionRequestDto
            {
                ClinicId = clinic.Id,
                PackageId = request.PackageId,
                AutoRenew = true,
                PaymentProvider = providerKey,
                BillingCycle = request.BillingCycle,
                StartDate = request.SubscriptionStartDate,
                EndDate = request.SubscriptionEndDate
            });

            if (!subscriptionResult.IsSuccess || subscriptionResult.Value == null)
            {
                _logger.LogWarning("Subscription creation failed for clinic {ClinicId}: {Error}", clinic.Id, subscriptionResult.Error);
                return Result<ClinicCreatedDto>.Failure(subscriptionResult.Error ?? "Failed to create subscription");
            }

            var subscription = subscriptionResult.Value;

            // --- Online (Stripe) path: create checkout session and return URL ---
            if (request.PaymentType == PaymentType.Online)
            {
                var returnUrl = $"{_appSettings.FrontendUrl}/payment/success?clinicId={clinic.Id}";
                var cancelUrl = $"{_appSettings.FrontendUrl}/payment/cancel?clinicId={clinic.Id}";

                var stripeResult = await _paymentService.CreateSubscriptionPaymentAsync(
                    clinic.Id,
                    request.PackageId,
                    returnUrl,
                    cancelUrl,
                    "stripe");

                if (stripeResult.IsFailure)
                {
                    _logger.LogWarning("Stripe checkout creation failed for clinic {ClinicId}: {Error}", clinic.Id, stripeResult.Error);
                    return Result<ClinicCreatedDto>.Failure(stripeResult.Error ?? "Failed to create Stripe payment");
                }

                _logger.LogInformation("Stripe checkout created for clinic {ClinicId}, awaiting payment", clinic.Id);

                return Result<ClinicCreatedDto>.Success(new ClinicCreatedDto
                {
                    Id = clinic.Id,
                    Name = clinic.Name,
                    Slug = clinic.Slug,
                    Email = clinic.Email ?? string.Empty,
                    Phone = clinic.Phone,
                    Status = clinic.Status,
                    CreatedAt = clinic.CreatedAt,
                    SubscriptionId = subscription.Id,
                    PackageName = subscription.PackageName,
                    SubscriptionStatus = subscription.Status,
                    SubscriptionStartDate = subscription.StartDate,
                    SubscriptionEndDate = subscription.EndDate,
                    PaymentType = request.PaymentType.ToString(),
                    PaymentTransactionId = stripeResult.Value.TransactionId,
                    PaymentUrl = stripeResult.Value.PaymentUrl
                });
            }

            // --- Cash / Free path: record payment (cash only), activate, send welcome email ---
            string? paymentTransactionId = null;

            if (request.PaymentType == PaymentType.Cash)
            {
                var priceSnapshot = subscription.PriceSnapshot;
                var price = request.BillingCycle == BillingCycle.Yearly
                    ? (priceSnapshot?.YearlyPrice ?? priceSnapshot?.MonthlyPrice ?? 0m)
                    : (priceSnapshot?.MonthlyPrice ?? 0m);

                var cashCurrency = _paymentService.GetProvider("cash").Currency;
                var paymentResult = await _paymentService.RecordCashPaymentAsync(
                    price,
                    cashCurrency,
                    $"Admin-created clinic activation for {clinic.Name}",
                    clinic.Id);

                if (paymentResult.IsFailure)
                {
                    _logger.LogWarning("Payment recording failed for clinic {ClinicId}: {Error}", clinic.Id, paymentResult.Error);
                    return Result<ClinicCreatedDto>.Failure(paymentResult.Error ?? "Failed to record payment");
                }

                paymentTransactionId = paymentResult.Value.TransactionId;
            }

            var activateResult = await _clinicService.ActivateClinicAsync(clinic.Id);
            if (activateResult.IsFailure)
            {
                _logger.LogWarning("Clinic activation failed for {ClinicId}: {Error}", clinic.Id, activateResult.Error);
                return Result<ClinicCreatedDto>.Failure(activateResult.Error ?? "Failed to activate clinic");
            }

            try
            {
                var ownerEmail = clinic.Email ?? string.Empty;
                var token = await _authPasswordService.GeneratePasswordResetTokenAsync(ownerEmail);
                await _authService.SendWelcomeEmailAsync(ownerEmail, token, clinic.Name, "Clinic Owner");
            }
            catch (Exception emailEx)
            {
                _logger.LogWarning(emailEx, "Welcome email failed for clinic {ClinicId} — continuing", clinic.Id);
            }

            _logger.LogInformation("Clinic {ClinicId} activated successfully via {PaymentType}", clinic.Id, request.PaymentType);

            return Result<ClinicCreatedDto>.Success(new ClinicCreatedDto
            {
                Id = clinic.Id,
                Name = clinic.Name,
                Slug = clinic.Slug,
                Email = clinic.Email ?? string.Empty,
                Phone = clinic.Phone,
                Status = clinic.Status,
                CreatedAt = clinic.CreatedAt,
                SubscriptionId = subscription.Id,
                PackageName = subscription.PackageName,
                SubscriptionStatus = subscription.Status,
                SubscriptionStartDate = subscription.StartDate,
                SubscriptionEndDate = subscription.EndDate,
                PaymentType = request.PaymentType.ToString(),
                PaymentTransactionId = paymentTransactionId,
                PaymentReference = request.PaymentReference
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error during clinic activation");
            return Result<ClinicCreatedDto>.Failure("An unexpected error occurred during clinic activation");
        }
    }
}
