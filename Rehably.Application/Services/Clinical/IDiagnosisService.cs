using Rehably.Application.Common;
using Rehably.Application.DTOs.Clinical;

namespace Rehably.Application.Services.Clinical;

public interface IDiagnosisService
{
    // ── Admin: global diagnosis management ────────────────────────────────────
    Task<Result<PagedResult<DiagnosisListItem>>> GetAllAsync(DiagnosisQueryParams query, CancellationToken ct = default);
    Task<Result<DiagnosisDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<DiagnosisDto>> CreateGlobalAsync(CreateDiagnosisRequest request, CancellationToken ct = default);
    Task<Result<DiagnosisDto>> UpdateAsync(Guid id, UpdateDiagnosisRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);

    // ── Clinic: get filtered by clinic's specialities + own custom ─────────────
    Task<Result<List<DiagnosisListItem>>> GetForClinicAsync(Guid clinicId, Guid? specialityId, Guid? bodyRegionId, string? search, CancellationToken ct = default);
    Task<Result<DiagnosisDto>> CreateClinicCustomAsync(Guid clinicId, CreateDiagnosisRequest request, CancellationToken ct = default);

    // ── Seeder ────────────────────────────────────────────────────────────────
    Task<Result<int>> SeedIcd10CuratedAsync(CancellationToken ct = default);
}
