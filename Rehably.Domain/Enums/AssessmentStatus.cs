namespace Rehably.Domain.Enums;

public enum AssessmentStatus
{
    Draft     = 0,  // In progress — not yet submitted
    Submitted = 1,  // Doctor submitted / completed
    Archived  = 2   // Old assessment, replaced by newer one
}
