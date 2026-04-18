using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rehably.API;
using Rehably.Infrastructure.Data;

namespace Rehably.Tests.Helpers;

/// <summary>
/// Shared WebApplicationFactory that replaces real database contexts with InMemory providers.
/// Use this for integration and security tests that need a running app without real databases.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"TestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
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

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            services.AddDbContext<AuditDbContext>(options =>
                options.UseInMemoryDatabase(_dbName + "_audit"));
        });

        builder.UseEnvironment("Development");
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
