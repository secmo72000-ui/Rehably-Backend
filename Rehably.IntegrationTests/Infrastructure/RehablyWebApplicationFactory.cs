using Hangfire;
using Hangfire.InMemory;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rehably.Application.Services.Storage;
using Rehably.Infrastructure.Data;
using Microsoft.AspNetCore.RateLimiting;
using Testcontainers.PostgreSql;

namespace Rehably.IntegrationTests.Infrastructure;

public class RehablyWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("rehably_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Remove all DbContext registrations (same pattern as CustomWebApplicationFactory)
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

            // Re-register ApplicationDbContext with Testcontainers PostgreSQL
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(_container.GetConnectionString())
                    .ConfigureWarnings(w => w.Ignore(
                        Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

            // Re-register AuditDbContext with InMemory
            services.AddDbContext<AuditDbContext>(options =>
                options.UseInMemoryDatabase("AuditTestDb"));

            // Replace Hangfire with InMemory storage
            var hangfireDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("Hangfire") == true)
                .ToList();
            foreach (var d in hangfireDescriptors)
                services.Remove(d);

            services.AddHangfire(config => config.UseInMemoryStorage());
            services.AddHangfireServer();

            // Keep rate limiting services as-is (middleware expects them)
            // The default dev config allows 200 requests/second which is fine for tests

            // Replace file upload service with no-op
            var fileUploadDescriptors = services
                .Where(d => d.ServiceType == typeof(IFileUploadService))
                .ToList();
            foreach (var d in fileUploadDescriptors)
                services.Remove(d);

            services.AddScoped<IFileUploadService, NoOpFileUploadService>();

            // Run migrations before seeders execute in Program.cs
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.Migrate();
        });
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _container.StopAsync();
        await _container.DisposeAsync();
        await base.DisposeAsync();
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
}
