using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rehably.Application.Common;
using Rehably.Application.DTOs.ClinicPortal;
using Rehably.Application.Services.ClinicPortal;

namespace Rehably.API.Controllers.Clinic;

[ApiController]
[Route("api/clinic/reports")]
[Authorize]
[Produces("application/json")]
[Tags("Clinic - Reports")]
public class ClinicReportsController : BaseController
{
    private readonly IClinicReportService _reportService;

    public ClinicReportsController(IClinicReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ClinicReportSummaryDto>> GetSummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct = default)
    {
        var dateFrom = from ?? DateTime.UtcNow.AddMonths(-1);
        var dateTo   = to   ?? DateTime.UtcNow;
        var result   = await _reportService.GetSummaryAsync(TenantId ?? Guid.Empty, dateFrom, dateTo, ct);
        return FromResult(result);
    }

    [HttpGet("sessions")]
    public async Task<ActionResult<PagedResult<SessionReportItemDto>>> GetSessions(
        [FromQuery] ReportQueryParams query,
        CancellationToken ct = default)
    {
        var result = await _reportService.GetSessionsReportAsync(TenantId ?? Guid.Empty, query, ct);
        return FromResult(result);
    }
}
