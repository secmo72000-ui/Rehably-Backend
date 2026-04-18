namespace Rehably.Domain.Enums;

public enum ClinicInvoiceStatus
{
    Draft = 0,
    Issued = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Cancelled = 4,
    Refunded = 5
}
