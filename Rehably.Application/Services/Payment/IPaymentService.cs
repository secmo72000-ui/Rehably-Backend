using Rehably.Application.Common;
using Rehably.Application.DTOs.Payment;
using PaymentEntity = Rehably.Domain.Entities.Platform.Payment;

namespace Rehably.Application.Services.Payment;

public interface IPaymentService
{
    IPaymentProvider GetProvider(string? providerKey = null);

    void RegisterProvider(string key, IPaymentProvider provider);

    Task<Result<PaymentResponse>> CreateSubscriptionPaymentAsync(
        Guid clinicId,
        Guid subscriptionPlanId,
        string returnUrl,
        string cancelUrl,
        string? providerKey = null);

    Task<Result> ProcessPaymentCallbackAsync(
        string transactionId,
        string payload,
        string? providerKey = null);

    Task<Result> RefundTransactionAsync(Guid transactionId);

    Task<PaymentEntity?> GetTransactionAsync(Guid transactionId);

    Task<PaymentEntity?> GetLatestTransactionForClinicAsync(Guid clinicId);

    Task<Result<CashPaymentResult>> RecordCashPaymentAsync(
        decimal amount,
        string currency,
        string description,
        Guid? clinicId = null);
}
