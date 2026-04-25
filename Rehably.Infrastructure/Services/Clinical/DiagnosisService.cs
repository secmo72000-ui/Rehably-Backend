using Microsoft.EntityFrameworkCore;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Clinical;
using Rehably.Application.Services.Clinical;
using Rehably.Domain.Entities.Clinical;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Services.Clinical;

public class DiagnosisService : IDiagnosisService
{
    private readonly ApplicationDbContext _db;

    public DiagnosisService(ApplicationDbContext db) => _db = db;

    // ── Admin ──────────────────────────────────────────────────────────────────

    public async Task<Result<PagedResult<DiagnosisListItem>>> GetAllAsync(DiagnosisQueryParams q, CancellationToken ct = default)
    {
        var query = _db.Diagnoses
            .Include(d => d.Speciality)
            .Include(d => d.BodyRegion)
            .Where(d => !d.IsDeleted)
            .AsQueryable();

        if (q.SpecialityId.HasValue) query = query.Where(d => d.SpecialityId == q.SpecialityId);
        if (q.BodyRegionCategoryId.HasValue) query = query.Where(d => d.BodyRegionCategoryId == q.BodyRegionCategoryId);
        if (q.IsGlobal.HasValue) query = query.Where(d => q.IsGlobal.Value ? d.ClinicId == null : d.ClinicId != null);
        if (q.IsActive.HasValue) query = query.Where(d => d.IsActive == q.IsActive);
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.ToLower();
            query = query.Where(d =>
                d.IcdCode.ToLower().Contains(s) ||
                d.NameEn.ToLower().Contains(s) ||
                d.NameAr.Contains(s));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(d => d.IcdCode)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(d => new DiagnosisListItem(
                d.Id, d.IcdCode, d.NameEn, d.NameAr,
                d.Speciality.NameAr,
                d.BodyRegion != null ? d.BodyRegion.NameArabic : null,
                d.ClinicId == null,
                d.IsActive))
            .ToListAsync(ct);

        return Result<PagedResult<DiagnosisListItem>>.Success(
            PagedResult<DiagnosisListItem>.Create(items, total, q.Page, q.PageSize));
    }

    public async Task<Result<DiagnosisDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var d = await _db.Diagnoses
            .Include(x => x.Speciality)
            .Include(x => x.BodyRegion)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

