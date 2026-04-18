namespace Rehably.Application.DTOs.Invoice;

public class CreateInvoicePaymentRequest
{
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";
    public string Provider { get; set; } = string.Empty;
}
