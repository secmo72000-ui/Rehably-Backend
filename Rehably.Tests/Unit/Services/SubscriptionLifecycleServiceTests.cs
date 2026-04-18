using FluentAssertions;
using Moq;
using Rehably.Application.Interfaces;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;

namespace Rehably.Tests.Unit.Services;

public class SubscriptionLifecycleServiceTests
{
    private readonly Mock<IClock> _clockMock;

    public SubscriptionLifecycleServiceTests()
    {
        _clockMock = new Mock<IClock>();
        _clockMock.Setup(c => c.UtcNow).Returns(new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void RenewForNextCycle_Monthly_ExtendsEndDateByOneMonth()
    {
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            Status = SubscriptionStatus.Active,
            AutoRenew = true,
            BillingCycle = BillingCycle.Monthly,
            StartDate = new DateTime(2026, 2, 1),
            EndDate = new DateTime(2026, 3, 1)
        };

        subscription.RenewForNextCycle();

        subscription.EndDate.Should().Be(new DateTime(2026, 4, 1));
        subscription.StartDate.Should().Be(new DateTime(2026, 3, 1));
        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.PaymentRetryCount.Should().Be(0);
    }

    [Fact]
    public void RenewForNextCycle_Yearly_ExtendsEndDateBy365Days()
    {
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            Status = SubscriptionStatus.Active,
            AutoRenew = true,
            BillingCycle = BillingCycle.Yearly,
            StartDate = new DateTime(2025, 3, 1),
            EndDate = new DateTime(2026, 3, 1)
        };

        subscription.RenewForNextCycle();

        subscription.EndDate.Should().Be(new DateTime(2027, 3, 1));
        subscription.PaymentRetryCount.Should().Be(0);
        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.SuspendedAt.Should().BeNull();
    }

    [Fact]
    public void RenewForNextCycle_SuspendedSubscription_ClearsSuspendedAt()
    {
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            Status = SubscriptionStatus.Suspended,
            BillingCycle = BillingCycle.Monthly,
            SuspendedAt = DateTime.UtcNow.AddDays(-2),
            PaymentRetryCount = 2,
            StartDate = new DateTime(2026, 2, 1),
            EndDate = new DateTime(2026, 3, 1)
        };

        subscription.RenewForNextCycle();

        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.SuspendedAt.Should().BeNull();
        subscription.PaymentRetryCount.Should().Be(0);
    }

    [Fact]
    public void SuspendClinic_Active_SetsStatusSuspendedAndSchedulesDeletion()
    {
        var clinic = new Clinic
        {
            Id = Guid.NewGuid(),
            Status = ClinicStatus.Active,
            Name = "Test Clinic"
        };

        clinic.Suspend();

        clinic.Status.Should().Be(ClinicStatus.Suspended);
        clinic.SuspendedAt.Should().NotBeNull();
        clinic.DataDeletionDate.Should().NotBeNull();
        clinic.DataDeletionDate!.Value.Should().BeCloseTo(
            DateTime.UtcNow.AddDays(30), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void SuspendClinic_AlreadySuspended_IsSuspendedPropertyReturnsTrue()
    {
        var clinic = new Clinic
        {
            Id = Guid.NewGuid(),
            Status = ClinicStatus.Suspended,
            SuspendedAt = DateTime.UtcNow.AddDays(-5)
        };

        // IsSuspended is a computed property — no mutation needed to assert the state
        clinic.IsSuspended.Should().BeTrue();
    }

    [Fact]
    public void ReactivateClinic_Suspended_ClearsAllSuspensionFields()
    {
        var clinic = new Clinic
        {
            Id = Guid.NewGuid(),
            Status = ClinicStatus.Suspended,
            SuspendedAt = DateTime.UtcNow.AddDays(-5),
            DataDeletionDate = DateTime.UtcNow.AddDays(25),
            DeletionJobId = "job-123",
            DeletionStage = DeletionStage.AuditLogged
        };

        clinic.Reactivate();

        clinic.Status.Should().Be(ClinicStatus.Active);
        clinic.IsSuspended.Should().BeFalse();
        clinic.SuspendedAt.Should().BeNull();
        clinic.DataDeletionDate.Should().BeNull();
        clinic.DeletionJobId.Should().BeNull();
        clinic.DeletionStage.Should().Be(DeletionStage.NotStarted);
    }

    [Fact]
    public void CanBeDeleted_Suspended_DeletionDatePassed_ReturnsTrue()
    {
        var clinic = new Clinic
        {
            Id = Guid.NewGuid(),
            Status = ClinicStatus.Suspended,
            SuspendedAt = DateTime.UtcNow.AddDays(-35),
            DataDeletionDate = DateTime.UtcNow.AddDays(-5)
        };

        clinic.CanBeDeleted().Should().BeTrue();
    }

    [Fact]
    public void CanBeDeleted_Suspended_DeletionDateNotYetPassed_ReturnsFalse()
    {
        var clinic = new Clinic
        {
            Id = Guid.NewGuid(),
            Status = ClinicStatus.Suspended,
            SuspendedAt = DateTime.UtcNow.AddDays(-5),
            DataDeletionDate = DateTime.UtcNow.AddDays(25)
        };

        clinic.CanBeDeleted().Should().BeFalse();
    }

    [Fact]
    public void CanBeDeleted_Active_ReturnsFalse()
    {
        var clinic = new Clinic
        {
            Id = Guid.NewGuid(),
            Status = ClinicStatus.Active
        };

        clinic.CanBeDeleted().Should().BeFalse();
    }

    [Fact]
    public void SuspendSubscription_Active_SetsStatusAndSuspendedAt()
    {
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            Status = SubscriptionStatus.Active
        };

        subscription.Suspend();

        subscription.Status.Should().Be(SubscriptionStatus.Suspended);
        subscription.SuspendedAt.Should().NotBeNull();
        subscription.SuspendedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void IsInDunning_ActiveWithRetryCountBelowThreshold_ReturnsTrue()
    {
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Active,
            PaymentRetryCount = 1
        };

        subscription.IsInDunning().Should().BeTrue();
    }

    [Fact]
    public void IsInDunning_ActiveWithRetryCountAtThreshold_ReturnsFalse()
    {
        // PaymentRetryCount must be > 0 AND < 3 — at 3 it is no longer in dunning
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Active,
            PaymentRetryCount = 3
        };

        subscription.IsInDunning().Should().BeFalse();
    }

    [Fact]
    public void IsInDunning_ActiveWithZeroRetries_ReturnsFalse()
    {
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Active,
            PaymentRetryCount = 0
        };

        subscription.IsInDunning().Should().BeFalse();
    }
}
