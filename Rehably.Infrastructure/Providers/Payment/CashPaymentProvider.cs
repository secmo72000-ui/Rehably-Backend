using Rehably.Application.Common;
using Rehably.Application.DTOs.Payment;
using Rehably.Application.Services.Payment;
using Rehably.Infrastructure.Settings;

namespace Rehably.Infrastructure.Providers.Payment;

public class CashPaymentProvider : IPaymentProvider
{
    private readonly PaymentProviderConfig _config;

    public CashPaymentProvider(PaymentProviderConfig config)
    {
        _config = config;
    }

    public string Name => "Cash";

    public string Currency => _config.Currency ?? "USD";

    public Task<Result<PaymentInitiationResult>> CreatePaymentAsync(
        decimal amount,
        string currency,
        string description,
        string returnUrl,
        string cancelUrl,
        Dictionary<string, string>? metadata = null)
    {
        var transactionId = $"CASH-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        return Task.FromResult(
            Result<PaymentInitiationResult>.Success(new PaymentInitiationResult(null, transactionId)));
    }

    public Task<Result<PaymentVerificationResult>> VerifyPaymentAsync(string payload)
    {
        return Task.FromResult(
            Result<PaymentVerificationResult>.Success(new PaymentVerificationResult(null)));
    }

    public Task<Result> RefundAsync(string transactionId, decimal? amount = null)
    {
        return Task.FromResult(Result.Success());
    }

    public bool ValidateWebhookSignature(string payload, string signature, string secret)
    {
        return true;
    }
}
