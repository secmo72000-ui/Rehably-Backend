using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rehably.Infrastructure.Data;

namespace Rehably.Tests.Helpers;

/// <summary>
/// Base class for integration tests that need a running WebApplicationFactory
/// with InMemory databases and configurable test authentication.
/// </summary>
/// <typeparam name="TAuthHandler">The authentication handler type to use for the test scheme.</typeparam>
public abstract class IntegrationTestBase<TAuthHandler> : IDisposable
    where TAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IServiceScope _scope;
    private readonly string _dbName = $"IntegrationTestDb_{Guid.NewGuid()}";

    /// <summary>
    /// Pre-configured HttpClient that sends requests through the test server.
    /// </summary>
    protected HttpClient Client { get; }

    /// <summary>
    /// The main application database context (PostgreSQL InMemory replacement).
    /// </summary>
    protected ApplicationDbContext DbContext { get; }

    /// <summary>
    /// The audit database context (MySQL InMemory replacement).
    /// </summary>
    protected AuditDbContext AuditDbContext { get; }

    protected IntegrationTestBase()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    RemoveDbContextServices(services);

                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseInMemoryDatabase(_dbName));

                    services.AddDbContext<AuditDbContext>(options =>
                        options.UseInMemoryDatabase(_dbName + "_audit"));

                    DisableRateLimiting(services);
                });

                builder.ConfigureTestServices(services =>
                {
                    services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, TAuthHandler>("Test", _ => { });

                    services.PostConfigure<AuthenticationOptions>(options =>
                    {
                        options.DefaultAuthenticateScheme = "Test";
                        options.DefaultChallengeScheme = "Test";
                    });
                });

                builder.UseEnvironment("Development");
            });

        Client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        DbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        AuditDbContext = _scope.ServiceProvider.GetRequiredService<AuditDbContext>();
    }

    /// <summary>
    /// Seeds entities into the application database context and saves changes.
    /// </summary>
    protected async Task SeedAsync(params object[] entities)
    {
        foreach (var entity in entities)
            DbContext.Add(entity);

        await DbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Creates a new service scope for resolving scoped services from the test server's DI container.
    /// Caller is responsible for disposing the returned scope.
    /// </summary>
    protected IServiceScope CreateScope()
    {
        return _factory.Services.CreateScope();
    }

    public void Dispose()
    {
        DbContext.Database.EnsureDeleted();
        AuditDbContext.Database.EnsureDeleted();
        _scope.Dispose();
        Client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private static void RemoveDbContextServices(IServiceCollection services)
    {
        var descriptorsToRemove = services
            .Where(d => IsDbContextRelated(d.ServiceType, typeof(ApplicationDbContext)) ||
                        IsDbContextRelated(d.ServiceType, typeof(AuditDbContext)))
            .ToList();

        foreach (var d in descriptorsToRemove)
            services.Remove(d);

        var nonGenericOptions = services
            .Where(d => d.ServiceType == typeof(DbContextOptions))
            .ToList();

        foreach (var d in nonGenericOptions)
            services.Remove(d);
    }

    private static bool IsDbContextRelated(Type serviceType, Type contextType)
    {
        if (serviceType == contextType)
            return true;

        if (serviceType.IsGenericType &&
            serviceType.GenericTypeArguments.Any(t => t == contextType))
            return true;

        return false;
    }

    private static void DisableRateLimiting(IServiceCollection services)
    {
        var rateLimiterDescriptors = services
            .Where(d => d.ServiceType.FullName?.Contains("RateLimiting") == true ||
                        d.ServiceType.FullName?.Contains("RateLimiter") == true)
            .ToList();

        foreach (var d in rateLimiterDescriptors)
            services.Remove(d);
    }
}

/// <summary>
/// Integration test base that authenticates as a PlatformAdmin with wildcard permissions.
/// </summary>
public abstract class AdminIntegrationTestBase : IntegrationTestBase<TestAdminAuthHandler>;

/// <summary>
/// Integration test base that authenticates as a tenant user.
/// Configure <see cref="TestTenantAuthHandler"/> static properties before test execution.
/// </summary>
public abstract class TenantIntegrationTestBase : IntegrationTestBase<TestTenantAuthHandler>;
