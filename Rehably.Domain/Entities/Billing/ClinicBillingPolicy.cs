using Rehably.Domain.Enums;

namespace Rehably.Domain.Entities.Billing;

public class ClinicBillingPolicy
{
    public Guid Id { get; set; }
    public Guid ClinicId { get; set; }
    public PaymentTiming DefaultPaymentTiming { get; set; } = PaymentTiming.AtBooking;
    public bool AllowInstallments { get; set; } = false;
    public bool AllowDiscountStackWithInsurance { get; set; } = false;
    public bool AllowMultipleDiscounts { get; set; } = false;
    public bool RequirePreAuthForInsurance { get; set; } = false;
    public string DefaultCurrency { get; set; } = "EGP";
    public decimal? TaxRatePercent { get; set; }
    public string InvoicePrefix { get; set; } = "INV-";
    public int NextInvoiceNumber { get; set; } = 1;
    public bool AutoGenerateInvoice { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
