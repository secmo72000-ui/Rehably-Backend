using Rehably.Domain.Enums;

namespace Rehably.Domain.Entities.Billing;

public class ClinicInvoiceLineItem
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? DescriptionArabic { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal InsuranceCoverageAmount { get; set; } = 0;
    public decimal DiscountAmount { get; set; } = 0;
    public decimal LineTotal { get; set; }
    public BillingServiceType ServiceType { get; set; }

    public ClinicInvoice Invoice { get; set; } = null!;
}