        if (d == null) return Result<DiagnosisDto>.Failure("Diagnosis not found");
        return Result<DiagnosisDto>.Success(Map(d));
    }

    public async Task<Result<DiagnosisDto>> CreateGlobalAsync(CreateDiagnosisRequest req, CancellationToken ct = default)
    {
        if (await _db.Diagnoses.AnyAsync(d => d.IcdCode == req.IcdCode && d.ClinicId == null, ct))
            return Result<DiagnosisDto>.Failure($"ICD code '{req.IcdCode}' already exists in the global list");

        return await CreateInternal(null, req, ct);
    }

    public async Task<Result<DiagnosisDto>> UpdateAsync(Guid id, UpdateDiagnosisRequest req, CancellationToken ct = default)
    {
        var d = await _db.Diagnoses
            .Include(x => x.Speciality)
            .Include(x => x.BodyRegion)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

        if (d == null) return Result<DiagnosisDto>.Failure("Diagnosis not found");

        if (req.BodyRegionCategoryId.HasValue) d.BodyRegionCategoryId = req.BodyRegionCategoryId;
        if (req.NameEn != null) d.NameEn = req.NameEn;
        if (req.NameAr != null) d.NameAr = req.NameAr;
        if (req.Description != null) d.Description = req.Description;
        if (req.IsActive.HasValue) d.IsActive = req.IsActive.Value;
        if (req.DefaultProtocolName != null) d.DefaultProtocolName = req.DefaultProtocolName;
        if (req.DefaultExerciseIds != null) d.DefaultExerciseIds = req.DefaultExerciseIds;
        if (req.SuggestedSessions.HasValue) d.SuggestedSessions = req.SuggestedSessions;
        if (req.SuggestedDurationWeeks.HasValue) d.SuggestedDurationWeeks = req.SuggestedDurationWeeks;
        d.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Result<DiagnosisDto>.Success(Map(d));
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var d = await _db.Diagnoses.FindAsync([id], ct);
        if (d == null || d.IsDeleted) return Result.Failure("Diagnosis not found");

        d.IsDeleted = true;
        d.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ── Clinic ─────────────────────────────────────────────────────────────────

    public async Task<Result<List<DiagnosisListItem>>> GetForClinicAsync(
        Guid clinicId, Guid? specialityId, Guid? bodyRegionId, string? search, CancellationToken ct = default)
    {
        // Get clinic's assigned speciality IDs
        var clinicSpecialityIds = await _db.ClinicSpecialities
            .Where(cs => cs.ClinicId == clinicId)
            .Select(cs => cs.SpecialityId)
            .ToListAsync(ct);

        var query = _db.Diagnoses
            .Include(d => d.Speciality)
            .Include(d => d.BodyRegion)
            .Where(d => !d.IsDeleted && d.IsActive)
            .Where(d =>
                // Global diagnoses for clinic's specialities
                (d.ClinicId == null && clinicSpecialityIds.Contains(d.SpecialityId)) ||
                // Clinic's own custom diagnoses
                d.ClinicId == clinicId)
            .AsQueryable();

        if (specialityId.HasValue) query = query.Where(d => d.SpecialityId == specialityId);
        if (bodyRegionId.HasValue) query = query.Where(d => d.BodyRegionCategoryId == bodyRegionId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(d =>
                d.IcdCode.ToLower().Contains(s) ||
                d.NameEn.ToLower().Contains(s) ||
                d.NameAr.Contains(s));
        }

        var items = await query
            .OrderBy(d => d.IcdCode)
            .Take(200)
            .Select(d => new DiagnosisListItem(
                d.Id, d.IcdCode, d.NameEn, d.NameAr,
                d.Speciality.NameAr,
                d.BodyRegion != null ? d.BodyRegion.NameArabic : null,
                d.ClinicId == null,
                d.IsActive))
            .ToListAsync(ct);

        return Result<List<DiagnosisListItem>>.Success(items);
    }

    public async Task<Result<DiagnosisDto>> CreateClinicCustomAsync(Guid clinicId, CreateDiagnosisRequest req, CancellationToken ct = default)
    {
        if (await _db.Diagnoses.AnyAsync(d => d.IcdCode == req.IcdCode && d.ClinicId == clinicId, ct))
            return Result<DiagnosisDto>.Failure($"Diagnosis code '{req.IcdCode}' already exists for this clinic");

        return await CreateInternal(clinicId, req, ct);
    }

    // ── ICD-10 Seed ────────────────────────────────────────────────────────────

    public async Task<Result<int>> SeedIcd10CuratedAsync(CancellationToken ct = default)
    {
        var specialities = await _db.Specialities.ToListAsync(ct);
        var existing = await _db.Diagnoses
            .Where(d => d.ClinicId == null)
            .Select(d => d.IcdCode)
            .ToHashSetAsync(ct);

        var toSeed = GetCuratedIcd10Diagnoses(specialities)
            .Where(d => !existing.Contains(d.IcdCode))
            .ToList();

        if (toSeed.Count > 0)
        {
            _db.Diagnoses.AddRange(toSeed);
            await _db.SaveChangesAsync(ct);
        }

        return Result<int>.Success(toSeed.Count);
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private async Task<Result<DiagnosisDto>> CreateInternal(Guid? clinicId, CreateDiagnosisRequest req, CancellationToken ct)
    {
        var speciality = await _db.Specialities.FindAsync([req.SpecialityId], ct);
        if (speciality == null) return Result<DiagnosisDto>.Failure("Speciality not found");

        var entity = new Diagnosis
        {
            ClinicId = clinicId,
            SpecialityId = req.SpecialityId,
            BodyRegionCategoryId = req.BodyRegionCategoryId,
            IcdCode = req.IcdCode.Trim().ToUpper(),
            NameEn = req.NameEn.Trim(),
            NameAr = req.NameAr.Trim(),
            Description = req.Description,
            DefaultProtocolName = req.DefaultProtocolName,
            DefaultExerciseIds = req.DefaultExerciseIds,
            SuggestedSessions = req.SuggestedSessions,
            SuggestedDurationWeeks = req.SuggestedDurationWeeks,
            IsActive = true
        };

        _db.Diagnoses.Add(entity);
        await _db.SaveChangesAsync(ct);

        // Reload with includes
        var loaded = await _db.Diagnoses
            .Include(d => d.Speciality)
            .Include(d => d.BodyRegion)
            .FirstAsync(d => d.Id == entity.Id, ct);

        return Result<DiagnosisDto>.Success(Map(loaded));
    }

    private static DiagnosisDto Map(Diagnosis d) => new(
        d.Id, d.ClinicId, d.SpecialityId,
        d.Speciality?.NameAr ?? "",
        d.BodyRegionCategoryId,
        d.BodyRegion?.NameArabic,
        d.IcdCode, d.NameEn, d.NameAr, d.Description, d.IsActive,
        d.ClinicId == null,
        d.DefaultProtocolName, d.DefaultExerciseIds,
        d.SuggestedSessions, d.SuggestedDurationWeeks);

    // ── Curated ICD-10 data ────────────────────────────────────────────────────

    private static List<Diagnosis> GetCuratedIcd10Diagnoses(List<Rehably.Domain.Entities.Clinical.Speciality> specialities)
    {
        var ortho  = specialities.FirstOrDefault(s => s.Code == "ORTHO");
        var neuro  = specialities.FirstOrDefault(s => s.Code == "NEURO");
        var peds   = specialities.FirstOrDefault(s => s.Code == "PEDS");
        var sports = specialities.FirstOrDefault(s => s.Code == "SPORTS");
        var card   = specialities.FirstOrDefault(s => s.Code == "CARDIO");

        var list = new List<Diagnosis>();

        // ── Orthopaedics (chapter M) ───────────────────────────────────────────
        if (ortho != null)
        {
            var diagnoses = new (string code, string en, string ar, int? sessions, int? weeks)[]
            {
                ("M54.5",  "Low back pain",                          "ألم أسفل الظهر",                    12, 6),
                ("M54.4",  "Lumbago with sciatica",                  "ألم الظهر مع عرق النسا",             16, 8),
                ("M51.1",  "Lumbar disc degeneration",               "تنكس الغضروف القطني",               20, 10),
                ("M51.16", "Intervertebral disc derangement - lumbar","انزلاق غضروفي قطني",                20, 10),
                ("M47.816","Spondylosis with radiculopathy - lumbar", "داء الفقرات مع اعتلال الجذر القطني",18, 9),
                ("M54.2",  "Cervicalgia",                            "ألم عنق الرحم",                      10, 5),
                ("M50.1",  "Cervical disc degeneration",             "تنكس الغضروف العنقي",               14, 7),
                ("M75.1",  "Rotator cuff syndrome",                  "متلازمة الكفة المدوّرة",             18, 9),
                ("M75.0",  "Adhesive capsulitis of shoulder",        "التهاب المحفظة اللاصق (الكتف المتجمد)",16, 8),
                ("M75.5",  "Bursitis of shoulder",                   "التهاب الجراب في الكتف",             10, 5),
                ("M17.11", "Primary osteoarthritis, right knee",     "فُصال الركبة الأيمن الأوّلي",        20, 10),
                ("M17.12", "Primary osteoarthritis, left knee",      "فُصال الركبة الأيسر الأوّلي",        20, 10),
                ("M23.61", "Other spontaneous disruption of ACL",    "تمزق الرباط الصليبي الأمامي",        36, 18),
                ("M23.200","Derangement of meniscus - right knee",   "إصابة الغضروف الهلالي الأيمن",       16, 8),
                ("M25.361","Stiffness of right knee",                "تصلب الركبة اليمنى",                 12, 6),
                ("M79.3",  "Panniculitis",                           "التهاب الدهون تحت الجلد",             8, 4),
                ("M65.3",  "Trigger finger",                         "الإصبع الزناد",                      8, 4),
                ("M72.2",  "Plantar fascial fibromatosis",           "لفافة أخمصية",                      12, 6),
                ("M77.31", "Calcaneal spur, right foot",             "نتوء العقب الأيمن",                  10, 5),
                ("M19.011","Primary osteoarthritis, right shoulder", "فُصال الكتف الأيمن",                 14, 7),
                ("M05.79", "Rheumatoid arthritis - multiple joints", "التهاب المفاصل الروماتويدي",          20, 10),
                ("M80.08", "Age-related osteoporosis - other site",  "هشاشة العظام",                       12, 6),
                ("M84.352","Stress fracture of left femur",          "كسر إجهادي في عظم الفخذ",            24, 12),
            };

            foreach (var (code, en, ar, sessions, weeks) in diagnoses)
                list.Add(new Diagnosis { IcdCode = code, NameEn = en, NameAr = ar, SpecialityId = ortho.Id, IsActive = true, SuggestedSessions = sessions, SuggestedDurationWeeks = weeks });
        }

        // ── Neurology (chapter G) ──────────────────────────────────────────────
        if (neuro != null)
        {
            var diagnoses = new (string code, string en, string ar, int? sessions, int? weeks)[]
            {
                ("G35",   "Multiple sclerosis",                   "التصلب المتعدد",             30, 16),
                ("G20",   "Parkinson's disease",                  "مرض باركنسون",               30, 16),
                ("G81.9", "Hemiplegia, unspecified",              "شلل نصفي",                   40, 20),
                ("G82.50","Quadriplegia, unspecified",            "شلل رباعي",                  40, 20),
                ("G83.10","Monoplegia of lower limb",             "شلل أحادي للطرف السفلي",     24, 12),
                ("G54.2", "Cervical root disorders",              "اضطرابات جذر عصبي عنقي",     16, 8),
                ("G54.4", "Lumbosacral root disorders",           "اضطرابات جذر عصبي قطني عجزي",18, 9),
                ("G57.0", "Lesion of sciatic nerve",              "إصابة العصب الوركي",          14, 7),
                ("G58.9", "Mononeuropathy, unspecified",          "اعتلال عصب أحادي",            12, 6),
                ("G60.0", "Hereditary motor and sensory neuropathy","اعتلال الأعصاب الوراثي",   24, 12),
            };

            foreach (var (code, en, ar, sessions, weeks) in diagnoses)
                list.Add(new Diagnosis { IcdCode = code, NameEn = en, NameAr = ar, SpecialityId = neuro.Id, IsActive = true, SuggestedSessions = sessions, SuggestedDurationWeeks = weeks });
        }

        // ── Sports (chapter S — injuries) ─────────────────────────────────────
        if (sports != null)
        {
            var diagnoses = new (string code, string en, string ar, int? sessions, int? weeks)[]
            {
                ("S83.511","Sprain of ACL, right knee",            "التواء الرباط الصليبي الأمامي الأيمن",  20, 10),
                ("S86.011","Strain of Achilles tendon, right",     "إجهاد وتر أكيليس الأيمن",              16, 8),
                ("S40.011","Contusion of right shoulder",          "كدمة الكتف الأيمن",                     6, 3),
                ("S93.401","Sprain of right ankle",                "التواء الكاحل الأيمن",                  8, 4),
                ("S72.001","Fracture of femoral head, right",      "كسر رأس عظمة الفخذ الأيمن",            30, 15),
                ("S52.501","Colles fracture, right wrist",         "كسر كولز الرسغ الأيمن",                 16, 8),
            };

            foreach (var (code, en, ar, sessions, weeks) in diagnoses)
                list.Add(new Diagnosis { IcdCode = code, NameEn = en, NameAr = ar, SpecialityId = sports.Id, IsActive = true, SuggestedSessions = sessions, SuggestedDurationWeeks = weeks });
        }

        // ── Paediatrics ────────────────────────────────────────────────────────
        if (peds != null)
        {
            var diagnoses = new (string code, string en, string ar, int? sessions, int? weeks)[]
            {
                ("G80.0", "Spastic quadriplegic cerebral palsy",    "الشلل الدماغي التشنجي الرباعي",  40, 20),
                ("G80.1", "Spastic diplegic cerebral palsy",        "الشلل الدماغي التشنجي الثنائي",   36, 18),
                ("Q65.0", "Congenital dislocation of right hip",    "خلع الورك الخلقي الأيمن",         20, 10),
                ("M41.20","Other idiopathic scoliosis, site unspecified","جنف مجهول السبب",            24, 12),
                ("G71.0", "Muscular dystrophy",                     "ضمور عضلي",                      40, 20),
                ("P11.5", "Birth injury to spine and spinal cord",  "إصابة ولادية للعمود الفقري",      30, 15),
            };

            foreach (var (code, en, ar, sessions, weeks) in diagnoses)
                list.Add(new Diagnosis { IcdCode = code, NameEn = en, NameAr = ar, SpecialityId = peds.Id, IsActive = true, SuggestedSessions = sessions, SuggestedDurationWeeks = weeks });
        }

        // ── Cardiopulmonary ────────────────────────────────────────────────────
        if (card != null)
        {
            var diagnoses = new (string code, string en, string ar, int? sessions, int? weeks)[]
            {
                ("I63.9", "Cerebral infarction, unspecified",       "احتشاء دماغي",                    30, 16),
                ("I25.10","Atherosclerotic heart disease",          "مرض القلب التصلبي",                20, 10),
                ("J44.1", "COPD with acute exacerbation",          "مرض الرئة الانسدادي المزمن",       20, 10),
                ("J45.40","Moderate persistent asthma",            "الربو المتوسط المستمر",             12, 6),
                ("I50.9", "Heart failure, unspecified",            "قصور القلب",                       16, 8),
            };

            foreach (var (code, en, ar, sessions, weeks) in diagnoses)
                list.Add(new Diagnosis { IcdCode = code, NameEn = en, NameAr = ar, SpecialityId = card.Id, IsActive = true, SuggestedSessions = sessions, SuggestedDurationWeeks = weeks });
        }

        return list;
    }
}
