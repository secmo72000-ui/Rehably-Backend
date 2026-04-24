using Microsoft.EntityFrameworkCore;
using Rehably.Application.DTOs.ClinicPortal;
using Rehably.Application.Services.ClinicPortal;
using Rehably.Domain.Entities.ClinicPortal;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Services.ClinicPortal;

public class ClinicWorkingHoursService : IClinicWorkingHoursService
{
    private readonly ApplicationDbContext _db;

    private static readonly string[] DayNamesAr =
        ["الأحد", "الاثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة", "السبت"];

    private static readonly string[] DayNamesEn =
        ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

    /// Default schedule: Fri = closed, all others 09:00-17:00
    private static readonly (bool isOpen, string? open, string? close)[] Defaults =
    [
        (true,  "09:00", "17:00"), // Sun
        (true,  "09:00", "17:00"), // Mon
        (true,  "09:00", "17:00"), // Tue
        (true,  "09:00", "17:00"), // Wed
        (true,  "09:00", "17:00"), // Thu
        (false, null,    null),    // Fri — weekend
        (true,  "09:00", "14:00"), // Sat — half day
    ];

    public ClinicWorkingHoursService(ApplicationDbContext db) => _db = db;

    public async Task<List<WorkingHoursDayDto>> GetAsync(Guid clinicId, CancellationToken ct = default)
    {
        var rows = await _db.ClinicWorkingHours
            .Where(w => w.ClinicId == clinicId)
            .OrderBy(w => w.DayOfWeek)
            .ToListAsync(ct);

        // First-time call — seed defaults and return them
        if (rows.Count == 0)
        {
            rows = Enumerable.Range(0, 7).Select(d => new ClinicWorkingHours
            {
                Id        = Guid.NewGuid(),
                ClinicId  = clinicId,
                DayOfWeek = d,
                IsOpen    = Defaults[d].isOpen,
                OpenTime  = Defaults[d].open,
                CloseTime = Defaults[d].close,
                UpdatedAt = DateTime.UtcNow,
            }).ToList();

            _db.ClinicWorkingHours.AddRange(rows);
            await _db.SaveChangesAsync(ct);
        }

        return rows.Select(r => new WorkingHoursDayDto(
            r.DayOfWeek,
            DayNamesAr[r.DayOfWeek],
            DayNamesEn[r.DayOfWeek],
            r.IsOpen,
            r.OpenTime,
            r.CloseTime
        )).ToList();
    }

    public async Task UpdateAsync(Guid clinicId, UpdateWorkingHoursRequest request, CancellationToken ct = default)
    {
        var existing = await _db.ClinicWorkingHours
            .Where(w => w.ClinicId == clinicId)
            .ToListAsync(ct);

        foreach (var dto in request.Schedule)
        {
            var row = existing.FirstOrDefault(r => r.DayOfWeek == dto.DayOfWeek);
            if (row is null)
            {
                row = new ClinicWorkingHours { Id = Guid.NewGuid(), ClinicId = clinicId, DayOfWeek = dto.DayOfWeek };
                _db.ClinicWorkingHours.Add(row);
            }

            row.IsOpen    = dto.IsOpen;
            row.OpenTime  = dto.IsOpen ? dto.OpenTime  : null;
            row.CloseTime = dto.IsOpen ? dto.CloseTime : null;
            row.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }
}
