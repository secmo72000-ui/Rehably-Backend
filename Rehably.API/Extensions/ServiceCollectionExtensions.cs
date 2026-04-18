using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Rehably.Application.Contexts;
using Rehably.Application.Repositories;
using Rehably.Application.Mapping;
using Rehably.Application.Services;
using Rehably.Application.Services.Admin;
using Rehably.Application.Services.Auth;
using Rehably.Application.Services.Clinic;
using Rehably.Application.Services.Communication;
using Rehably.Application.Services.Billing;
using Rehably.Application.Services.Payment;
using Rehably.Application.Services.Platform;
using Rehably.Application.Services.Storage;
using Rehably.Infrastructure.Providers.Payment;
using Rehably.Domain.Entities.Identity;
using Rehably.Infrastructure.Contexts;
using Rehably.Infrastructure.Repositories;
using Rehably.Infrastructure.Data;
using Rehably.Infrastructure.Interceptors;
using Rehably.Infrastructure.Seed;
using Rehably.Infrastructure.Services;
using Rehably.Infrastructure.Services.Billing;
using Rehably.Infrastructure.Services.Payment;
using Rehably.Infrastructure.Services.Storage;
using Rehably.Application.Interfaces;
using Rehably.Infrastructure.Settings;
using Rehably.Infrastructure.Services.Auth;
using Rehably.Infrastructure.Services.Communication;
using Rehably.Infrastructure.Services.Communication.Channels;
using Rehably.Infrastructure.Services.Communication.Email;
using Rehably.Infrastructure.Services.Communication.Sms;
using Rehably.Infrastructure.Services.Communication.WhatsApp;
using Rehably.Infrastructure.Services.Platform;
using Rehably.Infrastructure.Services.Clinic;
using Rehably.Infrastructure.Jobs;
using Rehably.Infrastructure.Services.Admin;
using Rehably.Application.Services.ClinicPortal;
using Rehably.Infrastructure.Services.ClinicPortal;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Rehably.API.Authorization;
using Rehably.Application.Services.Library;
using Rehably.Application.Services.Clinic;
using Rehably.Infrastructure.Services.Library;
using Rehably.Application.Validators.Clinic;
using Rehably.Infrastructure.Jobs;

