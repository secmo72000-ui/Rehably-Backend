using Rehably.Domain.Entities.Base;

namespace Rehably.Domain.Entities.Clinical;

/// <summary>
/// Per-clinic configuration of which assessment fields are visible/required.
/// Each row = one field in the assessment wizard.
/// Clinic manager can toggle visibility using the 👁 icon in Figma.
/// </summary>
public class ClinicAssessmentFieldConfig : BaseEntity
{
    public Guid ClinicId { get; set; }

    /// <summary>Assessment step number (1–7).</summary>
    public int StepNumber { get; set; }

    /// <summary>
    /// Unique field key matching the Figma label, e.g.
    /// "postop.procedure_name" | "objective.rom_table" | "neuro.special_tests"
    /// </summary>
    public string FieldKey { get; set; } = string.Empty;

    public bool IsVisible { get; set; } = true;
    public bool IsRequired { get; set; } = false;
}
