namespace Rehably.Domain.Enums;

public enum OnboardingStep
{
    PendingEmailVerification = 0,
    PendingDocumentUpload = 1,
    PendingDocumentsAndPackage = 1,
    PendingApproval = 2,
    PendingPayment = 3,
    Completed = 4,
    PendingCustomPackageReview = 5
}
