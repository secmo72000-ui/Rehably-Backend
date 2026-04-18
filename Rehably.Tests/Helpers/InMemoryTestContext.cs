using Microsoft.EntityFrameworkCore;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Entities.Tenant;
using Rehably.Infrastructure.Data;

namespace Rehably.Tests.Helpers;

public class InMemoryTestContext
{
    public DbSet<Feature> Features { get; set; }
    public DbSet<FeatureCategory> FeatureCategories { get; set; }
    public DbSet<Package> Packages { get; set; }
    public DbSet<PackageFeature> PackageFeatures { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<SubscriptionFeatureUsage> SubscriptionFeatureUsages { get; set; }
    public DbSet<Clinic> Clinics { get; set; }
    public DbSet<Rehably.Domain.Entities.Platform.TaxConfiguration> TaxConfigurations { get; set; }
}
