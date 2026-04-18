#if DEBUG
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rehably.Application.DTOs.Clinic;
using Rehably.Application.DTOs.Payment;
using Rehably.Application.Services.Payment;
using Rehably.Application.Services.Storage;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;
using Rehably.Infrastructure.Services.Communication;

namespace Rehably.API.Controllers.Admin;

/// <summary>
/// Development-only endpoints for testing integrations (mock messages, file uploads, payments)
/// </summary>
[ApiController]
[Route("api/development")]
[Authorize(Roles = "PlatformAdmin")]
[Produces("application/json")]
[Tags("Admin - Development")]
public class DevelopmentController : ControllerBase
{
    private readonly ILogger<DevelopmentController> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly IDocumentService _documentService;
    private readonly IPaymentService _paymentService;
    private readonly ApplicationDbContext _dbContext;

    public DevelopmentController(
        ILogger<DevelopmentController> logger,
        IWebHostEnvironment env,
        IDocumentService documentService,
        IPaymentService paymentService,
        ApplicationDbContext dbContext)
    {
        _logger = logger;
        _env = env;
        _documentService = documentService;
        _paymentService = paymentService;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Get all mock messages (Email, SMS, WhatsApp) sent during testing
    /// </summary>
    /// <remarks>
    /// Returns all mock messages captured during testing in development environment.
    /// Available in DEBUG mode only.
    /// </remarks>
    /// <response code="200">Returns list of mock messages</response>
    /// <response code="403">Access forbidden - not in development environment or insufficient role</response>
    [HttpGet("mock-messages")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult GetMockMessages()
    {
        if (!_env.IsDevelopment())
        {
            return Forbid();
        }

        return Ok(new { messages = MockMessageStore.GetAll() });
    }

    /// <summary>
    /// Clear all mock messages
    /// </summary>
    /// <remarks>
    /// Clears all captured mock messages from the in-memory store.
    /// Available in DEBUG mode only.
    /// </remarks>
    /// <response code="200">Mock messages cleared successfully</response>
    /// <response code="403">Access forbidden - not in development environment or insufficient role</response>
    [HttpDelete("mock-messages")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult ClearMockMessages()
    {
        if (!_env.IsDevelopment())
        {
            return Forbid();
        }

        MockMessageStore.Clear();
        return Ok(new { message = "Mock messages cleared." });
    }

    /// <summary>
    /// Upload a file to Cloudinary and get the public link
    /// </summary>
    /// <remarks>
    /// Tests Cloudinary integration by uploading a base64-encoded file.
    /// If no parameters provided, uploads a default test image.
    /// Available in DEBUG mode only.
    /// </remarks>
    /// <param name="request">Upload request containing base64 data, filename, clinic ID, and document type</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <response code="200">File uploaded successfully with public URL and metadata</response>
    /// <response code="400">Upload failed - invalid request or service error</response>
    /// <response code="403">Access forbidden - not in development environment or insufficient role</response>
    /// <response code="500">Internal server error during upload</response>
    [HttpPost("upload")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> UploadTestDocument(
        [FromBody] UploadRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_env.IsDevelopment())
        {
            return Forbid();
        }

        try
        {
            var base64Data = request.Base64Data ?? "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";
            var fileName = request.FileName ?? $"test_upload_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png";
            var clinicId = request.ClinicId ?? Guid.Empty;
            var documentType = request.DocumentType ?? DocumentType.MedicalLicense;

            var result = await _documentService.UploadDocumentFromBase64Async(
                clinicId,
                documentType,
                fileName,
                base64Data);

            if (!result.IsSuccess)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Upload failed",
                    details = result.Error
                });
            }

            return Ok(new
            {
                success = true,
                message = "File uploaded successfully to Cloudinary",
                publicUrl = result.Value.PublicUrl,
                data = new
                {
                    id = result.Value.Id,
                    clinicId = result.Value.ClinicId,
                    documentType = result.Value.DocumentType.ToString(),
                    storageUrl = result.Value.StorageUrl,
                    publicUrl = result.Value.PublicUrl,
                    uploadedAt = result.Value.UploadedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload test failed");
            return StatusCode(500, new
            {
                success = false,
                error = "Upload test failed",
                details = ex.Message
            });
        }
    }

    /// <summary>
    /// Test PayMob payment integration
    /// </summary>
    /// <remarks>
    /// Creates a test payment transaction using PayMob payment gateway.
    /// Uses default values if request parameters are omitted.
    /// Requires PayMob credentials configured in appsettings.json.
    /// Available in DEBUG mode only.
    /// </remarks>
    /// <param name="request">Payment test request with clinic ID, plan ID, and redirect URLs</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <response code="200">Payment created successfully with transaction ID and payment URL</response>
    /// <response code="400">Payment creation failed - invalid request or PayMob error</response>
    /// <response code="403">Access forbidden - not in development environment or insufficient role</response>
    /// <response code="500">Internal server error during payment creation</response>
    [HttpPost("test-paymob")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> TestPayMobPayment(
        [FromBody] PaymentTestRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_env.IsDevelopment())
        {
            return Forbid();
        }

        try
        {
            var amount = request.Amount ?? 100;
            var clinicId = request.ClinicId ?? Guid.Empty;
            var planId = request.SubscriptionPlanId ?? Guid.NewGuid();

            var returnUrl = request.ReturnUrl ?? "http://localhost:3000/payment/success";
            var cancelUrl = request.CancelUrl ?? "http://localhost:3000/payment/cancel";

            var paymobResult = await _paymentService.CreateSubscriptionPaymentAsync(
                clinicId,
                planId,
                returnUrl,
                cancelUrl,
                "paymob");

            if (paymobResult.IsFailure)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "PayMob payment creation failed",
                    details = paymobResult.Error,
                    note = "Make sure PayMob credentials are configured in appsettings.json"
                });
            }

            return Ok(new
            {
                success = true,
                message = "PayMob payment created successfully",
                transactionId = paymobResult.Value.TransactionId,
                paymentUrl = paymobResult.Value.PaymentUrl,
                instructions = new
                {
                    step1 = "Click on paymentUrl to open PayMob payment page",
                    step2 = "Complete payment with test card details",
                    step3 = "PayMob will send webhook to /api/webhooks/paymob",
                    step4 = "Check payment status in database"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayMob payment test failed");
            return StatusCode(500, new
            {
                success = false,
                error = "PayMob payment test failed",
                details = ex.Message
            });
        }
    }

    /// <summary>
    /// Test Stripe payment integration
    /// </summary>
    /// <remarks>
    /// Creates a test payment transaction using Stripe payment gateway.
    /// Uses default values if request parameters are omitted.
    /// Requires Stripe credentials configured in appsettings.json.
    /// Available in DEBUG mode only.
    /// </remarks>
    /// <param name="request">Payment test request with clinic ID, plan ID, and redirect URLs</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <response code="200">Payment created successfully with payment intent ID and instructions</response>
    /// <response code="400">Payment creation failed - invalid request or Stripe error</response>
    /// <response code="403">Access forbidden - not in development environment or insufficient role</response>
    /// <response code="500">Internal server error during payment creation</response>
    [HttpPost("test-stripe")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> TestStripePayment(
        [FromBody] PaymentTestRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_env.IsDevelopment())
        {
            return Forbid();
        }

        try
        {
            var amount = request.Amount ?? 100;
            var clinicId = request.ClinicId ?? Guid.Empty;
            var planId = request.SubscriptionPlanId ?? Guid.NewGuid();

            var returnUrl = request.ReturnUrl ?? "http://localhost:3000/payment/success";
            var cancelUrl = request.CancelUrl ?? "http://localhost:3000/payment/cancel";

            var paymentResult = await _paymentService.CreateSubscriptionPaymentAsync(
                clinicId,
                planId,
                returnUrl,
                cancelUrl,
                "stripe");

            if (paymentResult.IsFailure)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Stripe payment creation failed",
                    details = paymentResult.Error,
                    note = "Make sure Stripe credentials are configured in appsettings.json"
                });
            }

            return Ok(new
            {
                success = true,
                message = "Stripe payment created successfully",
                paymentIntentId = paymentResult.Value.TransactionId,
                instructions = new
                {
                    step1 = "Use Stripe.js on frontend with the PaymentIntentId",
                    step2 = "Collect card details and confirm payment",
                    step3 = "Stripe will send webhook to /api/webhooks/stripe",
                    step4 = "Check payment status in database",
                    testCards = "https://stripe.com/docs/testing#cards"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stripe payment test failed");
            return StatusCode(500, new
            {
                success = false,
                error = "Stripe payment test failed",
                details = ex.Message
            });
        }
    }
    /// <summary>
    /// Backfill CurrentSubscriptionId for clinics that have subscriptions but missing the FK link
    /// </summary>
    /// <response code="200">Backfill completed with count of fixed clinics</response>
    [HttpPost("backfill-subscription-links")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> BackfillSubscriptionLinks()
    {
        if (!_env.IsDevelopment())
            return Forbid();

        var clinicsWithoutSubscriptionLink = await _dbContext.Clinics
            .Where(c => c.CurrentSubscriptionId == null)
            .ToListAsync();

        var fixedCount = 0;
        foreach (var clinic in clinicsWithoutSubscriptionLink)
        {
            var subscription = await _dbContext.Subscriptions
                .Where(s => s.ClinicId == clinic.Id)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();

            if (subscription != null)
            {
                clinic.CurrentSubscriptionId = subscription.Id;
                fixedCount++;
            }
        }

        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = $"Backfilled {fixedCount} clinic subscription links",
            totalChecked = clinicsWithoutSubscriptionLink.Count,
            linkedCount = fixedCount
        });
    }
}
#endif
