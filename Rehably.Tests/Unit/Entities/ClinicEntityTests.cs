using FluentAssertions;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;

namespace Rehably.Tests.Unit.Entities;

public class ClinicEntityTests
{
    private static Clinic CreateActiveClinic() => new Clinic
    {
        Name = "Test Clinic",
        Slug = "test-clinic",
        Phone = "01234567890",
        Status = ClinicStatus.Active
    };

    private static Clinic CreateSuspendedClinic() => new Clinic
    {
        Name = "Test Clinic",
        Slug = "test-clinic",
        Phone = "01234567890",
        Status = ClinicStatus.Suspended,
        SuspendedAt = DateTime.UtcNow.AddDays(-35),
        DataDeletionDate = DateTime.UtcNow.AddDays(-5)
    };

    #region Suspend

    [Fact]
    public void Suspend_ActiveClinic_SetsStatusSuspendedAndDeletionDate()
    {
        var clinic = CreateActiveClinic();

        clinic.Suspend();

        clinic.Status.Should().Be(ClinicStatus.Suspended);
        clinic.DataDeletionDate.Should().NotBeNull();
    }

    [Fact]
    public void Suspend_SetsDataDeletionDateTo30DaysFromNow()
    {
        var clinic = CreateActiveClinic();
        var before = DateTime.UtcNow;

        clinic.Suspend();

        clinic.DataDeletionDate.Should().NotBeNull();
        clinic.DataDeletionDate!.Value.Should().BeCloseTo(before.AddDays(30), TimeSpan.FromSeconds(5));
    }

    #endregion

    #region Reactivate

    [Fact]
    public void Reactivate_SuspendedClinic_ClearsAllSuspensionFields()
    {
        var clinic = CreateSuspendedClinic();

        clinic.Reactivate();

        clinic.Status.Should().Be(ClinicStatus.Active);
        clinic.SuspendedAt.Should().BeNull();
        clinic.DataDeletionDate.Should().BeNull();
    }

    #endregion

    #region IsSuspended

    [Fact]
    public void IsSuspended_SuspendedStatus_ReturnsTrue()
    {
        var clinic = new Clinic
        {
            Name = "Test Clinic",
            Slug = "test-clinic",
            Phone = "01234567890",
            Status = ClinicStatus.Suspended
        };

        clinic.IsSuspended.Should().BeTrue();
    }

    [Fact]
    public void IsSuspended_ActiveStatus_ReturnsFalse()
    {
        var clinic = CreateActiveClinic();

        clinic.IsSuspended.Should().BeFalse();
    }

    #endregion

    #region CanBeDeleted

    [Fact]
    public void CanBeDeleted_SuspendedAndDatePassed_ReturnsTrue()
    {
        var clinic = new Clinic
        {
            Name = "Test Clinic",
            Slug = "test-clinic",
            Phone = "01234567890",
            Status = ClinicStatus.Suspended,
            SuspendedAt = DateTime.UtcNow.AddDays(-35),
            DataDeletionDate = DateTime.UtcNow.AddDays(-1)
        };

        clinic.CanBeDeleted().Should().BeTrue();
    }

    [Fact]
    public void CanBeDeleted_SuspendedButDateNotReached_ReturnsFalse()
    {
        var clinic = new Clinic
        {
            Name = "Test Clinic",
            Slug = "test-clinic",
            Phone = "01234567890",
            Status = ClinicStatus.Suspended,
            SuspendedAt = DateTime.UtcNow.AddDays(-5),
            DataDeletionDate = DateTime.UtcNow.AddDays(25)
        };

        clinic.CanBeDeleted().Should().BeFalse();
    }

    [Fact]
    public void CanBeDeleted_NotSuspended_ReturnsFalse()
    {
        var clinic = CreateActiveClinic();

        clinic.CanBeDeleted().Should().BeFalse();
    }

    #endregion
}
