namespace Rehably.Application.DTOs.Platform;

public record PaymentInfoDto
{
    public decimal Amount { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? TransactionId { get; init; }
}
