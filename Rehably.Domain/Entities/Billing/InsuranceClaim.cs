using Rehably.Domain.Enums;

namespace Rehably.Domain.Entities.Billing;

public class InsuranceClaim
{
    public Guid Id { get; set; }
    public Guid ClinicId { get; set; }
    public Guid PatientId { get; set; }
    public Guid PatientInsuranceId { get; set; }
    public Guid? InvoiceId { get; set; }
    public string? ClaimNumber { get; set; }
    public ClaimStatus Status { get; set; } = ClaimStatus.Draft;
    public DateTime? SubmittedAt { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public decimal? PaidAmount { get; set; }
    public string? RejectedReason { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public PatientInsurance PatientInsurance { get; set; } = null!;
}
