using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Rehably.Application.Services.Payment;
using Rehably.Infrastructure.Settings;

namespace Rehably.API.Controllers.Public;

/// <summary>
/// Receives Stripe webhook events and processes completed payments.
/// </summary>
[ApiController]
[Route("api/webhooks/stripe")]
[Produces("application/json")]
[Tags("Webhooks")]
public class StripeWebhookController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly PaymentSettings _settings;
    private readonly ILogger<StripeWebhookController> _logger;

    public StripeWebhookController(
        IPaymentService paymentService,
        IOptions<PaymentSettings> settings,
        ILogger<StripeWebhookController> logger)
    {
        _paymentService = paymentService;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Handle incoming Stripe webhook events.
    /// </summary>
    /// <remarks>
    /// Validates the Stripe-Signature header and processes checkout.session.completed
    /// and payment_intent.succeeded events to activate clinics after successful payment.
    /// </remarks>
    /// <response code="200">Event processed successfully.</response>
    /// <response code="400">Invalid payload or unsupported event type.</response>
    /// <response code="401">Signature validation failed.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> HandleWebhook(CancellationToken cancellationToken = default)
    {
        string payload;
        using (var reader = new StreamReader(Request.Body))
        {
            payload = await reader.ReadToEndAsync(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            return BadRequest(new { error = "Empty payload" });
        }

        var signature = Request.Headers["Stripe-Signature"].FirstOrDefault();
        if (string.IsNullOrEmpty(signature))
        {
            _logger.LogWarning("Stripe webhook received without Stripe-Signature header");
            return Unauthorized(new { error = "Missing Stripe-Signature header" });
        }

        var stripeConfig = _settings.Providers.FirstOrDefault(p =>
            p.Key.Equals("stripe", StringComparison.OrdinalIgnoreCase));

        if (stripeConfig == null || string.IsNullOrEmpty(stripeConfig.WebhookSecret))
        {
            _logger.LogError("Stripe webhook secret not configured");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Stripe not configured" });
        }

        var stripeProvider = _paymentService.GetProvider("stripe");
        var isValid = stripeProvider.ValidateWebhookSignature(payload, signature, stripeConfig.WebhookSecret);

        if (!isValid)
        {
            _logger.LogWarning("Stripe webhook signature validation failed");
            return Unauthorized(new { error = "Invalid signature" });
        }

        // Extract event type and transaction ID from the payload
        string? eventType = null;
        string? transactionId = null;

        try
        {
            var jsonDoc = System.Text.Json.JsonDocument.Parse(payload);
            var root = jsonDoc.RootElement;

            eventType = root.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;

            // For checkout.session.completed the relevant ID is payment_intent
            // For payment_intent.succeeded it is data.object.id
            if (root.TryGetProperty("data", out var dataProp) &&
                dataProp.TryGetProperty("object", out var obj))
            {
                if (eventType == "checkout.session.completed")
                {
                    // The session's payment_intent is the ID stored in our Payment record
                    transactionId = obj.TryGetProperty("payment_intent", out var pi)
                        ? pi.GetString()
                        : null;
                }
                else if (eventType == "payment_intent.succeeded")
                {
                    transactionId = obj.TryGetProperty("id", out var id)
                        ? id.GetString()
                        : null;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Stripe webhook payload");
            return BadRequest(new { error = "Invalid JSON payload" });
        }

        if (eventType == null)
        {
            return BadRequest(new { error = "Missing event type" });
        }

        if (eventType is not ("checkout.session.completed" or "payment_intent.succeeded"))
        {
            // Acknowledge unhandled events so Stripe stops retrying
            _logger.LogDebug("Stripe webhook event type {EventType} ignored", eventType);
            return Ok(new { received = true });
        }

        if (string.IsNullOrEmpty(transactionId))
        {
            _logger.LogWarning("Stripe {EventType} event missing transaction ID", eventType);
            return BadRequest(new { error = "Missing transaction ID in event" });
        }

        _logger.LogInformation("Processing Stripe {EventType} for transaction {TransactionId}", eventType, transactionId);

        var result = await _paymentService.ProcessPaymentCallbackAsync(transactionId, payload, "stripe");

        if (result.IsFailure)
        {
            _logger.LogError("Payment callback processing failed for {TransactionId}: {Error}", transactionId, result.Error);
            // Return 200 to prevent Stripe from retrying non-retriable errors (e.g. not found)
            return Ok(new { received = true, warning = result.Error });
        }

        return Ok(new { received = true });
    }
}
