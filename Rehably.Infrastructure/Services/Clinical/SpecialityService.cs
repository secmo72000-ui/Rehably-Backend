using Microsoft.EntityFrameworkCore;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Clinical;
using Rehably.Application.Services.Clinical;
using Rehably.Domain.Entities.Clinical;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Services.Clinical;

public class SpecialityService : ISpecialityService
{
    private readonly ApplicationDbContext _db;

    public SpecialityService(ApplicationDbContext db) => _db = db;

    // ── Global ─────────────────────────────────────────────────────────────────

    public async Task<Result<List<SpecialityDto>>> GetAllAsync(bool activeOnly = false, CancellationToken ct = default)
    {
        var query = _db.Specialities.AsQueryable();
        if (activeOnly) query = query.Where(s => s.IsActive);

        var items = await query
            .OrderBy(s => s.DisplayOrder).ThenBy(s => s.NameEn)
            .Select(s => new SpecialityDto(
                s.Id, s.Code, s.NameEn, s.NameAr, s.IcdChapters,
                s.Description, s.IconUrl, s.DisplayOrder, s.IsActive,
                s.Diagnoses.Count(d => !d.IsDeleted)))
            .ToListAsync(ct);

        return Result<List<SpecialityDto>>.Success(items);
    }

    public async Task<Result<SpecialityDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var s = await _db.Specialities
            .Include(x => x.Diagnoses)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

        if (s == null) return Result<SpecialityDto>.Failure("Speciality not found");

