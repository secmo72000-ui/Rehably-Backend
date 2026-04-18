namespace Rehably.Domain.Enums;

public enum AuditActionType
{
    Login,
    Logout,
    LoginFailed,
    OtpRequested,
    OtpVerified,
    OtpFailed,
    PasswordChanged,
    PasswordResetRequested,
    PasswordResetCompleted,
    Create,
    Update,
    Delete,
    Export,
    Import,
    View,
    Invite,
    Activate,
    Deactivate,
    Suspend,
    Subscribe,
    Unsubscribe,
    PaymentProcessed,
    PaymentFailed,
    RefundProcessed
}
