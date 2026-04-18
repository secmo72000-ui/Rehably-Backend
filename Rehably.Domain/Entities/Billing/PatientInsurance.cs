namespace Rehably.Domain.Entities.Billing;

public class PatientInsurance
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid ClinicInsuranceProviderId { get; set; }
    public string? PolicyNumber { get; set; }
    public string? MembershipId { get; set; }
    public string? HolderName { get; set; }
    public decimal CoveragePercent { get; set; }          // overrides clinic default
    public decimal? MaxAnnualCoverageAmount { get; set; } // optional cap
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ClinicInsuranceProvider ClinicInsuranceProvider { get; set; } = null!;
    public ICollection<InsuranceClaim> Claims { get; set; } = new List<InsuranceClaim>();
}
