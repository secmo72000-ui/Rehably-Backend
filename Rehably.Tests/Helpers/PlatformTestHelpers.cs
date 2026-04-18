using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Rehably.Application.Contexts;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;
using System.Linq.Expressions;

namespace Rehably.Tests.Helpers;

public static class PlatformTestHelpers
{
    /// <summary>
    /// Creates an InMemory database context for integration testing.
    /// Each call creates a new isolated database instance.
    /// Uses a mock ITenantContext with null TenantId so query filters are pass-through.
    /// </summary>
    public static ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(t => t.TenantId).Returns((Guid?)null);

        var context = new ApplicationDbContext(options, null, mockTenantContext.Object);

        context.Database.EnsureCreated();

        return context;
    }

    /// <summary>
    /// Seeds the context with test data.
    /// </summary>
    public static async Task SeedContextAsync(ApplicationDbContext context, params object[] entities)
    {
        foreach (var entity in entities)
        {
            context.Add(entity);
        }
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds default test data for subscription tests.
    /// </summary>
    public static async Task SeedDefaultDataAsync(ApplicationDbContext context)
    {
        var feature1Id = Guid.NewGuid();
        var feature2Id = Guid.NewGuid();
        var package1Id = Guid.NewGuid();
        var package2Id = Guid.NewGuid();
        var clinic1Id = Guid.NewGuid();
        var clinic2Id = Guid.NewGuid();

        // Seed features
        var features = new List<Feature>
        {
            CreateTestFeature(feature1Id, "users", "User Seats", PricingType.PerUser, categoryId: Guid.NewGuid()),
            CreateTestFeature(feature2Id, "storage", "Storage", PricingType.PerStorageGB, categoryId: Guid.NewGuid())
        };
        context.Features.AddRange(features);

        // Seed packages
        var packages = new List<Package>
        {
            CreateTestPackage(package1Id, "basic", "Basic Package", PackageStatus.Active, monthlyPrice: 100m, yearlyPrice: 1000m, trialDays: 14),
            CreateTestPackage(package2Id, "premium", "Premium Package", PackageStatus.Active, monthlyPrice: 500m, yearlyPrice: 5000m, trialDays: 30)
        };
        context.Packages.AddRange(packages);

        // Seed package features with Feature navigation property
        var packageFeatures = new List<PackageFeature>
        {
            new PackageFeature { Id = Guid.NewGuid(), PackageId = package1Id, FeatureId = feature1Id, IsIncluded = true, Quantity = 5, CalculatedPrice = 50m, Feature = features[0], CreatedAt = DateTime.UtcNow.AddDays(-5) },
            new PackageFeature { Id = Guid.NewGuid(), PackageId = package1Id, FeatureId = feature2Id, IsIncluded = true, Quantity = 50, CalculatedPrice = 25m, Feature = features[1], CreatedAt = DateTime.UtcNow.AddDays(-5) },
            new PackageFeature { Id = Guid.NewGuid(), PackageId = package2Id, FeatureId = feature1Id, IsIncluded = true, Quantity = 20, CalculatedPrice = 200m, Feature = features[0], CreatedAt = DateTime.UtcNow.AddDays(-5) },
            new PackageFeature { Id = Guid.NewGuid(), PackageId = package2Id, FeatureId = feature2Id, IsIncluded = true, Quantity = 200, CalculatedPrice = 100m, Feature = features[1], CreatedAt = DateTime.UtcNow.AddDays(-5) }
        };
        context.PackageFeatures.AddRange(packageFeatures);

        // Seed clinics
        context.Clinics.AddRange(
            new Clinic { Id = clinic1Id, Name = "Test Clinic", IsDeleted = false },
            new Clinic { Id = clinic2Id, Name = "Another Clinic", IsDeleted = false }
        );

        await context.SaveChangesAsync();

        // Seed subscriptions
        var subscriptions = new List<Subscription>
        {
            CreateTestSubscription(Guid.NewGuid(), clinic1Id, package1Id, SubscriptionStatus.Active),
            CreateTestSubscription(Guid.NewGuid(), clinic2Id, package2Id, SubscriptionStatus.Trial),
            CreateTestSubscription(Guid.NewGuid(), clinic1Id, package1Id, SubscriptionStatus.Cancelled)
        };
        context.Subscriptions.AddRange(subscriptions);

        // Seed usage records with Feature navigation property
        var usages = new List<SubscriptionFeatureUsage>
        {
            new SubscriptionFeatureUsage { Id = Guid.NewGuid(), SubscriptionId = subscriptions[0].Id, FeatureId = feature1Id, Used = 3, Limit = 5, Feature = features[0], LastResetAt = DateTime.UtcNow.AddDays(-15), CreatedAt = DateTime.UtcNow.AddDays(-30) },
            new SubscriptionFeatureUsage { Id = Guid.NewGuid(), SubscriptionId = subscriptions[0].Id, FeatureId = feature2Id, Used = 25, Limit = 50, Feature = features[1], LastResetAt = DateTime.UtcNow.AddDays(-15), CreatedAt = DateTime.UtcNow.AddDays(-30) }
        };
        context.SubscriptionFeatureUsages.AddRange(usages);

        await context.SaveChangesAsync();
    }

    public static Feature CreateTestFeature(
        Guid? id = null,
        string code = "test-feature",
        string name = "Test Feature",
        PricingType pricingType = PricingType.Fixed,
        Guid? categoryId = null,
        bool isActive = true,
        bool isDeleted = false)
    {
        return new Feature
        {
            Id = id ?? Guid.NewGuid(),
            Code = code,
            Name = name,
            Description = $"Test description for {name}",
            PricingType = pricingType,
            IsActive = isActive,
            DisplayOrder = 1,
            CategoryId = categoryId ?? Guid.NewGuid(),
            IsDeleted = isDeleted,
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };
    }

    public static FeatureCategory CreateTestCategory(
        Guid? id = null,
        string code = "test-category",
        string name = "Test Category",
        Guid? parentCategoryId = null,
        bool isActive = true,
        bool isDeleted = false)
    {
        return new FeatureCategory
        {
            Id = id ?? Guid.NewGuid(),
            Code = code,
            Name = name,
            Description = $"Test description for {name}",
            DisplayOrder = 1,
            ParentCategoryId = parentCategoryId,
            IsActive = isActive,
            IsDeleted = isDeleted,
            CreatedAt = DateTime.UtcNow.AddDays(-20)
        };
    }

    public static Package CreateTestPackage(
        Guid? id = null,
        string code = "test-package",
        string name = "Test Package",
        PackageStatus status = PackageStatus.Draft,
        decimal monthlyPrice = 500m,
        decimal yearlyPrice = 5000m,
        int trialDays = 14,
        bool isActive = true,
        bool isDeleted = false)
    {
        return new Package
        {
            Id = id ?? Guid.NewGuid(),
            Code = code,
            Name = name,
            Description = $"Test description for {name}",
            MonthlyPrice = monthlyPrice,
            YearlyPrice = yearlyPrice,
            CalculatedMonthlyPrice = monthlyPrice * 0.8m,
            CalculatedYearlyPrice = yearlyPrice * 0.8m,
            Status = status,
            DisplayOrder = 1,
            TrialDays = trialDays,
            IsPublic = true,
            IsCustom = false,
            IsDeleted = isDeleted,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };
    }

    public static PackageFeature CreateTestPackageFeature(
        Guid? id = null,
        Guid? packageId = null,
        Guid? featureId = null,
        bool isIncluded = true,
        int? quantity = null,
        decimal calculatedPrice = 0)
    {
        return new PackageFeature
        {
            Id = id ?? Guid.NewGuid(),
            PackageId = packageId ?? Guid.NewGuid(),
            FeatureId = featureId ?? Guid.NewGuid(),
            IsIncluded = isIncluded,
            Quantity = quantity,
            CalculatedPrice = calculatedPrice,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };
    }

    public static Subscription CreateTestSubscription(
        Guid? id = null,
        Guid? clinicId = null,
        Guid? packageId = null,
        SubscriptionStatus status = SubscriptionStatus.Active,
        DateTime? startDate = null,
        DateTime? endDate = null,
        DateTime? trialEndsAt = null,
        bool autoRenew = true,
        string priceSnapshot = "{}")
    {
        var now = DateTime.UtcNow;
        return new Subscription
        {
            Id = id ?? Guid.NewGuid(),
            ClinicId = clinicId ?? Guid.NewGuid(),
            PackageId = packageId ?? Guid.NewGuid(),
            Status = status,
            BillingCycle = BillingCycle.Monthly,
            StartDate = startDate ?? now.AddDays(-30),
            EndDate = endDate ?? now.AddDays(30),
            TrialEndsAt = trialEndsAt,
            PriceSnapshot = priceSnapshot,
            AutoRenew = autoRenew,
            CreatedAt = now.AddDays(-30)
        };
    }

    public static SubscriptionFeatureUsage CreateTestUsage(
        Guid? id = null,
        Guid? subscriptionId = null,
        Guid? featureId = null,
        int used = 5,
        int limit = 10,
        DateTime? lastResetAt = null)
    {
        return new SubscriptionFeatureUsage
        {
            Id = id ?? Guid.NewGuid(),
            SubscriptionId = subscriptionId ?? Guid.NewGuid(),
            FeatureId = featureId ?? Guid.NewGuid(),
            Used = used,
            Limit = limit,
            LastResetAt = lastResetAt ?? DateTime.UtcNow.AddDays(-15),
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };
    }

    public static Invoice CreateTestInvoice(
        Guid? id = null,
        Guid? clinicId = null,
        Guid? subscriptionId = null,
        decimal amount = 100m,
        decimal taxRate = 14m,
        DateTime? dueDate = null,
        DateTime? paidAt = null)
    {
        return new Invoice
        {
            Id = id ?? Guid.NewGuid(),
            ClinicId = clinicId ?? Guid.NewGuid(),
            SubscriptionId = subscriptionId ?? Guid.NewGuid(),
            InvoiceNumber = $"INV-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
            Amount = amount,
            TaxRate = taxRate,
            TaxAmount = amount * taxRate / 100,
            TotalAmount = amount * (1 + taxRate / 100),
            DueDate = dueDate ?? DateTime.UtcNow.AddDays(30),
            PaidAt = paidAt,
            BillingPeriodStart = DateTime.UtcNow.AddDays(-30),
            BillingPeriodEnd = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };
    }

    public static Payment CreateTestPayment(
        Guid? id = null,
        Guid? clinicId = null,
        Guid? invoiceId = null,
        decimal amount = 100m,
        PaymentStatus status = PaymentStatus.Completed)
    {
        return new Payment
        {
            Id = id ?? Guid.NewGuid(),
            ClinicId = clinicId ?? Guid.NewGuid(),
            InvoiceId = invoiceId ?? Guid.NewGuid(),
            Amount = amount,
            Currency = "EGP",
            Provider = PaymentProvider.PayMob,
            Status = status,
            ProcessedAt = status == PaymentStatus.Completed ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };
    }

    public static Clinic CreateTestClinic(
        Guid? id = null,
        string name = "Test Clinic",
        ClinicStatus status = ClinicStatus.Active,
        bool isDeleted = false)
    {
        return new Clinic
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            Slug = name.ToLower().Replace(" ", "-"),
            Status = status,
            IsDeleted = isDeleted,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };
    }

    public static Rehably.Domain.Entities.Platform.TaxConfiguration CreateTestTaxConfiguration(
        Guid? id = null,
        string name = "Test VAT",
        string? countryCode = null,
        decimal taxRate = 14.00m,
        bool isDefault = false,
        DateTime? effectiveDate = null,
        bool isDeleted = false)
    {
        return new Rehably.Domain.Entities.Platform.TaxConfiguration
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            CountryCode = countryCode,
            TaxRate = taxRate,
            EffectiveDate = effectiveDate ?? DateTime.UtcNow.AddDays(-30),
            ExpiryDate = null,
            IsDefault = isDefault,
            CreatedBy = "test-user",
            IsDeleted = isDeleted,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };
    }

    public static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data) where T : class
    {
        var mockSet = new Mock<DbSet<T>>();
        var queryable = data.AsQueryable();

        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());

        mockSet.Setup(d => d.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .Callback<T, CancellationToken>((entity, _) => data.Add(entity))
            .Returns(() => new ValueTask<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<T>>(
                Mock.Of<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<T>>()));

        mockSet.Setup(d => d.Remove(It.IsAny<T>()))
            .Callback<T>(entity => data.Remove(entity));

        return mockSet;
    }

    public static Mock<DbSet<T>> CreateMockDbSetWithAsyncSupport<T>(List<T> data) where T : class
    {
        var mockSet = CreateMockDbSet(data);

        mockSet.As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new AsyncEnumerator<T>(data.GetEnumerator()));

        mockSet.As<IQueryable<T>>()
            .Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<T>(data.AsQueryable().Provider));

        return mockSet;
    }

    private class AsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _enumerator;

        public AsyncEnumerator(IEnumerator<T> enumerator)
        {
            _enumerator = enumerator;
        }

        public T Current => _enumerator.Current;
        public ValueTask DisposeAsync()
        {
            _enumerator.Dispose();
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return ValueTask.FromResult(_enumerator.MoveNext());
        }
    }

    private class TestAsyncQueryProvider<T> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        public TestAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new TestAsyncQueryable<T>(this, expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestAsyncQueryable<TElement>(new TestAsyncQueryProvider<TElement>(_inner), expression);
        }

        public object Execute(Expression expression)
        {
            return _inner.Execute(expression)!;
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _inner.Execute<TResult>(expression);
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            var resultType = typeof(TResult);
            if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var innerType = resultType.GetGenericArguments()[0];
                var executeMethod = typeof(IQueryProvider).GetMethod(nameof(IQueryProvider.Execute), 1, [typeof(Expression)])!;
                var result = executeMethod.MakeGenericMethod(innerType).Invoke(_inner, [expression]);
                var fromResultMethod = typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(innerType);
                return (TResult)fromResultMethod.Invoke(null, [result])!;
            }

            return Execute<TResult>(expression);
        }
    }

    /// <summary>
    /// Wraps a List&lt;T&gt; as an IQueryable&lt;T&gt; that supports EF Core async operations
    /// (CountAsync, ToListAsync, etc.) for use with mocked repositories.
    /// </summary>
    public static IQueryable<T> ToAsyncQueryable<T>(this List<T> source)
    {
        var queryable = source.AsQueryable();
        return new TestAsyncQueryable<T>(new TestAsyncQueryProvider<T>(queryable.Provider), queryable.Expression);
    }

    private class TestAsyncQueryable<T> : IOrderedQueryable<T>, IAsyncEnumerable<T>
    {
        private readonly TestAsyncQueryProvider<T> _provider;
        private readonly Expression _expression;

        public TestAsyncQueryable(TestAsyncQueryProvider<T> provider, Expression expression)
        {
            _provider = provider;
            _expression = expression;
        }

        public Type ElementType => typeof(T);
        public Expression Expression => _expression;
        public IQueryProvider Provider => _provider;

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new AsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _provider.Execute<IEnumerable<T>>(_expression).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
