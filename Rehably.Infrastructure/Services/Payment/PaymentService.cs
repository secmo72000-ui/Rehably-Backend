using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Payment;
using Rehably.Application.Repositories;
using Rehably.Application.Services;
using Rehably.Application.Services.Payment;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Settings;
using PaymentEntity = Rehably.Domain.Entities.Platform.Payment;

namespace Rehably.Infrastructure.Services.Payment;

public class PaymentService : IPaymentService
{
    private readonly Dictionary<string, IPaymentProvider> _providers = new();
    private readonly IPaymentRepository _paymentRepository;
    private readonly IClinicRepository _clinicRepository;
    private readonly IPackageRepository _packageRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IClinicOnboardingRepository _onboardingRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PaymentSettings _settings;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IEnumerable<IPaymentProvider> providers,
        IPaymentRepository paymentRepository,
        IClinicRepository clinicRepository,
        IPackageRepository packageRepository,
        ISubscriptionRepository subscriptionRepository,
        IClinicOnboardingRepository onboardingRepository,
        IUnitOfWork unitOfWork,
        IOptions<PaymentSettings> settings,
        ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _clinicRepository = clinicRepository;
        _packageRepository = packageRepository;
        _subscriptionRepository = subscriptionRepository;
        _onboardingRepository = onboardingRepository;
        _unitOfWork = unitOfWork;
        _settings = settings.Value;
        _logger = logger;

        foreach (var provider in providers)
        {
            _providers[provider.Name.ToLower()] = provider;
            _logger.LogInformation("Auto-registered payment provider: {Name}", provider.Name);
        }

