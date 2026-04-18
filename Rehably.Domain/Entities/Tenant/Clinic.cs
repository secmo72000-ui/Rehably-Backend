using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Enums;

namespace Rehably.Domain.Entities.Tenant;

public class Clinic
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameArabic { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public ClinicStatus Status { get; set; } = ClinicStatus.PendingEmailVerification;
    public Guid? OnboardingId { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public string? BanReason { get; set; }
    public DateTime? BannedAt { get; set; }
    public string? BannedBy { get; set; }
    public DateTime? SuspendedAt { get; set; }
    public DateTime? DataDeletionDate { get; set; }
    public DeletionStage DeletionStage { get; set; } = DeletionStage.NotStarted;
    public string? DeletionJobId { get; set; }
    public string? OriginalSlug { get; set; }
    public Guid? CurrentSubscriptionId { get; set; }
    public long StorageUsedBytes { get; set; }
    public long StorageLimitBytes { get; set; }
    public int PatientsCount { get; set; }
    public int? PatientsLimit { get; set; }
    public int UsersCount { get; set; }
    public int? UsersLimit { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Subscription? CurrentSubscription { get; set; }
    public ClinicOnboarding? Onboarding { get; set; }
    public ICollection<UsageHistory> UsageHistories { get; set; } = new List<UsageHistory>();
    public ICollection<ClinicDocument> Documents { get; set; } = new List<ClinicDocument>();
    public ICollection<UsageRecord> UsageRecords { get; set; } = new List<UsageRecord>();

    #region Domain Methods

    /// <summary>
    /// Checks if the clinic is currently banned.
    /// </summary>
    public bool IsBanned => Status == ClinicStatus.Banned;
    public bool IsSuspended => Status == ClinicStatus.Suspended;

    /// <summary>
    /// Checks if the clinic can be banned.
    /// </summary>
    public bool CanBeBanned() => Status != ClinicStatus.PendingEmailVerification && Status != ClinicStatus.Banned;

    /// <summary>
    /// Bans the clinic with a reason.
    /// </summary>
    public void Ban(string reason, string bannedBy)
    {
        if (Status == ClinicStatus.PendingEmailVerification)
        {
            throw new InvalidOperationException("Cannot ban a clinic that is pending email verification.");
        }
        if (Status == ClinicStatus.Banned)
        {
            throw new InvalidOperationException("Clinic is already banned.");
        }

        Status = ClinicStatus.Banned;
        BanReason = reason;
        BannedAt = DateTime.UtcNow;
        BannedBy = bannedBy;
    }

    /// <summary>
    /// Checks if the clinic can be activated.
    /// </summary>
    public bool CanBeActivated() => Status != ClinicStatus.Active && Status != ClinicStatus.Banned;

    /// <summary>
    /// Activates the clinic.
    /// </summary>
    public void Activate()
    {
        Status = ClinicStatus.Active;
        ActivatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Suspend()
    {
        if (Status == ClinicStatus.Suspended)
            throw new InvalidOperationException("Clinic is already suspended.");
        if (Status == ClinicStatus.Banned)
            throw new InvalidOperationException("Cannot suspend a banned clinic.");

        Status = ClinicStatus.Suspended;
        SuspendedAt = DateTime.UtcNow;
        DataDeletionDate = DateTime.UtcNow.AddDays(30);
    }

    public void Reactivate()
    {
        if (Status != ClinicStatus.Suspended)
            throw new InvalidOperationException($"Cannot reactivate a clinic with status {Status}.");

        Status = ClinicStatus.Active;
        SuspendedAt = null;
        DataDeletionDate = null;
        DeletionStage = DeletionStage.NotStarted;
        DeletionJobId = null;
    }

    public bool CanBeDeleted() => Status == ClinicStatus.Suspended && DataDeletionDate != null && DataDeletionDate <= DateTime.UtcNow;

    #endregion
}
