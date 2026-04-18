using Rehably.Domain.Entities.Base;
using Rehably.Domain.Entities.Tenant;

namespace Rehably.Domain.Entities.Platform;

public class Invoice : BaseEntity
{
    public Guid ClinicId { get; set; }
    public Guid SubscriptionId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal TaxRate { get; set; } = 14.00m;
    public decimal TaxAmount { get; set; }
    public decimal AddOnsAmount { get; set; } = 0m;
    public decimal TotalAmount { get; set; }
    public DateTime BillingPeriodStart { get; set; }
    public DateTime BillingPeriodEnd { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaidVia { get; set; }
    public string? Notes { get; set; }

    public Clinic Clinic { get; set; } = null!;
    public Subscription Subscription { get; set; } = null!;
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public List<InvoiceLineItem> LineItems { get; set; } = new();

    #region Domain Methods

    /// <summary>
    /// Checks if the invoice can be paid (not already paid and not past due date).
    /// </summary>
    public bool CanBePaid() => PaidAt == null && DueDate >= DateTime.UtcNow;

    /// <summary>
    /// Checks if the invoice is overdue (past due date and not paid).
    /// </summary>
    public bool IsOverdue() => DueDate < DateTime.UtcNow && PaidAt == null;

    #endregion
}
