namespace Rehably.Application.DTOs.Invoice;

public record CreateInvoiceDto
{
    public Guid SubscriptionId { get; init; }
    public decimal Amount { get; init; }
    public decimal TaxRate { get; init; }
    public DateTime BillingPeriodStart { get; init; }
    public DateTime BillingPeriodEnd { get; init; }
    public int? DueDays { get; init; } = 7;
}
