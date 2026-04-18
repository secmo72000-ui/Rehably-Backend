namespace Rehably.Application.Common;

/// <summary>
/// Standard error codes for API responses.
/// These codes are machine-readable identifiers for error types.
/// </summary>
public static class ErrorCodes
{
    // Validation Errors (400)
    public const string ValidationFailed = "VALIDATION_ERROR";
    public const string InvalidInput = "INVALID_INPUT";
    public const string MissingRequiredField = "MISSING_REQUIRED_FIELD";
    public const string InvalidFormat = "INVALID_FORMAT";
    public const string InvalidEmail = "INVALID_EMAIL";
    public const string InvalidPassword = "INVALID_PASSWORD";
    public const string PasswordMismatch = "PASSWORD_MISMATCH";
    public const string DuplicateEntry = "DUPLICATE_ENTRY";

    // Authentication Errors (401)
    public const string Unauthorized = "UNAUTHORIZED";
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string TokenExpired = "TOKEN_EXPIRED";
    public const string TokenInvalid = "TOKEN_INVALID";
    public const string SessionExpired = "SESSION_EXPIRED";
    public const string OtpInvalid = "OTP_INVALID";
    public const string OtpExpired = "OTP_EXPIRED";

    // Authorization Errors (403)
    public const string Forbidden = "FORBIDDEN";
    public const string InsufficientPermissions = "INSUFFICIENT_PERMISSIONS";
    public const string SubscriptionRequired = "SUBSCRIPTION_REQUIRED";
    public const string FeatureNotAvailable = "FEATURE_NOT_AVAILABLE";
    public const string LimitExceeded = "LIMIT_EXCEEDED";

    // Not Found Errors (404)
    public const string NotFound = "NOT_FOUND";
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";
    public const string UserNotFound = "USER_NOT_FOUND";
    public const string ClinicNotFound = "CLINIC_NOT_FOUND";
    public const string SubscriptionNotFound = "SUBSCRIPTION_NOT_FOUND";
    public const string PackageNotFound = "PACKAGE_NOT_FOUND";
    public const string FeatureNotFound = "FEATURE_NOT_FOUND";
    public const string ExerciseNotFound = "EXERCISE_NOT_FOUND";
    public const string TreatmentNotFound = "TREATMENT_NOT_FOUND";
    public const string AssessmentNotFound = "ASSESSMENT_NOT_FOUND";
    public const string DeviceNotFound = "DEVICE_NOT_FOUND";
    public const string ModalityNotFound = "MODALITY_NOT_FOUND";
    public const string InvoiceNotFound = "INVOICE_NOT_FOUND";
    public const string PaymentNotFound = "PAYMENT_NOT_FOUND";

    // Conflict Errors (409)
    public const string Conflict = "CONFLICT";
    public const string DuplicateEmail = "DUPLICATE_EMAIL";
    public const string DuplicateClinic = "DUPLICATE_CLINIC";
    public const string DuplicateSubscription = "DUPLICATE_SUBSCRIPTION";
    public const string ClinicAlreadyExists = "CLINIC_ALREADY_EXISTS";
    public const string SubscriptionAlreadyActive = "SUBSCRIPTION_ALREADY_ACTIVE";
    public const string SubscriptionAlreadyCancelled = "SUBSCRIPTION_ALREADY_CANCELLED";

    // Business Logic Errors (422)
    public const string BusinessRuleViolation = "BUSINESS_RULE_VIOLATION";
    public const string InvalidOperation = "INVALID_OPERATION";
    public const string InvalidStateTransition = "INVALID_STATE_TRANSITION";
    public const string ClinicNotActive = "CLINIC_NOT_ACTIVE";
    public const string ClinicBanned = "CLINIC_BANNED";
    public const string ClinicPendingVerification = "CLINIC_PENDING_VERIFICATION";
    public const string PaymentFailed = "PAYMENT_FAILED";
    public const string RefundNotAllowed = "REFUND_NOT_ALLOWED";

    // Server Errors (500)
    public const string InternalError = "INTERNAL_ERROR";
    public const string DatabaseError = "DATABASE_ERROR";
    public const string ExternalServiceError = "EXTERNAL_SERVICE_ERROR";
    public const string EmailServiceError = "EMAIL_SERVICE_ERROR";
    public const string SmsServiceError = "SMS_SERVICE_ERROR";
    public const string PaymentServiceError = "PAYMENT_SERVICE_ERROR";
    public const string FileUploadError = "FILE_UPLOAD_ERROR";

    // Service Unavailable (503)
    public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
    public const string MaintenanceMode = "MAINTENANCE_MODE";
}
