using Rehably.Application.DTOs.ClinicPortal;

namespace Rehably.Application.Services.ClinicPortal;

public interface IClinicWorkingHoursService
{
    /// <summary>Returns all 7 days (creates defaults on first call if none stored).</summary>
    Task<List<WorkingHoursDayDto>> GetAsync(Guid clinicId, CancellationToken ct = default);

    /// <summary>Upserts the full weekly schedule (replaces all existing rows).</summary>
    Task UpdateAsync(Guid clinicId, UpdateWorkingHoursRequest request, CancellationToken ct = default);
}
