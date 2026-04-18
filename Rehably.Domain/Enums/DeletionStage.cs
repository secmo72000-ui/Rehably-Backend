namespace Rehably.Domain.Enums;

public enum DeletionStage
{
    NotStarted = 0,
    AuditLogged = 1,
    SlugReleased = 2,
    UsageDataDeleted = 3,
    SubscriptionDataDeleted = 4,
    BillingDataDeleted = 5,
    DocumentsDeleted = 6,
    UsersDeleted = 7,
    ClinicRowDeleted = 8,
    Completed = 9
}
