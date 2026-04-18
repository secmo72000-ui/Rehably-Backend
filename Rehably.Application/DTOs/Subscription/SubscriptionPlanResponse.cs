using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Subscription;

public record SubscriptionPlanResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? NameArabic { get; init; }
    public decimal Price { get; init; }
    public string Currency { get; init; } = "USD";
    public BillingCycle BillingCycle { get; init; }
    public bool IsActive { get; init; }
    public bool IsTrial { get; init; }
    public int TrialDays { get; init; }

    public long StorageLimitBytes { get; init; }
    public int PatientsLimit { get; init; }
    public int UsersLimit { get; init; }
    public string StorageLimit => FormatBytes(StorageLimitBytes);
    public string PatientsLimitText => PatientsLimit == int.MaxValue ? "Unlimited" : PatientsLimit.ToString();
    public string UsersLimitText => UsersLimit == int.MaxValue ? "Unlimited" : UsersLimit.ToString();
    public Dictionary<string, bool>? Features { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
