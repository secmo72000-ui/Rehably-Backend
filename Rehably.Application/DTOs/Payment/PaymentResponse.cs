namespace Rehably.Application.DTOs.Payment;

public record PaymentResponse
{
    public string TransactionId { get; init; } = string.Empty;
    public string? PaymentUrl { get; init; }
    public string? Message { get; init; }
}
