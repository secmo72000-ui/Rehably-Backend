namespace Rehably.Application.DTOs.Registration;

public record InitiatePaymentResponse
{
    public string TransactionId { get; init; } = string.Empty;
    public string? PaymentUrl { get; init; }
    public string? Message { get; init; }
}
