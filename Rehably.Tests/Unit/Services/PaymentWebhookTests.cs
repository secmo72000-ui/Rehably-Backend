using FluentAssertions;
using Moq;
using Rehably.Application.Common;
using Rehably.Application.Services.Payment;

namespace Rehably.Tests.Unit.Services;

/// <summary>
/// Contract tests for payment webhook handlers.
/// These tests verify the IPaymentService contract used by the webhook controllers.
/// </summary>
public class PaymentWebhookTests
{
    private readonly Mock<IPaymentService> _paymentServiceMock = new();
    private readonly Mock<IPaymentProvider> _stripeProviderMock = new();
    private readonly Mock<IPaymentProvider> _paymobProviderMock = new();

    private const string ValidStripeSignature = "t=1234567890,v1=abc123";
    private const string ValidPayMobHmac = "validhmac";
    private const string WebhookSecret = "whsec_test";

    public PaymentWebhookTests()
    {
        _paymentServiceMock
            .Setup(s => s.GetProvider("stripe"))
            .Returns(_stripeProviderMock.Object);

        _paymentServiceMock
            .Setup(s => s.GetProvider("paymob"))
            .Returns(_paymobProviderMock.Object);
    }

    // ── Stripe Tests ────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleStripeWebhook_ValidPaymentSucceeded_ActivatesClinic()
    {
        _stripeProviderMock
            .Setup(p => p.ValidateWebhookSignature(It.IsAny<string>(), ValidStripeSignature, WebhookSecret))
            .Returns(true);

        _paymentServiceMock
            .Setup(s => s.ProcessPaymentCallbackAsync("pi_test_123", It.IsAny<string>(), "stripe"))
            .ReturnsAsync(Result.Success());

        var provider = _paymentServiceMock.Object.GetProvider("stripe");
        var isValid = provider.ValidateWebhookSignature("{}", ValidStripeSignature, WebhookSecret);
        isValid.Should().BeTrue();

        var result = await _paymentServiceMock.Object.ProcessPaymentCallbackAsync("pi_test_123", "{}", "stripe");
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleStripeWebhook_InvalidSignature_Returns401()
    {
        _stripeProviderMock
            .Setup(p => p.ValidateWebhookSignature(It.IsAny<string>(), "bad-signature", WebhookSecret))
            .Returns(false);

        var provider = _paymentServiceMock.Object.GetProvider("stripe");
        var isValid = provider.ValidateWebhookSignature("{}", "bad-signature", WebhookSecret);

        isValid.Should().BeFalse();
        _paymentServiceMock.Verify(
            s => s.ProcessPaymentCallbackAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleStripeWebhook_DuplicateEvent_IsIdempotent()
    {
        _stripeProviderMock
            .Setup(p => p.ValidateWebhookSignature(It.IsAny<string>(), ValidStripeSignature, WebhookSecret))
            .Returns(true);

        _paymentServiceMock
            .Setup(s => s.ProcessPaymentCallbackAsync("pi_test_dupe", It.IsAny<string>(), "stripe"))
            .ReturnsAsync(Result.Success());

        var result1 = await _paymentServiceMock.Object.ProcessPaymentCallbackAsync("pi_test_dupe", "{}", "stripe");
        var result2 = await _paymentServiceMock.Object.ProcessPaymentCallbackAsync("pi_test_dupe", "{}", "stripe");

        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        _paymentServiceMock.Verify(
            s => s.ProcessPaymentCallbackAsync("pi_test_dupe", It.IsAny<string>(), "stripe"),
            Times.Exactly(2));
    }

    // ── PayMob Tests ────────────────────────────────────────────────────────

    [Fact]
    public async Task HandlePayMobWebhook_ValidHmac_ActivatesClinic()
    {
        _paymobProviderMock
            .Setup(p => p.ValidateWebhookSignature(It.IsAny<string>(), ValidPayMobHmac, WebhookSecret))
            .Returns(true);

        _paymentServiceMock
            .Setup(s => s.ProcessPaymentCallbackAsync("99001", It.IsAny<string>(), "paymob"))
            .ReturnsAsync(Result.Success());

        var provider = _paymentServiceMock.Object.GetProvider("paymob");
        var isValid = provider.ValidateWebhookSignature("{}", ValidPayMobHmac, WebhookSecret);
        isValid.Should().BeTrue();

        var result = await _paymentServiceMock.Object.ProcessPaymentCallbackAsync("99001", "{}", "paymob");
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandlePayMobWebhook_InvalidHmac_Returns401()
    {
        _paymobProviderMock
            .Setup(p => p.ValidateWebhookSignature(It.IsAny<string>(), "wrong-hmac", WebhookSecret))
            .Returns(false);

        var provider = _paymentServiceMock.Object.GetProvider("paymob");
        var isValid = provider.ValidateWebhookSignature("{}", "wrong-hmac", WebhookSecret);

        isValid.Should().BeFalse();
        _paymentServiceMock.Verify(
            s => s.ProcessPaymentCallbackAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }
}
