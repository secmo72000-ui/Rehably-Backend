namespace Rehably.Application.DTOs.Invoice;

public record AdminInvoiceDto
{
    /// <summary>Invoice identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Human-readable invoice number.</summary>
    public string InvoiceNumber { get; init; } = string.Empty;

    /// <summary>Clinic identifier.</summary>
    public Guid ClinicId { get; init; }

    /// <summary>Clinic display name.</summary>
    public string ClinicName { get; init; } = string.Empty;

    /// <summary>Clinic contact email.</summary>
    public string ClinicEmail { get; init; } = string.Empty;

    /// <summary>Clinic contact phone.</summary>
    public string ClinicPhone { get; init; } = string.Empty;

    /// <summary>Clinic VAT registration number.</summary>
    public string? ClinicVatNumber { get; init; }

    /// <summary>Clinic billing address.</summary>
    public string ClinicAddress { get; init; } = string.Empty;

    /// <summary>Clinic tax identification number.</summary>
    public string? TaxIdentificationNumber { get; init; }

    /// <summary>Clinic country.</summary>
    public string ClinicCountry { get; init; } = string.Empty;

    /// <summary>Package identifier.</summary>
    public Guid PackageId { get; init; }

    /// <summary>Package display name.</summary>
    public string PackageName { get; init; } = string.Empty;

    /// <summary>Base amount before tax.</summary>
    public decimal Amount { get; init; }

    /// <summary>Currency code.</summary>
    public string Currency { get; init; } = "EGP";

    /// <summary>Tax rate percentage applied.</summary>
    public decimal TaxRate { get; init; }

    /// <summary>Tax amount.</summary>
    public decimal TaxAmount { get; init; }

    /// <summary>Add-on charges total.</summary>
    public decimal AddOnsAmount { get; init; }

    /// <summary>Total amount including tax.</summary>
    public decimal TotalAmount { get; init; }

    /// <summary>Billing period start date.</summary>
    public DateTime BillingPeriodStart { get; init; }

    /// <summary>Billing period end date.</summary>
    public DateTime BillingPeriodEnd { get; init; }

    /// <summary>Payment due date.</summary>
    public DateTime DueDate { get; init; }

    /// <summary>Date the invoice was paid, if paid.</summary>
    public DateTime? PaidAt { get; init; }

    /// <summary>Payment method used.</summary>
    public string? PaidVia { get; init; }

    /// <summary>Human-readable payment status.</summary>
    public string PaymentStatus { get; init; } = string.Empty;

    /// <summary>Transaction type (Online, Cash, etc.).</summary>
    public string TransactionType { get; init; } = string.Empty;

    /// <summary>Line items on this invoice.</summary>
    public List<InvoiceLineItemDto> LineItems { get; init; } = new();
}
