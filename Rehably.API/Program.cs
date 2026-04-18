using FluentValidation;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using QuestPDF.Infrastructure;
using Rehably.API.Extensions;
using Rehably.API.Middleware;
using Rehably.Domain.Entities.Identity;
using Rehably.Infrastructure.BackgroundJobs;
using Rehably.Infrastructure.Data;
using Rehably.Infrastructure.Jobs;
using Rehably.Infrastructure.Seed;
using ApplicationRole = Rehably.Domain.Entities.Identity.ApplicationRole;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Ensure environment variables override appsettings.json values in production.
// Set ALL secrets as environment variables — never hardcode in appsettings.json.
// Variables use "__" as section separator, e.g.:
//   JwtSettings__Secret=<your-secret>
//   ConnectionStrings__DefaultConnection=<your-connection-string>
//   CloudinarySettings__ApiSecret=<your-api-secret>
builder.Configuration.AddEnvironmentVariables();

builder.Configuration.ConfigureSerilog();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAuthentication(builder.Configuration);
builder.Services.AddPaymentServices(builder.Configuration);
builder.Services.Configure<Rehably.Infrastructure.Settings.AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.AddRepositories();
builder.Services.AddCloudinary(builder.Configuration);
builder.Services.AddCustomSwagger();
builder.Services.AddCustomCors(builder.Configuration, builder.Environment);
builder.Services.AddHangfire(builder.Configuration);
builder.Services.AddCommunicationServices(builder.Configuration, builder.Environment);
builder.Services.AddLibraryServices();

// In Development, use relaxed limits so tests can run repeatedly without
// exhausting IP-based buckets. Production values enforce strict security.
var isDev = builder.Environment.IsDevelopment();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("registration", opt =>
    {
        opt.PermitLimit = isDev ? 200 : 5;
        opt.Window = TimeSpan.FromHours(1);
        opt.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("otp-verification", opt =>
    {
        opt.PermitLimit = isDev ? 200 : 10;
        opt.Window = TimeSpan.FromHours(1);
        opt.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("auth-login", opt =>
    {
        opt.PermitLimit = isDev ? 200 : 5;
        opt.Window = TimeSpan.FromMinutes(15);
        opt.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("auth-password", opt =>
    {
        opt.PermitLimit = isDev ? 200 : 3;
        opt.Window = TimeSpan.FromHours(1);
        opt.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("auth-forgot", opt =>
    {
        opt.PermitLimit = isDev ? 200 : 3;
        opt.Window = TimeSpan.FromHours(1);
        opt.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("auth-otp", opt =>
    {
        opt.PermitLimit = isDev ? 200 : 5;
        opt.Window = TimeSpan.FromMinutes(15);
        opt.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("auth-refresh", opt =>
    {
        opt.PermitLimit = isDev ? 200 : 10;
        opt.Window = TimeSpan.FromMinutes(15);
        opt.QueueLimit = 0;
    });
});

builder.Services.AddControllers(options =>
{
    options.Filters.Add<Rehably.API.Filters.ValidationActionFilter>();
    options.Filters.Add<Rehably.API.Extensions.NullableFormFieldFilter>();
    options.ModelBinderProviders.Insert(0, new Rehably.API.Extensions.NullableDateTimeModelBinderProvider());
});
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddValidatorsFromAssemblyContaining<Rehably.Application.Validators.Platform.CreatePackageRequestDtoValidator>();
builder.Services.AddSerilogServices();

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

//if (app.Environment.IsDevelopment())
//{
    app.UseCustomSwagger();
//}

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Applying pending migrations...");
        await context.Database.MigrateAsync();
        logger.LogInformation("Migrations applied successfully");

        logger.LogInformation("Starting database seeding...");

        await RoleSeeder.SeedAsync(context, roleManager);
        logger.LogInformation("Roles seeded successfully");

        await PermissionSeeder.SeedAsync(roleManager, logger);
        logger.LogInformation("Permissions seeded successfully");

        await AuthDataSeeder.SeedAsync(context, userManager, roleManager, app.Environment);
        logger.LogInformation("Auth data seeded successfully");

        await SubscriptionPackageSeeder.SeedSubscriptionPackageSystemAsync(context);
        logger.LogInformation("Subscription package system seeded successfully");

        logger.LogInformation("Database seeding completed");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.UseErrorHandling();
app.UseHttpsRedirection();

// Environment-aware CORS policy
if (app.Environment.IsDevelopment())
{
    app.UseCors("Development");
}
else
{
    app.UseCors("Production");
}
app.UseRateLimiter();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseAuthentication();
app.UseMiddleware<MustChangePasswordMiddleware>();
app.UseTenantMiddleware();
app.UseSubscriptionLimit();
app.UseAuthorization();
app.UseFeatureRequirement();
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new Rehably.API.Authorization.HangfireAuthorizationFilter()]
});

app.MapControllers();

// Health check for Render + load balancers
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
   .AllowAnonymous()
   .WithTags("Health");

RecurringJob.AddOrUpdate<AuditLogCleanupJob>("audit-log-cleanup", job => job.ExecuteAsync(), Cron.Yearly);
RecurringJob.AddOrUpdate<RefreshTokenCleanupJob>("refresh-token-cleanup", job => job.ExecuteAsync(), Cron.Daily(2));

using (var scope = app.Services.CreateScope())
{
    var recurringJobs = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    recurringJobs.ScheduleClinicOnboardingJobs();
    recurringJobs.ScheduleAddOnExpiryJob();
    recurringJobs.ScheduleSubscriptionReminderJob();
    recurringJobs.ScheduleSubscriptionExpiryCheckJob();
    recurringJobs.ScheduleTrialReminderJob();
    recurringJobs.ScheduleRegistrationCleanupJob();
    recurringJobs.ScheduleSubscriptionUsageJobs();
}

app.Run();
