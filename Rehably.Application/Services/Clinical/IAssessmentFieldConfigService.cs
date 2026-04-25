using Rehably.Application.Common;
using Rehably.Application.DTOs.Clinical;

namespace Rehably.Application.Services.Clinical;

public interface IAssessmentFieldConfigService
{
    /// <summary>
    /// Returns the field config for a clinic.
    /// Fields not explicitly configured default to IsVisible=true, IsRequired=false.
    /// </summary>
    Task<Result<List<AssessmentFieldConfigDto>>> GetConfigAsync(Guid clinicId, CancellationToken ct = default);

    /// <summary>Batch upsert field visibility/required settings for a clinic.</summary>
    Task<Result<List<AssessmentFieldConfigDto>>> UpsertConfigAsync(
        Guid clinicId,
        List<UpdateFieldConfigItem> fields,
        CancellationToken ct = default);
}
