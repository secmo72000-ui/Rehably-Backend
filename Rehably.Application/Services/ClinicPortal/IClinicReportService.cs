using Rehably.Application.Common;
using Rehably.Application.DTOs.ClinicPortal;

namespace Rehably.Application.Services.ClinicPortal;

public interface IClinicReportService
{
    Task<Result<ClinicReportSummaryDto>> GetSummaryAsync(Guid clinicId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<Result<PagedResult<SessionReportItemDto>>> GetSessionsReportAsync(Guid clinicId, ReportQueryParams query, CancellationToken ct = default);
}
