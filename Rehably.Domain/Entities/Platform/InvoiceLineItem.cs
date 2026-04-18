using Rehably.Domain.Entities.Base;

namespace Rehably.Domain.Entities.Platform;

public class InvoiceLineItem : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public string Description { get; set; } = string.Empty;

    public string ItemType { get; set; } = string.Empty;

    public Guid? ReferenceId { get; set; }

    public decimal Quantity { get; set; } = 1;

    public decimal UnitPrice { get; set; }

    public decimal Amount { get; set; }

    public int SortOrder { get; set; }
}
