using FluentAssertions;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Enums;

namespace Rehably.Tests.Domain;

public class PaymentDomainTests
{
    private static Payment CreateCompletedPayment(DateTime? processedAt = null) => new Payment
    {
        Amount = 299.00m,
        Currency = "EGP",
        Provider = PaymentProvider.PayMob,
        Status = PaymentStatus.Completed,
        ProcessedAt = processedAt ?? DateTime.UtcNow.AddDays(-5)
    };

    #region IsSuccessful

    [Fact]
    public void IsSuccessful_WhenStatusIsCompleted_ShouldReturnTrue()
    {
        var payment = CreateCompletedPayment();

        payment.IsSuccessful().Should().BeTrue();
    }

    [Fact]
    public void IsSuccessful_WhenStatusIsPending_ShouldReturnFalse()
    {
        var payment = new Payment
        {
            Amount = 299.00m,
            Currency = "EGP",
            Provider = PaymentProvider.PayMob,
            Status = PaymentStatus.Pending
        };

        payment.IsSuccessful().Should().BeFalse();
    }

    [Fact]
    public void IsSuccessful_WhenStatusIsFailed_ShouldReturnFalse()
    {
        var payment = new Payment
        {
            Amount = 299.00m,
            Currency = "EGP",
            Provider = PaymentProvider.PayMob,
            Status = PaymentStatus.Failed,
            FailureReason = "Insufficient funds"
        };

        payment.IsSuccessful().Should().BeFalse();
    }

    [Fact]
    public void IsSuccessful_WhenStatusIsRefunded_ShouldReturnFalse()
    {
        var payment = new Payment
        {
            Amount = 299.00m,
            Currency = "EGP",
            Provider = PaymentProvider.PayMob,
            Status = PaymentStatus.Refunded
        };

        payment.IsSuccessful().Should().BeFalse();
    }

    [Fact]
    public void IsSuccessful_WhenStatusIsProcessing_ShouldReturnFalse()
    {
        var payment = new Payment
        {
            Amount = 299.00m,
            Currency = "EGP",
            Provider = PaymentProvider.PayMob,
            Status = PaymentStatus.Processing
        };

        payment.IsSuccessful().Should().BeFalse();
    }

    #endregion

    #region CanRefund

    [Fact]
    public void CanRefund_WhenStatusIsCompleted_ShouldReturnTrue()
    {
        var payment = CreateCompletedPayment();

        payment.CanRefund().Should().BeTrue();
    }

    [Fact]
    public void CanRefund_WhenStatusIsPending_ShouldReturnFalse()
    {
        var payment = new Payment
        {
            Amount = 299.00m,
            Currency = "EGP",
            Provider = PaymentProvider.PayMob,
            Status = PaymentStatus.Pending
        };

        payment.CanRefund().Should().BeFalse();
    }

    [Fact]
    public void CanRefund_WhenStatusIsAlreadyRefunded_ShouldReturnFalse()
    {
        var payment = new Payment
        {
            Amount = 299.00m,
            Currency = "EGP",
            Provider = PaymentProvider.PayMob,
            Status = PaymentStatus.Refunded,
            ProcessedAt = DateTime.UtcNow.AddDays(-5)
        };

        payment.CanRefund().Should().BeFalse();
    }

    [Fact]
    public void CanRefund_WhenStatusIsFailed_ShouldReturnFalse()
    {
        var payment = new Payment
        {
            Amount = 299.00m,
            Currency = "EGP",
            Provider = PaymentProvider.PayMob,
            Status = PaymentStatus.Failed,
            FailureReason = "Card declined"
        };

        payment.CanRefund().Should().BeFalse();
    }

    #endregion

    #region Refund

    [Fact]
    public void Refund_WhenPaymentIsCompleted_ShouldChangeStatusToRefunded()
    {
        var payment = CreateCompletedPayment();

        payment.Refund();

        payment.Status.Should().Be(PaymentStatus.Refunded);
    }

    [Fact]
    public void Refund_WhenPaymentIsPending_ShouldThrowInvalidOperationException()
    {
        var payment = new Payment
        {
            Amount = 299.00m,
            Currency = "EGP",
            Provider = PaymentProvider.PayMob,
            Status = PaymentStatus.Pending
        };

        var act = () => payment.Refund();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Refund_WhenAlreadyRefunded_ShouldThrowInvalidOperationException()
    {
        var payment = new Payment
        {
            Amount = 299.00m,
            Currency = "EGP",
            Provider = PaymentProvider.PayMob,
            Status = PaymentStatus.Refunded,
            ProcessedAt = DateTime.UtcNow.AddDays(-10)
        };

        var act = () => payment.Refund();

        act.Should().Throw<InvalidOperationException>();
    }

    #endregion
}
