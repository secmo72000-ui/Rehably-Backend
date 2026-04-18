using FluentAssertions;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Enums;

namespace Rehably.Tests.Domain;

public class SubscriptionDomainTests
{
    #region IsExpired

    [Fact]
    public void IsExpired_WhenEndDateIsInPast_ShouldReturnTrue()
    {
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(-1)
        };

        subscription.IsExpired().Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WhenEndDateIsInFuture_ShouldReturnFalse()
    {
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        subscription.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenEndDateIsNow_ShouldReturnTrue()
    {
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow
        };

        subscription.IsExpired().Should().BeTrue();
    }

    #endregion

    #region CanRenew

    [Fact]
    public void CanRenew_WhenStatusIsActiveAndNotCancelled_ShouldReturnTrue()
    {
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        subscription.CanRenew().Should().BeTrue();
    }

    [Fact]
    public void CanRenew_WhenStatusIsActiveButCancelledAtIsSet_ShouldReturnFalse()
    {
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(30),
            CancelledAt = DateTime.UtcNow.AddDays(-5)
        };

        subscription.CanRenew().Should().BeFalse();
    }

    [Fact]
    public void CanRenew_WhenStatusIsExpired_ShouldReturnFalse()
    {
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Expired,
            StartDate = DateTime.UtcNow.AddDays(-60),
            EndDate = DateTime.UtcNow.AddDays(-30)
        };

        subscription.CanRenew().Should().BeFalse();
    }

    [Fact]
    public void CanRenew_WhenStatusIsCancelled_ShouldReturnFalse()
    {
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Cancelled,
            StartDate = DateTime.UtcNow.AddDays(-60),
            EndDate = DateTime.UtcNow.AddDays(-30),
            CancelledAt = DateTime.UtcNow.AddDays(-40)
        };

        subscription.CanRenew().Should().BeFalse();
    }

    [Fact]
    public void CanRenew_WhenStatusIsTrial_ShouldReturnFalse()
    {
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Trial,
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow.AddDays(7),
            TrialEndsAt = DateTime.UtcNow.AddDays(7)
        };

        subscription.CanRenew().Should().BeFalse();
    }

    #endregion

    #region CanCancel

    [Fact]
    public void CanCancel_WhenStatusIsActiveAndNotCancelled_ShouldReturnTrue()
    {
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        subscription.CanCancel().Should().BeTrue();
    }

    [Fact]
    public void CanCancel_WhenStatusIsActiveButCancelledAtIsSet_ShouldReturnFalse()
    {
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(30),
            CancelledAt = DateTime.UtcNow.AddDays(-5)
        };

        subscription.CanCancel().Should().BeFalse();
    }

    [Fact]
    public void CanCancel_WhenStatusIsCancelled_ShouldReturnFalse()
    {
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Cancelled,
            StartDate = DateTime.UtcNow.AddDays(-60),
            EndDate = DateTime.UtcNow.AddDays(-30),
            CancelledAt = DateTime.UtcNow.AddDays(-40)
        };

        subscription.CanCancel().Should().BeFalse();
    }

    [Fact]
    public void CanCancel_WhenStatusIsExpired_ShouldReturnFalse()
    {
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Expired,
            StartDate = DateTime.UtcNow.AddDays(-60),
            EndDate = DateTime.UtcNow.AddDays(-1)
        };

        subscription.CanCancel().Should().BeFalse();
    }

    #endregion

    #region DaysUntilExpiry

    [Fact]
    public void DaysUntilExpiry_WhenExpiringIn7Days_ShouldReturn7()
    {
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-23),
            EndDate = DateTime.UtcNow.Date.AddDays(7)
        };

        subscription.DaysUntilExpiry().Should().Be(7);
    }

    [Fact]
    public void DaysUntilExpiry_WhenAlreadyExpired_ShouldReturnZero()
    {
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Expired,
            StartDate = DateTime.UtcNow.AddDays(-60),
            EndDate = DateTime.UtcNow.AddDays(-5)
        };

        subscription.DaysUntilExpiry().Should().Be(0);
    }

    [Fact]
    public void DaysUntilExpiry_WhenExpiringIn30Days_ShouldReturn30()
    {
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.Date.AddDays(30)
        };

        subscription.DaysUntilExpiry().Should().Be(30);
    }

    #endregion

    #region IsInGracePeriod

    [Fact]
    public void IsInGracePeriod_WhenWithin7DaysAfterExpiry_ShouldReturnTrue()
    {
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Expired,
            StartDate = DateTime.UtcNow.AddDays(-37),
            EndDate = DateTime.UtcNow.AddDays(-3)
        };

        subscription.IsInGracePeriod().Should().BeTrue();
    }

    [Fact]
    public void IsInGracePeriod_WhenMoreThan7DaysAfterExpiry_ShouldReturnFalse()
    {
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Expired,
            StartDate = DateTime.UtcNow.AddDays(-50),
            EndDate = DateTime.UtcNow.AddDays(-10)
        };

        subscription.IsInGracePeriod().Should().BeFalse();
    }

    [Fact]
    public void IsInGracePeriod_WhenNotExpired_ShouldReturnFalse()
    {
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-23),
            EndDate = DateTime.UtcNow.AddDays(7)
        };

        subscription.IsInGracePeriod().Should().BeFalse();
    }

    #endregion

    #region IsInTrial

    [Fact]
    public void IsInTrial_WhenStatusIsTrialAndTrialEndsAtIsInFuture_ShouldReturnTrue()
    {
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Trial,
            StartDate = DateTime.UtcNow.AddDays(-3),
            EndDate = DateTime.UtcNow.AddDays(11),
            TrialEndsAt = DateTime.UtcNow.AddDays(11)
        };

        subscription.IsInTrial().Should().BeTrue();
    }

    [Fact]
    public void IsInTrial_WhenStatusIsTrialButTrialEndsAtIsInPast_ShouldReturnFalse()
    {
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Trial,
            StartDate = DateTime.UtcNow.AddDays(-20),
            EndDate = DateTime.UtcNow.AddDays(-1),
            TrialEndsAt = DateTime.UtcNow.AddDays(-1)
        };

        subscription.IsInTrial().Should().BeFalse();
    }

    [Fact]
    public void IsInTrial_WhenStatusIsTrialAndTrialEndsAtIsNull_ShouldReturnFalse()
    {
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Trial,
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow.AddDays(7),
            TrialEndsAt = null
        };

        subscription.IsInTrial().Should().BeFalse();
    }

    [Fact]
    public void IsInTrial_WhenStatusIsActive_ShouldReturnFalse()
    {
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(30),
            TrialEndsAt = DateTime.UtcNow.AddDays(30)
        };

        subscription.IsInTrial().Should().BeFalse();
    }

    #endregion

    #region CanUpgrade

    [Fact]
    public void CanUpgrade_WhenStatusIsActive_ShouldReturnTrue()
    {
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        subscription.CanUpgrade().Should().BeTrue();
    }

    [Fact]
    public void CanUpgrade_WhenStatusIsTrial_ShouldReturnTrue()
    {
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Trial,
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow.AddDays(7),
            TrialEndsAt = DateTime.UtcNow.AddDays(7)
        };

        subscription.CanUpgrade().Should().BeTrue();
    }

    [Fact]
    public void CanUpgrade_WhenStatusIsCancelled_ShouldReturnFalse()
    {
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Cancelled,
            StartDate = DateTime.UtcNow.AddDays(-60),
            EndDate = DateTime.UtcNow.AddDays(-30),
            CancelledAt = DateTime.UtcNow.AddDays(-40)
        };

        subscription.CanUpgrade().Should().BeFalse();
    }

    [Fact]
    public void CanUpgrade_WhenStatusIsExpired_ShouldReturnFalse()
    {
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Expired,
            StartDate = DateTime.UtcNow.AddDays(-60),
            EndDate = DateTime.UtcNow.AddDays(-1)
        };

        subscription.CanUpgrade().Should().BeFalse();
    }

    #endregion
}
