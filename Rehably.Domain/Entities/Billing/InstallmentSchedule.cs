using Rehably.Domain.Enums;

namespace Rehably.Domain.Entities.Billing;

public class InstallmentSchedule
{
    public Guid Id { get; set; }
    public Guid InstallmentPlanId { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public InstallmentStatus Status { get; set; } = InstallmentStatus.Pending;
    public Guid? PaymentId { get; set; }

    public InstallmentPlan Plan { get; set; } = null!;
}
