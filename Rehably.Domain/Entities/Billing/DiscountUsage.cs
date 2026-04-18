namespace Rehably.Domain.Entities.Billing;

public class DiscountUsage
{
    public Guid Id { get; set; }
    public Guid DiscountId { get; set; }
    public Guid PatientId { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid? AppointmentId { get; set; }
    public Guid? TreatmentPlanId { get; set; }
    public decimal AmountApplied { get; set; }
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public string AppliedByUserId { get; set; } = string.Empty;

    public Discount Discount { get; set; } = null!;
}
