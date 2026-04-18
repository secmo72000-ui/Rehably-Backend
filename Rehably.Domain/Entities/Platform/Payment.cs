using Rehably.Domain.Entities.Base;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;

namespace Rehably.Domain.Entities.Platform;

public class Payment : BaseEntity
{
    public Guid ClinicId { get; set; }
    public Guid? InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";
    public PaymentProvider Provider { get; set; }
    public string? ProviderTransactionId { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? FailureReason { get; set; }
    public string? Metadata { get; set; }

    public Clinic Clinic { get; set; } = null!;
    public Invoice? Invoice { get; set; }

    #region Domain Methods

    /// <summary>
    /// Checks if the payment was successful.
    /// </summary>
    public bool IsSuccessful() => Status == PaymentStatus.Completed;

    /// <summary>
    /// Checks if the payment can be refunded.
    /// </summary>
    public bool CanRefund() => Status == PaymentStatus.Completed;

    /// <summary>
    /// Refunds the payment.
    /// </summary>
    public void Refund()
    {
        if (!CanRefund())
        {
            throw new InvalidOperationException("Payment cannot be refunded");
        }
        Status = PaymentStatus.Refunded;
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion
}
