using FluentAssertions;
using Rehably.Domain.Enums;

namespace Rehably.Tests.Domain.Enums;

public class ClinicOnboardingEnumsTests
{
    [Fact]
    public void ClinicStatus_HasExpectedValues()
    {
        var values = Enum.GetValues(typeof(ClinicStatus)).Cast<ClinicStatus>();

        values.Should().Contain(ClinicStatus.PendingEmailVerification);
        values.Should().Contain(ClinicStatus.PendingDocumentUpload);
        values.Should().Contain(ClinicStatus.PendingApproval);
        values.Should().Contain(ClinicStatus.PendingPayment);
        values.Should().Contain(ClinicStatus.Active);
        values.Should().Contain(ClinicStatus.Suspended);
        values.Should().Contain(ClinicStatus.Cancelled);
    }

    [Fact]
    public void OnboardingStep_HasExpectedValues()
    {
        var values = Enum.GetValues(typeof(OnboardingStep)).Cast<OnboardingStep>();

        values.Should().Contain(OnboardingStep.PendingEmailVerification);
        values.Should().Contain(OnboardingStep.PendingDocumentUpload);
        values.Should().Contain(OnboardingStep.PendingApproval);
        values.Should().Contain(OnboardingStep.PendingPayment);
        values.Should().Contain(OnboardingStep.Completed);
    }

    [Fact]
    public void DocumentType_HasExpectedValues()
    {
        var values = Enum.GetValues(typeof(DocumentType)).Cast<DocumentType>();

        values.Should().Contain(DocumentType.OwnerId);
        values.Should().Contain(DocumentType.MedicalLicense);
    }

    [Fact]
    public void DocumentStatus_HasExpectedValues()
    {
        var values = Enum.GetValues(typeof(DocumentStatus)).Cast<DocumentStatus>();

        values.Should().Contain(DocumentStatus.Pending);
        values.Should().Contain(DocumentStatus.Verified);
        values.Should().Contain(DocumentStatus.Rejected);
    }

    [Fact]
    public void PaymentProvider_HasExpectedValues()
    {
        var values = Enum.GetValues(typeof(PaymentProvider)).Cast<PaymentProvider>();

        values.Should().Contain(PaymentProvider.PayMob);
        values.Should().Contain(PaymentProvider.Stripe);
        values.Should().Contain(PaymentProvider.Cash);
    }

    [Fact]
    public void PaymentStatus_HasExpectedValues()
    {
        var values = Enum.GetValues(typeof(PaymentStatus)).Cast<PaymentStatus>();

        values.Should().Contain(PaymentStatus.Pending);
        values.Should().Contain(PaymentStatus.Processing);
        values.Should().Contain(PaymentStatus.Completed);
        values.Should().Contain(PaymentStatus.Failed);
        values.Should().Contain(PaymentStatus.Refunded);
    }

    [Fact]
    public void UsageMetric_HasExpectedValues()
    {
        var values = Enum.GetValues(typeof(UsageMetric)).Cast<UsageMetric>();

        values.Should().Contain(UsageMetric.PatientCount);
        values.Should().Contain(UsageMetric.UserCount);
        values.Should().Contain(UsageMetric.StorageUsed);
        values.Should().Contain(UsageMetric.SmsSent);
        values.Should().Contain(UsageMetric.WhatsappSent);
        values.Should().Contain(UsageMetric.EmailSent);
    }
}
