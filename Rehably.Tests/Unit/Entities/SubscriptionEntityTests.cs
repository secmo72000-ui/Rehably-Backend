using FluentAssertions;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Enums;

namespace Rehably.Tests.Unit.Entities;

public class SubscriptionEntityTests
{
    private static Subscription CreateActiveSubscription(int paymentRetryCount = 0) => new Subscription
    {
        Status = SubscriptionStatus.Active,
        BillingCycle = BillingCycle.Monthly,
        StartDate = DateTime.UtcNow.AddDays(-15),
        EndDate = DateTime.UtcNow.AddDays(15),
        PaymentRetryCount = paymentRetryCount
    };

    #region IsInDunning

    [Fact]
    public void IsInDunning_ActiveWithRetryCount1_ReturnsTrue()
    {
        var subscription = CreateActiveSubscription(paymentRetryCount: 1);

        subscription.IsInDunning().Should().BeTrue();
    }

    [Fact]
    public void IsInDunning_ActiveWithRetryCount0_ReturnsFalse()
    {
        var subscription = CreateActiveSubscription(paymentRetryCount: 0);

        subscription.IsInDunning().Should().BeFalse();
    }

    [Fact]
    public void IsInDunning_ActiveWithRetryCount3_ReturnsFalse()
    {
        var subscription = CreateActiveSubscription(paymentRetryCount: 3);

        subscription.IsInDunning().Should().BeFalse();
    }

    #endregion

    #region Suspend

    [Fact]
    public void Suspend_SetsStatusAndSuspendedAt()
    {
        var subscription = CreateActiveSubscription();
        var before = DateTime.UtcNow;

        subscription.Suspend();

        subscription.Status.Should().Be(SubscriptionStatus.Suspended);
        subscription.SuspendedAt.Should().NotBeNull();
        subscription.SuspendedAt!.Value.Should().BeOnOrAfter(before);
    }

    #endregion

    #region RenewForNextCycle

    [Fact]
    public void RenewForNextCycle_MonthlyBilling_ExtendsEndDateByOneMonth()
    {
        var subscription = CreateActiveSubscription();
        var originalEndDate = subscription.EndDate;

        subscription.RenewForNextCycle();

        subscription.EndDate.Should().BeCloseTo(originalEndDate.AddMonths(1), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void RenewForNextCycle_YearlyBilling_ExtendsEndDateBy365Days()
    {
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Active,
            BillingCycle = BillingCycle.Yearly,
            StartDate = DateTime.UtcNow.AddDays(-180),
            EndDate = DateTime.UtcNow.AddDays(185),
            PaymentRetryCount = 0
        };
        var originalEndDate = subscription.EndDate;

        subscription.RenewForNextCycle();

        subscription.EndDate.Should().BeCloseTo(originalEndDate.AddDays(365), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void RenewForNextCycle_ResetsPaymentRetryCountToZero()
    {
        var subscription = CreateActiveSubscription(paymentRetryCount: 2);

        subscription.RenewForNextCycle();

        subscription.PaymentRetryCount.Should().Be(0);
    }

    [Fact]
    public void RenewForNextCycle_SetsStatusToActive()
    {
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Suspended,
            BillingCycle = BillingCycle.Monthly,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(-1),
            PaymentRetryCount = 2,
            SuspendedAt = DateTime.UtcNow.AddDays(-2)
        };

        subscription.RenewForNextCycle();

        subscription.Status.Should().Be(SubscriptionStatus.Active);
    }

    #endregion
}
