using Microsoft.EntityFrameworkCore;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Clinical;
using Rehably.Application.Services.Clinical;
using Rehably.Domain.Entities.Clinical;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Services.Clinical;

public class AssessmentFieldConfigService : IAssessmentFieldConfigService
{
    private readonly ApplicationDbContext _db;

    // Master list of all configurable fields — matches the assessment wizard
    private static readonly List<(int Step, string Key, string Label)> AllFields =
    [
        // Step 1 — Patient Info (not configurable, always shown)
        // Step 2 — Post-Op
        (2, "postop.procedure_name",     "اسم الإجراء"),
        (2, "postop.procedure_side",     "الجانب"),
        (2, "postop.surgery_date",       "تاريخ العملية"),
        (2, "postop.days_post_op",       "أيام ما بعد الجراحة"),
        (2, "postop.surgeon_facility",   "الجراح / المستشفى"),
        (2, "postop.wb_status",          "حالة التحميل"),
        (2, "postop.rom_restriction",    "قيود مدى الحركة"),
        (2, "postop.precautions",        "الاحتياطات"),
        (2, "postop.wound_status",       "حالة الجرح"),
        (2, "postop.notes",              "ملاحظات"),
        // Step 3 — Red Flags
        (3, "redflags.flags",            "العلامات التحذيرية"),
        (3, "redflags.decision",         "القرار"),
        (3, "redflags.actions",          "الإجراءات المتخذة"),
        // Step 4 — Subjective
        (4, "subj.chief_complaint",      "الشكوى الرئيسية"),
        (4, "subj.onset_mechanism",      "آلية الإصابة"),
        (4, "subj.pain_sliders",         "مستوى الألم (الآن / أفضل / أسوأ)"),
        (4, "subj.night_pain",           "ألم الليل"),
        (4, "subj.morning_stiffness",    "تيبس الصباح"),
        (4, "subj.pain_pattern",         "نمط الألم على مدار اليوم"),
        (4, "subj.aggravating_factors",  "العوامل المحفزة"),
        (4, "subj.easing_factors",       "العوامل المخففة"),
        (4, "subj.functional_limits",    "القيود الوظيفية"),
        (4, "subj.previous_injuries",    "الإصابات السابقة"),
        (4, "subj.medical_history",      "التاريخ الطبي"),
        (4, "subj.medications",          "الأدوية"),
        (4, "subj.screening_flags",      "علامات الفحص"),
        (4, "subj.patient_goals",        "أهداف المريض"),
        // Step 5 — Objective
        (5, "obj.posture",               "الوضعية"),
        (5, "obj.swelling",              "التورم"),
        (5, "obj.redness",               "الاحمرار"),
        (5, "obj.deformity",             "التشوه"),
        (5, "obj.gait",                  "المشية"),
        (5, "obj.transfers",             "التنقل"),
        (5, "obj.assistive_devices",     "الأجهزة المساعدة"),
        (5, "obj.functional_tests",      "الاختبارات الوظيفية"),
        (5, "obj.rom_table",             "جدول مدى الحركة"),
        (5, "obj.strength_table",        "جدول القوة العضلية (MMT)"),
        // Step 6 — Neuro
        (6, "neuro.sensation",           "الإحساس"),
        (6, "neuro.numbness",            "التخدر"),
        (6, "neuro.tingling",            "التنميل"),
        (6, "neuro.myotomes",            "ميوتومات"),
        (6, "neuro.reflexes",            "ردود الفعل"),
        (6, "neuro.neurovascular",       "فحوصات الأوعية الدموية العصبية"),
        (6, "neuro.special_tests",       "الاختبارات الخاصة"),
        // Step 7 — Clinical Reasoning
        (7, "cr.problem_list",           "قائمة المشاكل"),
        (7, "cr.working_hypothesis",     "الفرضية العملية"),
        (7, "cr.severity",              "الشدة / مستوى التهيج"),
        (7, "cr.differentials",         "التشخيصات التفريقية"),
        (7, "cr.decision_points",       "نقاط القرار"),
        (7, "cr.imaging",               "التصوير الطبي"),
        (7, "cr.referral",              "الإحالة"),
    ];

    public AssessmentFieldConfigService(ApplicationDbContext db) => _db = db;

    public async Task<Result<List<AssessmentFieldConfigDto>>> GetConfigAsync(
        Guid clinicId, CancellationToken ct = default)
    {
        try
        {
            var saved = await _db.ClinicAssessmentFieldConfigs
                .Where(c => c.ClinicId == clinicId)
                .ToListAsync(ct);

            var result = AllFields.Select(f =>
            {
                var s = saved.FirstOrDefault(x => x.StepNumber == f.Step && x.FieldKey == f.Key);
                return new AssessmentFieldConfigDto(
                    f.Step, f.Key, f.Label,
                    IsVisible: s?.IsVisible ?? true,
                    IsRequired: s?.IsRequired ?? false
                );
            }).ToList();

            return Result<List<AssessmentFieldConfigDto>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<List<AssessmentFieldConfigDto>>.Failure($"Failed to load field config: {ex.Message}");
        }
    }

    public async Task<Result<List<AssessmentFieldConfigDto>>> UpsertConfigAsync(
        Guid clinicId,
        List<UpdateFieldConfigItem> fields,
        CancellationToken ct = default)
    {
        try
        {
            var existing = await _db.ClinicAssessmentFieldConfigs
                .Where(c => c.ClinicId == clinicId)
                .ToListAsync(ct);

            foreach (var f in fields)
            {
                var entity = existing.FirstOrDefault(
                    x => x.StepNumber == f.StepNumber && x.FieldKey == f.FieldKey);

                if (entity is null)
                {
                    entity = new ClinicAssessmentFieldConfig
                    {
                        ClinicId   = clinicId,
                        StepNumber = f.StepNumber,
                        FieldKey   = f.FieldKey,
                        IsVisible  = f.IsVisible,
                        IsRequired = f.IsRequired,
                    };
                    _db.ClinicAssessmentFieldConfigs.Add(entity);
                }
                else
                {
                    entity.IsVisible  = f.IsVisible;
                    entity.IsRequired = f.IsRequired;
                }
            }

            await _db.SaveChangesAsync(ct);
            return await GetConfigAsync(clinicId, ct);
        }
        catch (Exception ex)
        {
            return Result<List<AssessmentFieldConfigDto>>.Failure($"Failed to save field config: {ex.Message}");
        }
    }
}