namespace Rehably.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var mainConnectionString = config.GetConnectionString("DefaultConnection");
        var auditConnectionString = config.GetConnectionString("AuditConnection");

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var auditInterceptor = sp.GetService<AuditInterceptor>();
            var tenantContext = sp.GetService<ITenantContext>();
            options.UseNpgsql(mainConnectionString);
            options.ConfigureWarnings(w =>
                w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            if (auditInterceptor != null)
            {
                options.AddInterceptors(auditInterceptor);
            }
        }, ServiceLifetime.Scoped);

        bool IsDesignTime()
        {
            var args = Environment.GetCommandLineArgs();
            return args.Any(a => a.Contains("ef", StringComparison.OrdinalIgnoreCase)) ||
                   args.Any(a => a.Contains("migrations", StringComparison.OrdinalIgnoreCase));
        }

        if (!IsDesignTime() && !string.IsNullOrEmpty(auditConnectionString))
        {
            services.AddDbContext<AuditDbContext>(options =>
            {
                options.UseNpgsql(auditConnectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);
                });
            });
        }

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
            options.Password.RequiredUniqueChars = 1;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        services.AddScoped<AuditInterceptor>(sp =>
        {
            var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
            var httpContext = httpContextAccessor.HttpContext;
            var userId = httpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var tenantId = httpContext?.User?.FindFirst("TenantId")?.Value;
            var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString();
            var userAgent = httpContext?.Request?.Headers["User-Agent"].ToString();
            var serviceScopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            var logger = sp.GetRequiredService<ILogger<AuditInterceptor>>();
            return new AuditInterceptor(userId, tenantId, ipAddress, userAgent, serviceScopeFactory, logger);
        });

        services.AddMemoryCache();
        services.AddDistributedMemoryCache(); // Required for IDistributedCache
        services.AddHttpContextAccessor();

        services.AddSingleton<IClock, SystemClock>();

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<IClinicUsageTrackingService, ClinicUsageTrackingService>();

        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IClinicCrudService, ClinicCrudService>();
        services.AddScoped<IClinicRegistrationService, ClinicRegistrationService>();
        services.AddScoped<IClinicQueryService, ClinicQueryService>();
        services.AddScoped<IClinicBillingService, ClinicBillingService>();
        services.AddScoped<IClinicService, ClinicService>();
        services.AddScoped<IUsageTrackingService, UsageTrackingService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuthOtpService, AuthOtpService>();
        services.AddScoped<IAuthPasswordService, AuthPasswordService>();
        services.AddScoped<IPermissionLookupService, PermissionLookupService>();
        services.AddScoped<ITenantResolutionService, TenantResolutionService>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionHandler>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IPlanPermissionService, PlanPermissionService>();
        services.AddScoped<IRoleManagementService, RoleManagementService>();
        services.AddScoped<IPlatformRoleService, PlatformRoleService>();
        services.AddScoped<IPlatformRoleManagementService, PlatformRoleManagementService>();
        services.AddScoped<IPlatformAdminService, PlatformAdminService>();
        services.AddScoped<IPlatformAdminManagementService, PlatformAdminManagementService>();

        services.AddScoped<IClinicOnboardingService, ClinicOnboardingService>();
        services.AddScoped<IClinicOnboardingApprovalService, ClinicOnboardingApprovalService>();
        services.AddScoped<IClinicActivationService, ClinicActivationService>();

        services.RegisterClinicOnboardingJobs();
        services.RegisterSubscriptionReminderJob();
        services.RegisterSubscriptionExpiryCheckJob();
        // DISABLED: PaymentRetryJob uses stub AttemptChargeAsync — will rebuild with self-registration
        // services.RegisterPaymentRetryJob();
        services.RegisterSubscriptionSuspensionJob();
        services.RegisterTrialReminderJob();
        services.RegisterRegistrationCleanupJob();
        services.RegisterDataDeletionJob();
        services.RegisterUsageResetJob();

        services.AddScoped<IFeatureService, FeatureService>();
        services.AddScoped<IFeaturePricingService, FeaturePricingService>();
        services.AddScoped<IFeatureCategoryService, FeatureCategoryService>();
        services.AddScoped<IPricingService, PricingService>();
        services.AddScoped<IPackageService, PackageService>();
        services.AddScoped<IPlatformSubscriptionService, PlatformSubscriptionService>();
        services.AddScoped<ISubscriptionLifecycleService, SubscriptionLifecycleService>();
        services.AddScoped<ISubscriptionModificationService, SubscriptionModificationService>();
        services.AddScoped<IUsageService, UsageService>();
        services.AddScoped<ISubscriptionNotificationService, SubscriptionNotificationService>();
        services.AddScoped<IAuditWriter, AuditWriter>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<ISubscriptionPaymentService, SubscriptionPaymentService>();
        services.AddScoped<IAddOnService, AddOnService>();
        services.AddScoped<IAddOnPurchaseService, AddOnPurchaseService>();
        services.AddScoped<IUsageAuditService, UsageAuditService>();

        services.RegisterAddOnExpiryJob();
        services.RegisterSubscriptionUsageJobs();

        services.AddScoped<IDataExportService, DataExportService>();

        services.AddScoped<IClinicDataImportJob, ClinicDataImportJob>();
        services.AddScoped<ClinicImportRequestValidator>();

        // Clinic Portal services
        services.AddScoped<IClinicDashboardService, ClinicDashboardService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<ITreatmentPlanService, TreatmentPlanService>();
        services.AddScoped<IClinicBranchService, ClinicBranchService>();
        services.AddScoped<IClinicStaffService, ClinicStaffService>();
        services.AddScoped<IClinicReportService, ClinicReportService>();

        // Billing services
        services.AddScoped<IInsuranceService, InsuranceService>();
        services.AddScoped<IDiscountService, DiscountService>();
        services.AddScoped<IClinicInvoiceService, ClinicInvoiceService>();
        services.AddScoped<IClinicPaymentService, ClinicPaymentService>();

        return services;
    }

    public static IServiceCollection AddLibraryServices(this IServiceCollection services)
    {
        services.AddScoped<IClinicLibraryQueryService, ClinicLibraryQueryService>();
        services.AddScoped<IClinicLibraryOverrideService, ClinicLibraryOverrideService>();
        services.AddScoped<IClinicLibraryService, ClinicLibraryService>();
        services.AddScoped<IBodyRegionService, BodyRegionService>();
        services.AddScoped<ITreatmentService, TreatmentService>();
        services.AddScoped<IExerciseService, ExerciseService>();
        services.AddScoped<IModalityService, ModalityService>();
        services.AddScoped<IAssessmentService, AssessmentService>();
        services.AddScoped<IDeviceService, DeviceService>();
        services.AddScoped<ITreatmentStageService, TreatmentStageService>();

        return services;
    }

    public static IServiceCollection AddAuthentication(this IServiceCollection services, IConfiguration config)
    {
        var jwtSettings = config.GetSection("JwtSettings");
        var secretKey = jwtSettings["Secret"];
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];

        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException("JWT Secret cannot be null or empty. Please set JwtSettings:Secret in configuration.");
        }

        if (secretKey.Length < 32)
        {
            throw new InvalidOperationException($"JWT Secret must be at least 32 characters (256 bits). Current length: {secretKey.Length}. Generate a secure key using: openssl rand -base64 32");
        }

        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new InvalidOperationException("JWT Issuer cannot be null or empty. Please set JwtSettings:Issuer in configuration.");
        }

        if (string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException("JWT Audience cannot be null or empty. Please set JwtSettings:Audience in configuration.");
        }

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey ?? string.Empty)),
                ClockSkew = TimeSpan.Zero,
                RoleClaimType = System.Security.Claims.ClaimTypes.Role
            };
        });

        services.AddAuthorization();

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IClinicRepository, ClinicRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IPackageRepository, PackageRepository>();
        services.AddScoped<IFeatureRepository, FeatureRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISubscriptionAddOnRepository, SubscriptionAddOnRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IOtpCodeRepository, OtpCodeRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<ISubscriptionFeatureUsageRepository, SubscriptionFeatureUsageRepository>();
        services.AddScoped<IUsageRecordRepository, UsageRecordRepository>();
        services.AddScoped<IFeatureCategoryRepository, FeatureCategoryRepository>();
        services.AddScoped<IFeaturePricingRepository, FeaturePricingRepository>();
        services.AddScoped<IClinicLibraryOverrideRepository, ClinicLibraryOverrideRepository>();
        services.AddScoped<IClinicOnboardingRepository, ClinicOnboardingRepository>();
        services.AddScoped<IOtpPasswordResetTokenRepository, OtpPasswordResetTokenRepository>();
        services.AddScoped<ITreatmentRepository, TreatmentRepository>();
        services.AddScoped<IExerciseRepository, ExerciseRepository>();
        services.AddScoped<IModalityRepository, ModalityRepository>();
        services.AddScoped<IAssessmentRepository, AssessmentRepository>();
        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<IBodyRegionCategoryRepository, BodyRegionCategoryRepository>();
        services.AddScoped<IPackageFeatureRepository, PackageFeatureRepository>();
        services.AddScoped<ITreatmentStageRepository, TreatmentStageRepository>();

        MapsterConfig.ConfigureMappings();

        return services;
    }

    public static IServiceCollection AddCloudinary(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<CloudinarySettings>(config.GetSection(CloudinarySettings.SectionName));
        services.AddScoped<IDocumentService, CloudinaryDocumentService>();
        services.AddScoped<IStorageService, CloudinaryStorageService>();
        services.AddScoped<IFileUploadService, CloudinaryFileUploadService>();
        services.AddScoped<IDocumentManagementService, DocumentManagementService>();
        return services;
    }

    public static IServiceCollection AddCustomCors(this IServiceCollection services, IConfiguration config, IWebHostEnvironment env)
    {
        var allowedOrigins = config.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        services.AddCors(options =>
        {
            // Development policy - more permissive for local development
            options.AddPolicy("Development",
                policy => policy
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader());

            // Production policy - restrictive, requires explicit origins
            options.AddPolicy("Production",
                policy =>
                {
                    if (allowedOrigins.Length == 0)
                    {
                        throw new InvalidOperationException(
                            "Production CORS policy requires at least one origin in 'AllowedOrigins' configuration. " +
                            "Set the AllowedOrigins environment variable or configuration value.");
                    }

                    policy
                        .WithOrigins(allowedOrigins)
                        .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
                        .WithHeaders("Content-Type", "Authorization", "Accept", "X-Requested-With", "X-Tenant-Id")
                        .AllowCredentials();
                });

            // Default policy (backward compatible) - uses configured origins
            options.AddPolicy("AllowFrontend",
                policy =>
                {
                    if (allowedOrigins.Length > 0)
                    {
                        policy
                            .WithOrigins(allowedOrigins)
                            .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
                            .WithHeaders("Content-Type", "Authorization", "Accept", "X-Requested-With", "X-Tenant-Id")
                            .AllowCredentials();
                    }
                    else
                    {
                        // Fallback for development when no origins configured
                        policy
                            .AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader();
                    }
                });
        });

        return services;
    }

    public static IServiceCollection AddHangfire(this IServiceCollection services, IConfiguration config)
    {
        var hangfireSettings = config.GetSection("HangfireSettings");
        var storageConnectionString = hangfireSettings["JobsStorageConnectionString"];
        var workerCount = int.Parse(hangfireSettings["WorkerCount"] ?? "5");
        var queues = (hangfireSettings["Queues"] ?? "default,high,low").Split(',');

        services.AddHangfire(configure => configure
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(storageConnectionString, new PostgreSqlStorageOptions
            {
                QueuePollInterval = TimeSpan.FromSeconds(15)
            }));

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = workerCount;
            options.Queues = queues;
        });

        services.AddScoped<IJobService, JobService>();

        return services;
    }

    public static IServiceCollection AddPaymentServices(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<AppSettings>(config.GetSection("AppSettings"));
        services.Configure<PaymentSettings>(config.GetSection("PaymentSettings"));
        services.AddHttpClient("PayMob");

        services.AddScoped<IPaymentService, PaymentService>();

        var settings = config.GetSection("PaymentSettings").Get<PaymentSettings>();
        if (settings?.Providers != null)
        {
            foreach (var providerConfig in settings.Providers)
            {
                services.AddScoped<IPaymentProvider>(sp =>
                {
                    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();

                    return providerConfig.Key.ToLower() switch
                    {
                        "cash" => new CashPaymentProvider(providerConfig),
                        "paymob" => new PayMobPaymentProvider(providerConfig, loggerFactory.CreateLogger<PayMobPaymentProvider>(), httpClientFactory),
                        "stripe" => new StripePaymentProvider(providerConfig, loggerFactory.CreateLogger<StripePaymentProvider>()),
                        _ => throw new NotSupportedException($"Payment provider {providerConfig.Key} not supported")
                    };
                });
            }
        }

        return services;
    }

    public static IServiceCollection AddCommunicationServices(this IServiceCollection services, IConfiguration config, IWebHostEnvironment env)
    {
        services.Configure<SmsSettings>(config.GetSection("SmsSettings"));
        services.Configure<EmailSettings>(config.GetSection("EmailSettings"));
        services.Configure<WhatsAppSettings>(config.GetSection("WhatsAppSettings"));

        services.AddScoped<IEmailService, MockEmailService>();
        services.AddScoped<ISmsService, MockSmsService>();
        services.AddScoped<IWhatsAppService, MockWhatsAppService>();

        services.Configure<NotificationSettings>(config.GetSection("NotificationSettings"));
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<INotificationChannel, EmailNotificationChannel>();

        return services;
    }
}
