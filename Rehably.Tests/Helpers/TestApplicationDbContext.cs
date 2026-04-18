using Microsoft.EntityFrameworkCore;
using Rehably.Domain.Entities.Identity;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Entities.Tenant;
using Rehably.Infrastructure.Data;

namespace Rehably.Tests.Helpers;

public class TestApplicationDbContext : ApplicationDbContext
{
    public TestApplicationDbContext()
        : base(new DbContextOptions<ApplicationDbContext>())
    {
    }

    public TestApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // New virtual DbSet properties that hide base properties
    // These can be mocked by Moq
    public virtual new DbSet<Feature> Features { get; set; } = null!;
    public virtual new DbSet<FeatureCategory> FeatureCategories { get; set; } = null!;
    public virtual new DbSet<Package> Packages { get; set; } = null!;
    public virtual new DbSet<PackageFeature> PackageFeatures { get; set; } = null!;
    public virtual new DbSet<Subscription> Subscriptions { get; set; } = null!;
    public virtual new DbSet<SubscriptionFeatureUsage> SubscriptionFeatureUsages { get; set; } = null!;
    public virtual new DbSet<Clinic> Clinics { get; set; } = null!;
}