        return Result<SpecialityDto>.Success(Map(s));
    }

    public async Task<Result<SpecialityDto>> CreateAsync(CreateSpecialityRequest req, CancellationToken ct = default)
    {
        if (await _db.Specialities.AnyAsync(s => s.Code == req.Code.ToUpper(), ct))
            return Result<SpecialityDto>.Failure($"Speciality code '{req.Code}' already exists");

        var entity = new Speciality
        {
            Code = req.Code.ToUpper(),
            NameEn = req.NameEn,
            NameAr = req.NameAr,
            IcdChapters = req.IcdChapters,
            Description = req.Description,
            IconUrl = req.IconUrl,
            DisplayOrder = req.DisplayOrder,
            IsActive = true
        };

        _db.Specialities.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Result<SpecialityDto>.Success(Map(entity));
    }

    public async Task<Result<SpecialityDto>> UpdateAsync(Guid id, UpdateSpecialityRequest req, CancellationToken ct = default)
    {
        var entity = await _db.Specialities.FindAsync([id], ct);
        if (entity == null || entity.IsDeleted) return Result<SpecialityDto>.Failure("Speciality not found");

        if (req.NameEn != null) entity.NameEn = req.NameEn;
        if (req.NameAr != null) entity.NameAr = req.NameAr;
        if (req.IcdChapters != null) entity.IcdChapters = req.IcdChapters;
        if (req.Description != null) entity.Description = req.Description;
        if (req.IconUrl != null) entity.IconUrl = req.IconUrl;
        if (req.DisplayOrder.HasValue) entity.DisplayOrder = req.DisplayOrder.Value;
        if (req.IsActive.HasValue) entity.IsActive = req.IsActive.Value;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Result<SpecialityDto>.Success(Map(entity));
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.Specialities.FindAsync([id], ct);
        if (entity == null || entity.IsDeleted) return Result.Failure("Speciality not found");

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ── Seed defaults ──────────────────────────────────────────────────────────

    public async Task<Result<int>> SeedDefaultsAsync(CancellationToken ct = default)
    {
        var defaults = new[]
        {
            new { Code = "ORTHO",  NameEn = "Orthopedics",           NameAr = "العظام والمفاصل",      IcdChapters = "M",   Order = 1  },
            new { Code = "NEURO",  NameEn = "Neurology",             NameAr = "الأعصاب",               IcdChapters = "G",   Order = 2  },
            new { Code = "SPORTS", NameEn = "Sports Rehabilitation",  NameAr = "إعادة تأهيل رياضي",   IcdChapters = "S",   Order = 3  },
            new { Code = "PEDS",   NameEn = "Pediatrics",            NameAr = "الأطفال",               IcdChapters = "Q",   Order = 4  },
            new { Code = "GERI",   NameEn = "Geriatrics",            NameAr = "المسنين",               IcdChapters = "M,G", Order = 5  },
            new { Code = "CARD",   NameEn = "Cardiopulmonary",       NameAr = "القلب والرئة",          IcdChapters = "I,J", Order = 6  },
            new { Code = "BURNS",  NameEn = "Burns & Plastic",       NameAr = "الحروق والتجميل",      IcdChapters = "S",   Order = 7  },
            new { Code = "WOMH",   NameEn = "Women's Health",        NameAr = "صحة المرأة",            IcdChapters = "O,N", Order = 8  },
            new { Code = "ONCO",   NameEn = "Oncology Rehab",        NameAr = "إعادة تأهيل الأورام",   IcdChapters = "C",   Order = 9  },
            new { Code = "HAND",   NameEn = "Hand Therapy",          NameAr = "علاج اليد",             IcdChapters = "M,S", Order = 10 },
            new { Code = "SPINE",  NameEn = "Spine",                 NameAr = "العمود الفقري",         IcdChapters = "M",   Order = 11 },
            new { Code = "VEST",   NameEn = "Vestibular",            NameAr = "الدهليزي والتوازن",     IcdChapters = "H,G", Order = 12 },
        };

        var existingCodes = await _db.Specialities.Select(s => s.Code).ToListAsync(ct);

        var toAdd = defaults
            .Where(d => !existingCodes.Contains(d.Code))
            .Select(d => new Speciality
            {
                Code         = d.Code,
                NameEn       = d.NameEn,
                NameAr       = d.NameAr,
                IcdChapters  = d.IcdChapters,
                DisplayOrder = d.Order,
                IsActive     = true,
            })
            .ToList();

        if (toAdd.Count > 0)
        {
            _db.Specialities.AddRange(toAdd);
            await _db.SaveChangesAsync(ct);
        }

        return Result<int>.Success(toAdd.Count);
    }

    // ── Clinic assignment ──────────────────────────────────────────────────────

    public async Task<Result<List<ClinicSpecialityDto>>> GetClinicSpecialitiesAsync(Guid clinicId, CancellationToken ct = default)
    {
        var items = await _db.ClinicSpecialities
            .Where(cs => cs.ClinicId == clinicId)
            .Include(cs => cs.Speciality)
            .Select(cs => new ClinicSpecialityDto(
                cs.SpecialityId,
                cs.Speciality.Code,
                cs.Speciality.NameEn,
                cs.Speciality.NameAr,
                cs.AssignedAt))
            .ToListAsync(ct);

        return Result<List<ClinicSpecialityDto>>.Success(items);
    }

    public async Task<Result> AssignToClinicAsync(Guid clinicId, AssignSpecialitiesRequest request, CancellationToken ct = default)
    {
        var existing = await _db.ClinicSpecialities
            .Where(cs => cs.ClinicId == clinicId)
            .Select(cs => cs.SpecialityId)
            .ToListAsync(ct);

        var toAdd = request.SpecialityIds
            .Except(existing)
            .Select(sid => new ClinicSpeciality { ClinicId = clinicId, SpecialityId = sid })
            .ToList();

        if (toAdd.Count > 0)
        {
            _db.ClinicSpecialities.AddRange(toAdd);
            await _db.SaveChangesAsync(ct);
        }

        return Result.Success();
    }

    public async Task<Result> RemoveFromClinicAsync(Guid clinicId, Guid specialityId, CancellationToken ct = default)
    {
        var row = await _db.ClinicSpecialities
            .FirstOrDefaultAsync(cs => cs.ClinicId == clinicId && cs.SpecialityId == specialityId, ct);

        if (row == null) return Result.Failure("Speciality not assigned to this clinic");

        _db.ClinicSpecialities.Remove(row);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ── Mapping ────────────────────────────────────────────────────────────────

    private static SpecialityDto Map(Speciality s) => new(
        s.Id, s.Code, s.NameEn, s.NameAr, s.IcdChapters,
        s.Description, s.IconUrl, s.DisplayOrder, s.IsActive,
        s.Diagnoses?.Count(d => !d.IsDeleted) ?? 0);
}
