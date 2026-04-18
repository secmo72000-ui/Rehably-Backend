using FluentAssertions;
using Rehably.Application.DTOs.Clinic;
using Rehably.Domain.Enums;
using Xunit;

namespace Rehably.Tests.Services.ClinicSecurity;

/// <summary>
/// Security tests verifying that GET responses never expose sensitive fields.
/// T024: ClinicResponse must not expose TempPassword.
/// </summary>
public class ClinicResponseSecurityTests
{
    [Fact]
    public void GetClinic_ById_DoesNotReturnTempPassword()
    {
        var response = new ClinicResponse
        {
            Id = Guid.NewGuid(),
            Name = "Test Clinic",
            Slug = "test-clinic",
            Phone = "01000000000",
            Status = ClinicStatus.Active,
            SubscriptionStatus = SubscriptionStatus.Active,
            SubscriptionStartDate = DateTime.UtcNow
        };

        var type = typeof(ClinicResponse);
        var hasTempPassword = type.GetProperty("TempPassword") != null;

        hasTempPassword.Should().BeFalse(
            "ClinicResponse must not expose TempPassword — it is a security violation");
    }

    [Fact]
    public void ClinicResponse_HasPaymentMethod_NotTempPassword()
    {
        var type = typeof(ClinicResponse);

        type.GetProperty("PaymentMethod").Should().NotBeNull(
            "ClinicResponse should expose PaymentMethod for admin visibility");

        type.GetProperty("TempPassword").Should().BeNull(
            "ClinicResponse must not expose TempPassword");
    }
}
