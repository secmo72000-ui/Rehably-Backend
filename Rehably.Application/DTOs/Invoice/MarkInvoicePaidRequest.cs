namespace Rehably.Application.DTOs.Invoice;

public record MarkInvoicePaidRequest
{
    public string PaymentMethod { get; init; } = "cash";
    public string? TransactionId { get; init; }
    public string? Notes { get; init; }
}
