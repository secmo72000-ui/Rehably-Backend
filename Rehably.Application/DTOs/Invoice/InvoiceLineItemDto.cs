namespace Rehably.Application.DTOs.Invoice;

public record InvoiceLineItemDto
{
    public Guid Id { get; init; }
    public Guid InvoiceId { get; init; }
    public string Description { get; init; } = string.Empty;
    public string ItemType { get; init; } = string.Empty;
    public Guid? ReferenceId { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Amount { get; init; }
    public int SortOrder { get; init; }
}
