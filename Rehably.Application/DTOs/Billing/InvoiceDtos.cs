using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Billing;

public record ClinicInvoiceDto(
    Guid Id,
    string InvoiceNumber,
    Guid PatientId,
    string PatientName,
    Guid? AppointmentId,
    Guid? TreatmentPlanId,
    ClinicInvoiceStatus Status,
    decimal SubTotal,
    decimal InsuranceCoverageAmount,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalDue,
    decimal TotalPaid,
    decimal Balance,
    string Currency,
    DateTime? DueDate,
    string? Notes,
    DateTime? IssuedAt,
    DateTime? PaidAt,
    DateTime CreatedAt,
    List<InvoiceLineItemDto> LineItems,
    InstallmentPlanDto? InstallmentPlan
);

public record InvoiceLineItemDto(
    Guid Id,
    string Description,
    string? DescriptionArabic,
    int Quantity,
    decimal UnitPrice,
    decimal InsuranceCoverageAmount,
    decimal DiscountAmount,
    decimal LineTotal,
    BillingServiceType ServiceType
);

public record InstallmentPlanDto(
    Guid Id,
    decimal TotalAmount,
    int NumberOfInstallments,
    DateTime StartDate,
    string? Notes,
    List<InstallmentScheduleDto> Schedule
);

public record InstallmentScheduleDto(
    Guid Id,
    DateTime DueDate,
    decimal Amount,
    InstallmentStatus Status,
    Guid? PaymentId
);

public record CreateInvoiceRequest(
    Guid PatientId,
    Guid? AppointmentId,
    Guid? TreatmentPlanId,
    string Currency,
    DateTime? DueDate,
    string? Notes,
    List<CreateLineItemRequest> LineItems,
    Guid? PatientInsuranceId,
    List<Guid>? DiscountIds
);

public record CreateLineItemRequest(
    string Description,
    string? DescriptionArabic,
    int Quantity,
    decimal UnitPrice,
    BillingServiceType ServiceType
);

public record UpdateInvoiceRequest(
    ClinicInvoiceStatus Status,
    DateTime? DueDate,
    string? Notes
);

public record CreateInstallmentPlanRequest(
    int NumberOfInstallments,
    DateTime StartDate,
    string? Notes
);

public record InvoiceSummaryDto(
    Guid Id,
    string InvoiceNumber,
    Guid PatientId,
    string PatientName,
    ClinicInvoiceStatus Status,
    decimal TotalDue,
    decimal TotalPaid,
    decimal Balance,
    string Currency,
    DateTime? DueDate,
    DateTime CreatedAt
);

public record InvoiceQueryParams
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public ClinicInvoiceStatus? Status { get; init; }
    public Guid? PatientId { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public string? Search { get; init; }
}

public record BillingBreakdownRequest(
    Guid PatientId,
    Guid? PatientInsuranceId,
    List<CreateLineItemRequest> LineItems,
    List<Guid>? DiscountIds,
    string? PromoCode
);

public record BillingBreakdownResponse(
    decimal SubTotal,
    decimal InsuranceCoverageAmount,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalDue,
    decimal PatientDue,
    decimal InsuranceDue,
    List<BillingBreakdownLineDto> Lines,
    string Currency
);

public record BillingBreakdownLineDto(
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal InsuranceCoverage,
    decimal Discount,
    decimal LineTotal
);
