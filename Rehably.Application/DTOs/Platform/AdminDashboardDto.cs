using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Platform;

public record AdminDashboardDto(
    int TotalClinics,
    int ActiveClinics,
    int SuspendedClinics,
    int TotalUsers,
    int ActiveSubscriptions,
    decimal TotalRevenue,
    List<RecentSubscriptionItem> RecentSubscriptions
);

public record RecentSubscriptionItem(
    Guid Id,
    Guid ClinicId,
    string ClinicName,
    string PackageName,
    SubscriptionStatus Status,
    DateTime StartDate,
    DateTime EndDate,
    DateTime CreatedAt
);
