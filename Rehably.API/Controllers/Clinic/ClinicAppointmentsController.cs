using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rehably.Application.Common;
using Rehably.Application.DTOs.ClinicPortal;
using Rehably.Application.Services.ClinicPortal;

namespace Rehably.API.Controllers.Clinic;

[ApiController]
[Route("api/clinic/appointments")]
[Authorize]
[Produces("application/json")]
[Tags("Clinic - Appointments")]
public class ClinicAppointmentsController : BaseController
{
    private readonly IAppointmentService _appointmentService;

    public ClinicAppointmentsController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    /// <summary>
    /// Get all appointments with filtering and pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AppointmentDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AppointmentDto>>> GetAppointments(
        [FromQuery] AppointmentQueryParams query,
        CancellationToken cancellationToken = default)
    {
        var result = await _appointmentService.GetAllAsync(query, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Get appointments by date range (for calendar view).
    /// </summary>
    [HttpGet("calendar")]
    [ProducesResponseType(typeof(ApiResponse<List<AppointmentDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AppointmentDto>>> GetCalendar(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] string? therapistId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _appointmentService.GetByDateRangeAsync(from, to, therapistId, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Get an appointment by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentDto>> GetAppointment(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _appointmentService.GetByIdAsync(id, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Create a new appointment.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AppointmentDto>> CreateAppointment(
        [FromBody] CreateAppointmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _appointmentService.CreateAsync(request, cancellationToken);
        return FromResult(result, 201);
    }

    /// <summary>
    /// Update an existing appointment.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentDto>> UpdateAppointment(
        Guid id,
        [FromBody] UpdateAppointmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _appointmentService.UpdateAsync(id, request, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Reception check-in: patient arrived and payment confirmed.
    /// Transitions status: Scheduled/Confirmed → CheckedIn.
    /// </summary>
    [HttpPost("{id:guid}/checkin")]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AppointmentDto>> CheckIn(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _appointmentService.CheckInAsync(id, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Confirm an appointment.
    /// </summary>
    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentDto>> ConfirmAppointment(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _appointmentService.ConfirmAsync(id, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Mark an appointment as completed.
    /// </summary>
    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentDto>> CompleteAppointment(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _appointmentService.CompleteAsync(id, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Cancel an appointment with a reason.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentDto>> CancelAppointment(
        Guid id,
        [FromBody] string reason,
        CancellationToken cancellationToken = default)
    {
        var result = await _appointmentService.CancelAsync(id, reason, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Delete an appointment (soft delete).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteAppointment(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _appointmentService.DeleteAsync(id, cancellationToken);
        return FromResult(result, 204);
    }
}
