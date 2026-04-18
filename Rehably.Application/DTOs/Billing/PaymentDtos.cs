using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Billing;

public record ClinicPaymentDto(
    Guid Id,
    Guid InvoiceId,
    string InvoiceNumber,
    Guid PatientId,
    string PatientName,
    decimal Amount,
    PaymentMethod Method,
    PaymentStatus Status,
    string? TransactionReference,
    string? PaymentGateway,
    DateTime? PaidAt,
    string? Notes,
    string RecordedByUserId,
    DateTime CreatedAt
);

public record RecordPaymentRequest(
    Guid InvoiceId,
    decimal Amount,
    PaymentMethod Method,
    string? TransactionReference,
    string? Notes
);

public record RefundPaymentRequest(
    string? Reason
);

public record BillingPolicyDto(
    Guid Id,
    PaymentTiming DefaultPaymentTiming,
    bool AllowInstallments,
    bool AllowDiscountStackWithInsurance,
    bool AllowMultipleDiscounts,
    bool RequirePreAuthForInsurance,
    string DefaultCurrency,
    decimal? TaxRatePercent,
    string InvoicePrefix,
    bool AutoGenerateInvoice
);

public record UpdateBillingPolicyRequest(
    PaymentTiming DefaultPaymentTiming,
    bool AllowInstallments,
    bool AllowDiscountStackWithInsurance,
    bool AllowMultipleDiscounts,
    bool RequirePreAuthForInsurance,
    string DefaultCurrency,
    decimal? TaxRatePercent,
    string InvoicePrefix,
    bool AutoGenerateInvoice
);

public record PaymentQueryParams
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public Guid? PatientId { get; init; }
    public Guid? InvoiceId { get; init; }
    public PaymentMethod? Method { get; init; }
    public PaymentStatus? Status { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}

public record PaymentSummaryDto(
    decimal TotalCollected,
    decimal TotalPending,
    decimal TotalRefunded,
    int TotalTransactions,
    Dictionary<string, decimal> ByMethod
);
