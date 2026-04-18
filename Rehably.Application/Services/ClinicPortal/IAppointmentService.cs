using Rehably.Application.Common;
using Rehably.Application.DTOs.ClinicPortal;

namespace Rehably.Application.Services.ClinicPortal;

public interface IAppointmentService
{
    Task<Result<PagedResult<AppointmentDto>>> GetAllAsync(AppointmentQueryParams query, CancellationToken ct = default);
    Task<Result<AppointmentDto>> GetByIdAsync(Guid appointmentId, CancellationToken ct = default);
    Task<Result<List<AppointmentDto>>> GetByDateRangeAsync(DateTime from, DateTime to, string? therapistId = null, CancellationToken ct = default);
    Task<Result<AppointmentDto>> CreateAsync(CreateAppointmentRequest request, CancellationToken ct = default);
    Task<Result<AppointmentDto>> UpdateAsync(Guid appointmentId, UpdateAppointmentRequest request, CancellationToken ct = default);
    Task<Result<AppointmentDto>> ConfirmAsync(Guid appointmentId, CancellationToken ct = default);
    Task<Result<AppointmentDto>> CompleteAsync(Guid appointmentId, CancellationToken ct = default);
    Task<Result<AppointmentDto>> CancelAsync(Guid appointmentId, string reason, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid appointmentId, CancellationToken ct = default);
}
