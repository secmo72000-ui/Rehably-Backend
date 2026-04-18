namespace Rehably.Domain.Entities.Billing;

public class InstallmentPlan
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public decimal TotalAmount { get; set; }
    public int NumberOfInstallments { get; set; }
    public DateTime StartDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ClinicInvoice Invoice { get; set; } = null!;
    public ICollection<InstallmentSchedule> Schedule { get; set; } = new List<InstallmentSchedule>();
}
