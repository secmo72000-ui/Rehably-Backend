using Mapster;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Payment;
using Rehably.Application.DTOs.Platform;
using Rehably.Application.Services.Platform;
using Rehably.Application.Repositories;
using Rehably.Application.Services.Payment;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Settings;
using System.Text.Json;
using DomainPayment = Rehably.Domain.Entities.Platform.Payment;

namespace Rehably.Infrastructure.Services.Platform;

public class SubscriptionPaymentService : ISubscriptionPaymentService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentService _paymentService;
    private readonly PaymentSettings _settings;
    private readonly ILogger<SubscriptionPaymentService> _logger;

    public SubscriptionPaymentService(
        IInvoiceRepository invoiceRepository,
        IPaymentRepository paymentRepository,
        ISubscriptionRepository subscriptionRepository,
        IUnitOfWork unitOfWork,
        IPaymentService paymentService,
        IOptions<PaymentSettings> settings,
        ILogger<SubscriptionPaymentService> logger)
    {
        _invoiceRepository = invoiceRepository;
        _paymentRepository = paymentRepository;
        _subscriptionRepository = subscriptionRepository;
        _unitOfWork = unitOfWork;
        _paymentService = paymentService;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<Result<PaymentDto>> CreatePaymentAsync(Guid invoiceId, string provider, decimal amount, string currency)
    {
        var invoice = await _invoiceRepository.GetWithLineItemsAsync(invoiceId);

        if (invoice == null)
            return Result<PaymentDto>.Failure("Invoice not found");

        if (invoice.PaidAt != null)
            return Result<PaymentDto>.Failure("Invoice already paid");

        var providerKey = provider.ToLower();
        if (!_settings.Providers.Any(p => p.Key.ToLower() == providerKey))
            return Result<PaymentDto>.Failure($"Payment provider {provider} not configured");

        var payment = new DomainPayment
        {
            ClinicId = invoice.ClinicId,
            InvoiceId = invoice.Id,
            Amount = amount,
            Currency = currency,
            Provider = Enum.Parse<PaymentProvider>(provider, true),
            Status = PaymentStatus.Pending,
            Metadata = JsonSerializer.Serialize(new
            {
                invoice_id = invoice.Id,
                invoice_number = invoice.InvoiceNumber,
                subscription_id = invoice.SubscriptionId
            })
        };

        await _paymentRepository.AddAsync(payment);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Created payment {PaymentId} for invoice {InvoiceId}", payment.Id, invoiceId);
        return Result<PaymentDto>.Success(payment.Adapt<PaymentDto>());
    }

    public async Task<Result<PaymentDto>> GetPaymentAsync(Guid paymentId)
    {
        var payment = await _paymentRepository.GetWithInvoiceAsync(paymentId);

        if (payment == null)
            return Result<PaymentDto>.Failure("Payment not found");

        return Result<PaymentDto>.Success(payment.Adapt<PaymentDto>());
    }

    public async Task<Result<List<PaymentDto>>> GetInvoicePaymentsAsync(Guid invoiceId)
    {
        var payments = await _paymentRepository.GetByInvoiceIdAsync(invoiceId);
        var dtos = payments.Select(p => p.Adapt<PaymentDto>()).ToList();
        return Result<List<PaymentDto>>.Success(dtos);
    }

    public async Task<Result<PaymentDto>> ProcessPaymentCallbackAsync(Guid paymentId, string payload)
    {
        var payment = await _paymentRepository.GetWithInvoiceAsync(paymentId);

        if (payment == null)
            return Result<PaymentDto>.Failure("Payment not found");

        if (payment.Status == PaymentStatus.Completed)
            return Result<PaymentDto>.Success(payment.Adapt<PaymentDto>());

        payment.Status = PaymentStatus.Processing;
        await _unitOfWork.SaveChangesAsync();

        try
        {
            var provider = _paymentService.GetProvider(payment.Provider.ToString());

            var verifyResult = await provider.VerifyPaymentAsync(payload);

            if (verifyResult.IsFailure)
            {
                payment.Status = PaymentStatus.Failed;
                payment.FailureReason = verifyResult.Error ?? "Payment verification failed";
                payment.ProcessedAt = DateTime.UtcNow;
                await _unitOfWork.SaveChangesAsync();

                _logger.LogWarning("Payment {PaymentId} failed: {Error}", paymentId, payment.FailureReason);
                return Result<PaymentDto>.Failure(payment.FailureReason);
            }

            payment.Status = PaymentStatus.Completed;
            payment.ProviderTransactionId = verifyResult.Value.TransactionId ?? payment.ProviderTransactionId;
            payment.ProcessedAt = DateTime.UtcNow;
            payment.UpdatedAt = DateTime.UtcNow;

            await HandleSuccessfulPaymentAsync(payment);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Payment {PaymentId} completed successfully", paymentId);
            return Result<PaymentDto>.Success(payment.Adapt<PaymentDto>());
        }
        catch (Exception ex)
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = ex.Message;
            payment.ProcessedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();

            _logger.LogError(ex, "Error processing payment {PaymentId}", paymentId);
            return Result<PaymentDto>.Failure("Payment processing error");
        }
    }

    public async Task<Result<PaymentDto>> RefundPaymentAsync(Guid paymentId, decimal? amount = null)
    {
        var payment = await _paymentRepository.GetWithInvoiceAsync(paymentId);

        if (payment == null)
            return Result<PaymentDto>.Failure("Payment not found");

        if (payment.Status != PaymentStatus.Completed)
            return Result<PaymentDto>.Failure("Only completed payments can be refunded");

        var refundAmount = amount ?? payment.Amount;

        try
        {
            var provider = _paymentService.GetProvider(payment.Provider.ToString());
            var refundResult = await provider.RefundAsync(
                payment.ProviderTransactionId ?? string.Empty,
                refundAmount);

            if (refundResult.IsFailure)
            {
                _logger.LogWarning("Refund failed for payment {PaymentId}: {Error}", paymentId, refundResult.Error);
                return Result<PaymentDto>.Failure(refundResult.Error ?? "Refund failed");
            }

            if (amount.HasValue && amount < payment.Amount)
            {
                payment.Status = PaymentStatus.PartiallyRefunded;
            }
            else
            {
                payment.Status = PaymentStatus.Refunded;
            }

            payment.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Payment {PaymentId} refunded successfully", paymentId);
            return Result<PaymentDto>.Success(payment.Adapt<PaymentDto>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refunding payment {PaymentId}", paymentId);
            return Result<PaymentDto>.Failure("Refund error");
        }
    }

    private async Task HandleSuccessfulPaymentAsync(DomainPayment payment)
    {
        var invoice = payment.Invoice;
        if (invoice == null) return;

        var totalPaid = await _paymentRepository.GetTotalPaidByInvoiceAsync(invoice.Id);

        if (totalPaid >= invoice.TotalAmount)
        {
            invoice.PaidAt = DateTime.UtcNow;
            invoice.PaidVia = payment.Provider.ToString();
            invoice.UpdatedAt = DateTime.UtcNow;

            var subscription = await _subscriptionRepository.GetByIdAsync(invoice.SubscriptionId);

            if (subscription != null && subscription.EndDate <= DateTime.UtcNow)
            {
                var extensionDays = subscription.BillingCycle == BillingCycle.Yearly ? 365 : 30;
                subscription.EndDate = subscription.EndDate.AddDays(extensionDays);
                subscription.Status = SubscriptionStatus.Active;
                subscription.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation("Extended subscription {SubscriptionId} to {EndDate}",
                    subscription.Id, subscription.EndDate);
            }
        }
    }

}
