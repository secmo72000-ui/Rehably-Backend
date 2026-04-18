using Rehably.Domain.Enums;

namespace Rehably.Domain.Entities.Billing;

public class ClinicInvoice
{
    public Guid Id { get; set; }
    public Guid ClinicId { get; set; }
    public Guid PatientId { get; set; }
    public Guid? AppointmentId { get; set; }
    public Guid? TreatmentPlanId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public ClinicInvoiceStatus Status { get; set; } = ClinicInvoiceStatus.Draft;
    public decimal SubTotal { get; set; }
    public decimal InsuranceCoverageAmount { get; set; } = 0;
    public decimal DiscountAmount { get; set; } = 0;
    public decimal TaxAmount { get; set; } = 0;
    public decimal TotalDue { get; set; }
    public decimal TotalPaid { get; set; } = 0;
    public string Currency { get; set; } = "EGP";
    public DateTime? DueDate { get; set; }
    public string? Notes { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ClinicInvoiceLineItem> LineItems { get; set; } = new List<ClinicInvoiceLineItem>();
    public ICollection<ClinicPayment> Payments { get; set; } = new List<ClinicPayment>();
    public ICollection<InsuranceClaim> Claims { get; set; } = new List<InsuranceClaim>();
    public InstallmentPlan? InstallmentPlan { get; set; }
}