        foreach (var providerConfig in _settings.Providers)
        {
            if (!_providers.ContainsKey(providerConfig.Key.ToLower()))
            {
                _logger.LogWarning("Provider {Key} is configured but not registered", providerConfig.Key);
            }
        }
    }

    public void RegisterProvider(string key, IPaymentProvider provider)
    {
        _providers[key] = provider;
        _logger.LogInformation("Registered payment provider: {Key}", key);
    }

    public IPaymentProvider GetProvider(string? providerKey = null)
    {
        var key = providerKey ?? _settings.DefaultProvider;

        if (!string.IsNullOrEmpty(key) && _providers.TryGetValue(key, out var provider))
        {
            return provider;
        }

        return _providers.Values.FirstOrDefault()
            ?? throw new InvalidOperationException("No payment provider registered");
    }

    public async Task<Result<PaymentResponse>> CreateSubscriptionPaymentAsync(
        Guid clinicId,
        Guid packageId,
        string returnUrl,
        string cancelUrl,
        string? providerKey = null)
    {
        try
        {
            var clinic = await _clinicRepository.GetByIdAsync(clinicId);
            if (clinic == null)
            {
                return Result<PaymentResponse>.Failure($"Clinic {clinicId} not found");
            }

            var package = await _packageRepository.GetByIdAsync(packageId);
            if (package == null || package.Status != PackageStatus.Active)
            {
                return Result<PaymentResponse>.Failure($"Package {packageId} not found or inactive");
            }

            var provider = GetProvider(providerKey);

            var metadata = new Dictionary<string, string>
            {
                ["clinic_id"] = clinicId.ToString(),
                ["package_id"] = packageId.ToString(),
                ["clinic_name"] = clinic.Name
            };

            var providerResult = await provider.CreatePaymentAsync(
                package.MonthlyPrice,
                provider.Currency,
                $"{package.Name} Subscription",
                returnUrl,
                cancelUrl,
                metadata);

            if (providerResult.IsFailure)
            {
                return Result<PaymentResponse>.Failure(providerResult.Error);
            }

            var initiation = providerResult.Value;

            var payment = new PaymentEntity
            {
                Id = Guid.NewGuid(),
                ClinicId = clinicId,
                Amount = package.MonthlyPrice,
                Currency = provider.Currency,
                Provider = ParseProvider(providerKey),
                ProviderTransactionId = initiation.TransactionId,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _paymentRepository.AddAsync(payment);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Created payment transaction {TransactionId} for clinic {ClinicId}",
                payment.Id, clinicId);

            return Result<PaymentResponse>.Success(new PaymentResponse
            {
                TransactionId = payment.Id.ToString(),
                PaymentUrl = initiation.PaymentUrl,
                Message = "Payment created. Redirect to payment URL to complete payment."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment for clinic {ClinicId}", clinicId);
            return Result<PaymentResponse>.Failure("An error occurred while creating the payment. Please try again.");
        }
    }

    public async Task<Result> ProcessPaymentCallbackAsync(
        string transactionId,
        string payload,
        string? providerKey = null)
    {
        try
        {
            var payment = await _paymentRepository.GetByTransactionIdAsync(transactionId);

            if (payment == null)
            {
                return Result.Failure($"Transaction {transactionId} not found");
            }

            if (payment.Status == PaymentStatus.Completed)
            {
                return Result.Success();
            }

            var provider = GetProvider(providerKey);
            var verifyResult = await provider.VerifyPaymentAsync(payload);

            if (verifyResult.IsFailure)
            {
                payment.Status = PaymentStatus.Failed;
                await _unitOfWork.SaveChangesAsync();
                return Result.Failure(verifyResult.Error);
            }

            var verification = verifyResult.Value;

            payment.Status = PaymentStatus.Completed;
            payment.ProviderTransactionId = verification.TransactionId ?? payment.ProviderTransactionId;
            payment.ProcessedAt = DateTime.UtcNow;

            await HandleSuccessfulPaymentAsync(payment);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Payment processed successfully: {TransactionId}", transactionId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment callback for {TransactionId}", transactionId);
            return Result.Failure("An error occurred while processing the payment. Please contact support.");
        }
    }

    public async Task<Result> RefundTransactionAsync(Guid transactionId)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(transactionId);

            if (payment == null)
            {
                return Result.Failure($"Transaction {transactionId} not found");
            }

            if (!payment.CanRefund())
            {
                return Result.Failure("Only completed transactions can be refunded");
            }

            var provider = GetProvider(GetProviderKey(payment.Provider));
            var refundResult = await provider.RefundAsync(
                payment.ProviderTransactionId ?? string.Empty,
                payment.Amount);

            if (refundResult.IsFailure)
            {
                return Result.Failure(refundResult.Error);
            }

            payment.Refund();
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Transaction refunded: {TransactionId}", transactionId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refunding transaction {TransactionId}", transactionId);
            return Result.Failure("An error occurred while processing the refund. Please contact support.");
        }
    }

    public async Task<PaymentEntity?> GetTransactionAsync(Guid transactionId)
    {
        return await _paymentRepository.GetByIdAsync(transactionId);
    }

    public async Task<PaymentEntity?> GetLatestTransactionForClinicAsync(Guid clinicId)
    {
        return await _paymentRepository.GetLatestByClinicIdAsync(clinicId);
    }

    public async Task<Result<CashPaymentResult>> RecordCashPaymentAsync(
        decimal amount,
        string currency,
        string description,
        Guid? clinicId = null)
    {
        try
        {
            if (clinicId.HasValue)
            {
                var clinic = await _clinicRepository.GetByIdAsync(clinicId.Value);
                if (clinic == null)
                {
                    return Result<CashPaymentResult>.Failure($"Clinic {clinicId} not found");
                }
            }

            var cashProvider = GetProvider("cash");
            var providerResult = await cashProvider.CreatePaymentAsync(
                amount,
                currency,
                description,
                string.Empty,
                string.Empty,
                null);

            if (providerResult.IsFailure)
            {
                return Result<CashPaymentResult>.Failure(providerResult.Error);
            }

            var initiation = providerResult.Value;

            var payment = new PaymentEntity
            {
                Id = Guid.NewGuid(),
                ClinicId = clinicId ?? Guid.Empty,
                Amount = amount,
                Currency = currency,
                Provider = PaymentProvider.Cash,
                ProviderTransactionId = initiation.TransactionId,
                Status = PaymentStatus.Completed,
                ProcessedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            await _paymentRepository.AddAsync(payment);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Cash payment recorded: {TransactionId} for clinic {ClinicId}",
                payment.Id, clinicId ?? Guid.Empty);

            return Result<CashPaymentResult>.Success(new CashPaymentResult(payment.Id.ToString()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording cash payment for clinic {ClinicId}", clinicId);
            return Result<CashPaymentResult>.Failure("An error occurred while recording the payment. Please try again.");
        }
    }

    private async Task HandleSuccessfulPaymentAsync(PaymentEntity payment)
    {
        var subscription = await _subscriptionRepository.FirstOrDefaultAsync(s => s.ClinicId == payment.ClinicId);

        if (subscription != null)
        {
            subscription.Status = SubscriptionStatus.Active;
            _logger.LogInformation("Subscription activated for clinic {ClinicId}", payment.ClinicId);
        }

        var onboarding = await _onboardingRepository.GetByClinicIdAndStepAsync(payment.ClinicId, OnboardingStep.PendingPayment);

        if (onboarding != null)
        {
            onboarding.CurrentStep = OnboardingStep.Completed;
            onboarding.PaymentCompletedAt = DateTime.UtcNow;

            var clinic = await _clinicRepository.GetByIdAsync(payment.ClinicId);
            if (clinic != null)
            {
                clinic.Status = ClinicStatus.Active;
                clinic.ActivatedAt = DateTime.UtcNow;
                _logger.LogInformation("Clinic {ClinicId} activated and onboarding completed", payment.ClinicId);
            }
        }
    }

    private static PaymentProvider ParseProvider(string? providerKey)
    {
        return providerKey?.ToLower() switch
        {
            "paymob" => PaymentProvider.PayMob,
            "stripe" => PaymentProvider.Stripe,
            "cash" => PaymentProvider.Cash,
            _ => PaymentProvider.Cash
        };
    }

    private static string GetProviderKey(PaymentProvider provider)
    {
        return provider switch
        {
            PaymentProvider.PayMob => "paymob",
            PaymentProvider.Stripe => "stripe",
            PaymentProvider.Cash => "cash",
            _ => "cash"
        };
    }
}
