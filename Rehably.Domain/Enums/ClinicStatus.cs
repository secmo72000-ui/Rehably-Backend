namespace Rehably.Domain.Enums;

public enum ClinicStatus
{
    PendingEmailVerification = 0,
    PendingDocumentUpload = 1,
    PendingDocumentsAndPackage = 1,
    PendingApproval = 2,
    PendingPayment = 3,
    Active = 4,
    Suspended = 5,
    Cancelled = 6,
    Banned = 7,
    PendingCustomPackageReview = 8
}
