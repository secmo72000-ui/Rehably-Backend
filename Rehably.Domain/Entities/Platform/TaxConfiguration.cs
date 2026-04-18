using Rehably.Domain.Entities.Base;

namespace Rehably.Domain.Entities.Platform;

public class TaxConfiguration : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? CountryCode { get; set; }

    public decimal TaxRate { get; set; }

    public DateTime EffectiveDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public bool IsDefault { get; set; }

    public string? CreatedBy { get; set; }
}
