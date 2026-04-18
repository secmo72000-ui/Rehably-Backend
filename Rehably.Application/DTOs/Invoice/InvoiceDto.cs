using Rehably.Application.DTOs.Platform;
using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Invoice;

public record InvoiceDto
{
    public Guid Id { get; init; }
    public Guid ClinicId { get; init; }
    public Guid SubscriptionId { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public decimal TaxRate { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal AddOnsAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public DateTime BillingPeriodStart { get; init; }
    public DateTime BillingPeriodEnd { get; init; }
    public DateTime DueDate { get; init; }
    public DateTime? PaidAt { get; init; }
    public string? PaidVia { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsOverdue => DateTime.UtcNow > DueDate && PaidAt == null;
    public List<PaymentDto> Payments { get; init; } = new();
    public List<InvoiceLineItemDto> LineItems { get; init; } = new();
}
