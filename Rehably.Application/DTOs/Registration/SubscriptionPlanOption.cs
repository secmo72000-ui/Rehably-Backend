namespace Rehably.Application.DTOs.Registration;

public record SubscriptionPlanOption
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal MonthlyPrice { get; init; }
    public decimal YearlyPrice { get; init; }
    public int TrialDays { get; init; }
    public int? MaxPatients { get; init; }
    public int? MaxUsers { get; init; }
}
