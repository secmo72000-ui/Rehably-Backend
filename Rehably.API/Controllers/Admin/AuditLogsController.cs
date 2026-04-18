using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rehably.API.Authorization;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Audit;
using Rehably.Application.DTOs.Platform;
using Rehably.Application.Services.Platform;
using Rehably.Domain.Enums;

namespace Rehably.API.Controllers.Admin;

/// <summary>
/// Admin audit log viewing for platform administrators.
/// </summary>
[ApiController]
[Route("api/admin/audit-logs")]
[Authorize]
[RequirePermission("audit.view")]
[Produces("application/json")]
[Tags("Admin - Audit Logs")]
public class AuditLogsController : BaseController
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Get all audit logs with filtering and pagination
    /// </summary>
    /// <param name="clinicId">Filter by clinic ID</param>
    /// <param name="userId">Filter by user ID</param>
    /// <param name="actionType">Filter by action type enum value</param>
    /// <param name="role">Filter by user role name</param>
    /// <param name="email">Filter by user email address</param>
    /// <param name="isSuccess">Filter by success status</param>
    /// <param name="startDate">Filter from date</param>
    /// <param name="endDate">Filter to date</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<AuditLogListResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuditLogListResponseDto>> GetAuditLogs(
        [FromQuery] Guid? clinicId = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] AuditActionType? actionType = null,
        [FromQuery] string? role = null,
        [FromQuery] string? email = null,
        [FromQuery] bool? isSuccess = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new AuditLogQueryDto
        {
            ClinicId = clinicId,
            UserId = userId,
            ActionType = actionType,
            Role = role,
            Email = email,
            IsSuccess = isSuccess,
            StartDate = startDate,
            EndDate = endDate,
            Page = page,
            PageSize = Math.Min(pageSize, 100)
        };

        var result = await _auditLogService.GetAuditLogsAsync(query);
        return FromResult(result);
    }

    /// <summary>
    /// Get clinic activity details including usage statistics and login history
    /// </summary>
    /// <param name="id">Clinic ID</param>
    /// <param name="startDate">Activity from date (default: last 30 days)</param>
    /// <param name="endDate">Activity to date (default: now)</param>
    [HttpGet("clinics/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ClinicActivityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClinicActivityDto>> GetClinicActivity(
        Guid id,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _auditLogService.GetClinicActivityAsync(id, startDate, endDate);
        return FromResult(result);
    }
}
