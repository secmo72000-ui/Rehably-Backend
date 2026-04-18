using FluentAssertions;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;
using Xunit;

namespace Rehably.Tests.Domain;

public class ClinicDomainTests
{
    private static Clinic CreateActiveClinic() => new Clinic
    {
        Name = "Test Clinic",
        Slug = "test-clinic",
        Status = ClinicStatus.Active,
        Phone = "01234567890"
    };

    #region CanBeBanned

    [Fact]
    public void CanBeBanned_WhenNotBannedAndStatusIsActive_ShouldReturnTrue()
    {
        var clinic = CreateActiveClinic();

        clinic.CanBeBanned().Should().BeTrue();
    }

    [Fact]
    public void CanBeBanned_WhenNotBannedAndStatusIsSuspended_ShouldReturnTrue()
    {
        var clinic = new Clinic
        {
            Name = "Test Clinic",
            Slug = "test-clinic",
            Status = ClinicStatus.Suspended,
            Phone = "01234567890"
        };

        clinic.CanBeBanned().Should().BeTrue();
    }

    [Fact]
    public void CanBeBanned_WhenAlreadyBanned_ShouldReturnFalse()
    {
        var clinic = CreateActiveClinic();
        clinic.Ban("Violation", "admin-123");

        clinic.CanBeBanned().Should().BeFalse();
    }

    [Fact]
    public void CanBeBanned_WhenStatusIsPendingEmailVerification_ShouldReturnFalse()
    {
        var clinic = new Clinic
        {
            Name = "Test Clinic",
            Slug = "test-clinic",
            Status = ClinicStatus.PendingEmailVerification,
            Phone = "01234567890"
        };

        clinic.CanBeBanned().Should().BeFalse();
    }

    #endregion

    #region Ban

    [Fact]
    public void Ban_WhenClinicCanBeBanned_ShouldSetIsBannedToTrue()
    {
        var clinic = CreateActiveClinic();

        clinic.Ban("Violation of terms", "admin-123");

        clinic.IsBanned.Should().BeTrue();
    }

    [Fact]
    public void Ban_WhenClinicCanBeBanned_ShouldSetBanReason()
    {
        var clinic = CreateActiveClinic();
        const string reason = "Violation of terms";

        clinic.Ban(reason, "admin-123");

        clinic.BanReason.Should().Be(reason);
    }

    [Fact]
    public void Ban_WhenClinicCanBeBanned_ShouldSetBannedAt()
    {
        var clinic = CreateActiveClinic();
        var before = DateTime.UtcNow;

        clinic.Ban("Violation", "admin-123");

        clinic.BannedAt.Should().NotBeNull();
        clinic.BannedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Ban_WhenClinicCanBeBanned_ShouldSetBannedBy()
    {
        var clinic = CreateActiveClinic();
        const string adminId = "admin-456";

        clinic.Ban("Violation", adminId);

        clinic.BannedBy.Should().Be(adminId);
    }

    [Fact]
    public void Ban_WhenStatusIsPendingEmailVerification_ShouldThrowInvalidOperationException()
    {
        var clinic = new Clinic
        {
            Name = "Test Clinic",
            Slug = "test-clinic",
            Status = ClinicStatus.PendingEmailVerification,
            Phone = "01234567890"
        };

        var act = () => clinic.Ban("Reason", "admin-123");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Ban_WhenAlreadyBanned_ShouldThrowInvalidOperationException()
    {
        var clinic = CreateActiveClinic();
        clinic.Ban("First violation", "admin-123");

        var act = () => clinic.Ban("Second violation", "admin-456");

        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region Activate

    [Fact]
    public void Activate_WhenClinicIsSuspended_ShouldSetStatusToActive()
    {
        var clinic = new Clinic
        {
            Name = "Test Clinic",
            Slug = "test-clinic",
            Status = ClinicStatus.Suspended,
            Phone = "01234567890"
        };

        clinic.Activate();

        clinic.Status.Should().Be(ClinicStatus.Active);
    }

    [Fact]
    public void Activate_WhenClinicIsPendingApproval_ShouldSetStatusToActive()
    {
        var clinic = new Clinic
        {
            Name = "Test Clinic",
            Slug = "test-clinic",
            Status = ClinicStatus.PendingApproval,
            Phone = "01234567890"
        };

        clinic.Activate();

        clinic.Status.Should().Be(ClinicStatus.Active);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ShouldNotThrow()
    {
        var clinic = CreateActiveClinic();

        var act = () => clinic.Activate();

        act.Should().NotThrow();
        clinic.Status.Should().Be(ClinicStatus.Active);
    }

    [Fact]
    public void Activate_WhenFirstActivation_ShouldSetActivatedAt()
    {
        var clinic = new Clinic
        {
            Name = "Test Clinic",
            Slug = "test-clinic",
            Status = ClinicStatus.PendingApproval,
            Phone = "01234567890"
        };
        var before = DateTime.UtcNow;

        clinic.Activate();

        clinic.ActivatedAt.Should().NotBeNull();
        clinic.ActivatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Activate_ShouldCallMarkAsUpdated_SettingUpdatedAt()
    {
        var clinic = new Clinic
        {
            Name = "Test Clinic",
            Slug = "test-clinic",
            Status = ClinicStatus.Suspended,
            Phone = "01234567890"
        };
        var before = DateTime.UtcNow;

        clinic.Activate();

        clinic.UpdatedAt.Should().NotBeNull();
        clinic.UpdatedAt.Should().BeOnOrAfter(before);
    }

    #endregion

    #region CanBeActivated

    [Fact]
    public void CanBeActivated_WhenStatusIsPendingApproval_ShouldReturnTrue()
    {
        var clinic = new Clinic
        {
            Name = "Test Clinic",
            Slug = "test-clinic",
            Status = ClinicStatus.PendingApproval,
            Phone = "01234567890"
        };

        clinic.CanBeActivated().Should().BeTrue();
    }

    [Fact]
    public void CanBeActivated_WhenStatusIsSuspended_ShouldReturnTrue()
    {
        var clinic = new Clinic
        {
            Name = "Test Clinic",
            Slug = "test-clinic",
            Status = ClinicStatus.Suspended,
            Phone = "01234567890"
        };

        clinic.CanBeActivated().Should().BeTrue();
    }

    [Fact]
    public void CanBeActivated_WhenStatusIsAlreadyActive_ShouldReturnFalse()
    {
        var clinic = CreateActiveClinic();

        clinic.CanBeActivated().Should().BeFalse();
    }

    [Fact]
    public void CanBeActivated_WhenClinicIsBanned_ShouldReturnFalse()
    {
        var clinic = CreateActiveClinic();
        clinic.Ban("Violation", "admin-123");

        clinic.CanBeActivated().Should().BeFalse();
    }

    #endregion
}
