namespace Rehably.Domain.Enums;

/// <summary>
/// Defines when an assessment should be administered during treatment.
/// </summary>
public enum AssessmentTimePoint
{
    /// <summary>Initial assessment at start of treatment (خط الاساس)</summary>
    Baseline = 1,

    /// <summary>Regular assessment every two weeks (كل اسبوعين)</summary>
    Biweekly = 2,

    /// <summary>Final assessment at discharge (تفريغ)</summary>
    Discharge = 3
}
