using Rehably.Application.Common;
using Rehably.Application.DTOs.Billing;

namespace Rehably.Application.Services.Billing;

public interface IClinicPaymentService
{
    Task<PagedResult<ClinicPaymentDto>> GetPaymentsAsync(Guid clinicId, PaymentQueryParams query);
    Task<ClinicPaymentDto?> GetPaymentByIdAsync(Guid clinicId, Guid id);
    Task<ClinicPaymentDto> RecordPaymentAsync(Guid clinicId, string recordedByUserId, RecordPaymentRequest request);
    Task<ClinicPaymentDto> RefundPaymentAsync(Guid clinicId, Guid id, RefundPaymentRequest request);
    Task<PaymentSummaryDto> GetSummaryAsync(Guid clinicId, DateTime? from, DateTime? to);
    Task<BillingPolicyDto> GetPolicyAsync(Guid clinicId);
    Task<BillingPolicyDto> UpsertPolicyAsync(Guid clinicId, UpdateBillingPolicyRequest request);
}
