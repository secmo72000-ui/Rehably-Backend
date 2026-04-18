namespace Rehably.Domain.Entities.Billing;

public class InsuranceProvider
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameArabic { get; set; }
    public string? Country { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsGlobal { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ClinicInsuranceProvider> ClinicProviders { get; set; } = new List<ClinicInsuranceProvider>();
}
