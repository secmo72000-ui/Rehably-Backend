namespace Rehably.Domain.Entities.Billing;

public class ClinicInsuranceProvider
{
    public Guid Id { get; set; }
    public Guid ClinicId { get; set; }
    public Guid InsuranceProviderId { get; set; }
    public bool PreAuthRequired { get; set; } = false;
    public decimal DefaultCoveragePercent { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public InsuranceProvider Provider { get; set; } = null!;
    public ICollection<PatientInsurance> PatientPolicies { get; set; } = new List<PatientInsurance>();
    public ICollection<InsuranceServiceRule> ServiceRules { get; set; } = new List<InsuranceServiceRule>();
}
