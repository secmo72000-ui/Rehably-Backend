using Rehably.Application.Common;
using Rehably.Application.DTOs.Payment;

namespace Rehably.Application.Services.Payment;

public interface IPaymentProvider
{
    string Name { get; }
    string Currency { get; }

    Task<Result<PaymentInitiationResult>> CreatePaymentAsync(
        decimal amount,
        string currency,
        string description,
        string returnUrl,
        string cancelUrl,
        Dictionary<string, string>? metadata = null);

    Task<Result<PaymentVerificationResult>> VerifyPaymentAsync(string payload);

    Task<Result> RefundAsync(string transactionId, decimal? amount = null);

    bool ValidateWebhookSignature(string payload, string signature, string secret);
}
