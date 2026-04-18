using Rehably.Application.Common;
using Rehably.Application.DTOs.Audit;
using Rehably.Application.DTOs.Platform;

namespace Rehably.Application.Services.Platform;

public interface IAuditLogService
{
    Task<Result<AuditLogListResponseDto>> GetAuditLogsAsync(AuditLogQueryDto query);
    Task<Result<ClinicActivityDto>> GetClinicActivityAsync(Guid clinicId, DateTime? startDate = null, DateTime? endDate = null);
}
