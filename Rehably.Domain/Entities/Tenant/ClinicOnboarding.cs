using Rehably.Domain.Enums;

namespace Rehably.Domain.Entities.Tenant;

public class ClinicOnboarding
{
    public Guid Id { get; set; }
    public Guid ClinicId { get; set; }
    public OnboardingStep CurrentStep { get; set; }
    public OnboardingType? OnboardingType { get; set; }
    public string? PreferredSlug { get; set; }
    public Guid? SelectedPackageId { get; set; }
    public BillingCycle? SelectedBillingCycle { get; set; }
    public string? SelectedFeatures { get; set; }
    public DateTime? PackageSelectedAt { get; set; }
    public DateTime? EmailVerifiedAt { get; set; }
    public DateTime? DocumentsUploadedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime? PaymentCompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Clinic? Clinic { get; set; }
}
