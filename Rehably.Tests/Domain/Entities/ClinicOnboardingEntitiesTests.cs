using FluentAssertions;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;

namespace Rehably.Tests.Domain.Entities;

public class ClinicOnboardingEntitiesTests
{
    private readonly Guid _testClinicId = Guid.NewGuid();

    [Fact]
    public void ClinicOnboarding_HasExpectedProperties()
    {
        var entity = new ClinicOnboarding
        {
            Id = Guid.NewGuid(),
            ClinicId = _testClinicId,
            CurrentStep = OnboardingStep.PendingEmailVerification,
            EmailVerifiedAt = DateTime.UtcNow,
            DocumentsUploadedAt = DateTime.UtcNow,
            ApprovedAt = DateTime.UtcNow,
            PaymentCompletedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        entity.Id.Should().NotBe(Guid.Empty);
        entity.ClinicId.Should().Be(_testClinicId);
        entity.CurrentStep.Should().Be(OnboardingStep.PendingEmailVerification);
        entity.EmailVerifiedAt.Should().NotBeNull();
        entity.DocumentsUploadedAt.Should().NotBeNull();
        entity.ApprovedAt.Should().NotBeNull();
        entity.PaymentCompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void ClinicDocument_HasExpectedProperties()
    {
        var entity = new ClinicDocument
        {
            Id = Guid.NewGuid(),
            ClinicId = _testClinicId,
            DocumentType = DocumentType.OwnerId,
            StorageUrl = "https://storage.cloudinary.com/clinics/123/doc.pdf",
            PublicUrl = "https://res.cloudinary.com/...",
            Status = DocumentStatus.Pending,
            RejectionReason = null,
            UploadedAt = DateTime.UtcNow,
            VerifiedAt = null
        };

        entity.Id.Should().NotBe(Guid.Empty);
        entity.ClinicId.Should().Be(_testClinicId);
        entity.DocumentType.Should().Be(DocumentType.OwnerId);
        entity.StorageUrl.Should().NotBeEmpty();
        entity.Status.Should().Be(DocumentStatus.Pending);
    }

    [Fact]
    public void UsageRecord_HasExpectedProperties()
    {
        var entity = new UsageRecord
        {
            Id = Guid.NewGuid(),
            ClinicId = _testClinicId,
            Metric = UsageMetric.PatientCount,
            Value = 50,
            RecordedAt = DateTime.UtcNow,
            Period = new DateTime(2026, 1, 1)
        };

        entity.Id.Should().NotBe(Guid.Empty);
        entity.ClinicId.Should().Be(_testClinicId);
        entity.Metric.Should().Be(UsageMetric.PatientCount);
        entity.Value.Should().Be(50);
        entity.Period.Should().Be(new DateTime(2026, 1, 1));
    }
}
