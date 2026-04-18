using Stripe;
using Microsoft.Extensions.Logging;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Payment;
using Rehably.Application.Services.Payment;
using Rehably.Infrastructure.Settings;

namespace Rehably.Infrastructure.Providers.Payment;

public class StripePaymentProvider : IPaymentProvider
{
    private readonly PaymentProviderConfig _config;
    private readonly ILogger<StripePaymentProvider> _logger;

    public string Name => "Stripe";
    public string Currency => _config.Currency ?? "USD";

    public StripePaymentProvider(PaymentProviderConfig config, ILogger<StripePaymentProvider> logger)
    {
        _config = config;
        _logger = logger;

        if (!string.IsNullOrEmpty(_config.ApiSecret))
        {
            StripeConfiguration.ApiKey = _config.ApiSecret;
        }
    }

    public async Task<Result<PaymentInitiationResult>> CreatePaymentAsync(
        decimal amount,
        string currency,
        string description,
        string returnUrl,
        string cancelUrl,
        Dictionary<string, string>? metadata = null)
    {
        try
        {
            if (string.IsNullOrEmpty(_config.ApiSecret))
            {
                return Result<PaymentInitiationResult>.Failure("Stripe API secret is not configured");
            }

            var sessionService = new Stripe.Checkout.SessionService();
            var sessionOptions = new Stripe.Checkout.SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                Mode = "payment",
                SuccessUrl = returnUrl,
                CancelUrl = !string.IsNullOrEmpty(cancelUrl) ? cancelUrl : returnUrl,
                LineItems = new List<Stripe.Checkout.SessionLineItemOptions>
                {
                    new()
                    {
                        PriceData = new Stripe.Checkout.SessionLineItemPriceDataOptions
                        {
                            Currency = currency.ToLowerInvariant(),
                            UnitAmount = (long)(amount * 100),
                            ProductData = new Stripe.Checkout.SessionLineItemPriceDataProductDataOptions
                            {
                                Name = description
                            }
                        },
                        Quantity = 1
                    }
                },
                Metadata = metadata != null ? new Dictionary<string, string>(metadata) : new Dictionary<string, string>()
            };

            var session = await sessionService.CreateAsync(sessionOptions);

            _logger.LogInformation("Stripe Checkout Session created: {SessionId}", session.Id);

            return Result<PaymentInitiationResult>.Success(
                new PaymentInitiationResult(session.Url, session.Id));
        }
        catch (StripeException stripeEx)
        {
            _logger.LogError(stripeEx, "Stripe error creating payment");
            return Result<PaymentInitiationResult>.Failure($"Stripe error: {stripeEx.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Stripe payment");
            return Result<PaymentInitiationResult>.Failure($"Failed to create payment: {ex.Message}");
        }
    }

    public async Task<Result<PaymentVerificationResult>> VerifyPaymentAsync(string payload)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return Result<PaymentVerificationResult>.Failure("Payload is required");
            }

            var jsonDoc = System.Text.Json.JsonDocument.Parse(payload);
            var root = jsonDoc.RootElement;

            var paymentIntentId = root.TryGetProperty("data", out var dataProp) &&
                                  dataProp.TryGetProperty("object", out var objectProp) &&
                                  objectProp.TryGetProperty("id", out var idProp)
                    ? idProp.GetString()
                    : root.TryGetProperty("payment_intent", out var piProp)
                        ? piProp.GetString()
                        : null;

            if (string.IsNullOrEmpty(paymentIntentId))
            {
                return Result<PaymentVerificationResult>.Failure("Payment intent ID not found in payload");
            }

            var service = new PaymentIntentService();
            var paymentIntent = await service.GetAsync(paymentIntentId);

            if (paymentIntent.Status == "succeeded")
            {
                _logger.LogInformation("Stripe payment verified: {PaymentIntentId}", paymentIntentId);
                return Result<PaymentVerificationResult>.Success(
                    new PaymentVerificationResult(paymentIntentId));
            }

            _logger.LogWarning("Stripe payment not successful: {PaymentIntentId} - {Status}", paymentIntentId, paymentIntent.Status);
            return Result<PaymentVerificationResult>.Failure($"Payment status: {paymentIntent.Status}");
        }
        catch (StripeException stripeEx)
        {
            _logger.LogError(stripeEx, "Stripe error verifying payment");
            return Result<PaymentVerificationResult>.Failure($"Stripe error: {stripeEx.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Stripe payment");
            return Result<PaymentVerificationResult>.Failure($"Failed to verify payment: {ex.Message}");
        }
    }

    public async Task<Result> RefundAsync(string transactionId, decimal? amount = null)
    {
        try
        {
            if (string.IsNullOrEmpty(_config.ApiSecret))
            {
                return Result.Failure("Stripe API secret is not configured");
            }

            var service = new RefundService();
            var options = new RefundCreateOptions
            {
                PaymentIntent = transactionId
            };

            if (amount.HasValue)
            {
                options.Amount = (long)(amount.Value * 100);
            }

            var refund = await service.CreateAsync(options);

            _logger.LogInformation("Stripe refund created: {RefundId} for transaction {TransactionId}", refund.Id, transactionId);

            if (refund.Status == "succeeded")
            {
                return Result.Success();
            }

            return Result.Failure($"Refund status: {refund.Status}");
        }
        catch (StripeException stripeEx)
        {
            _logger.LogError(stripeEx, "Stripe error creating refund");
            return Result.Failure($"Stripe error: {stripeEx.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe refund for transaction {TransactionId}", transactionId);
            return Result.Failure($"Failed to process refund: {ex.Message}");
        }
    }

    public bool ValidateWebhookSignature(string payload, string signature, string secret)
    {
        try
        {
            if (string.IsNullOrEmpty(payload) || string.IsNullOrEmpty(signature))
            {
                return false;
            }

            var secretToUse = !string.IsNullOrEmpty(secret) ? secret : _config.ApiSecret;
            if (string.IsNullOrEmpty(secretToUse))
            {
                _logger.LogWarning("Stripe webhook secret is not configured");
                return false;
            }

            var stripeEvent = EventUtility.ConstructEvent(
                payload,
                signature,
                secretToUse,
                300
            );

            _logger.LogInformation("Stripe webhook signature validated for event type: {EventType}", stripeEvent.Type);
            return true;
        }
        catch (StripeException stripeEx)
        {
            _logger.LogWarning(stripeEx, "Stripe webhook signature validation failed");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Stripe webhook signature");
            return false;
        }
    }
}
