namespace Rehably.Application.DTOs.Payment;

public record PaymentInitiationResult(string? PaymentUrl, string? TransactionId);

public record PaymentVerificationResult(string? TransactionId);

public record CashPaymentResult(string TransactionId);
