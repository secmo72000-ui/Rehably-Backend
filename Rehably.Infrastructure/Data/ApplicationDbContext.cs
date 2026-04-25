using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Rehably.Application.Contexts;
using Rehably.Domain.Entities.Audit;
using Rehably.Domain.Entities.Billing;
using Rehably.Domain.Entities.Clinical;
using Rehably.Domain.Entities.ClinicPortal;
using Rehably.Domain.Entities.Identity;
using Rehably.Domain.Entities.Library;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Interceptors;

namespace Rehably.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly AuditInterceptor? _auditInterceptor;
    private readonly ITenantContext? _tenantContext;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, AuditInterceptor? auditInterceptor, ITenantContext? tenantContext)
        : base(options)
    {
        _auditInterceptor = auditInterceptor;
        _tenantContext = tenantContext;
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<OtpPasswordResetToken> OtpPasswordResetTokens => Set<OtpPasswordResetToken>();
    public DbSet<Clinic> Clinics => Set<Clinic>();
    public DbSet<UsageHistory> UsageHistories => Set<UsageHistory>();
    public DbSet<FeatureCategory> FeatureCategories => Set<FeatureCategory>();
    public DbSet<Feature> Features => Set<Feature>();
    public DbSet<FeaturePricing> FeaturePricings => Set<FeaturePricing>();
    public DbSet<Package> Packages => Set<Package>();
    public DbSet<PackageFeature> PackageFeatures => Set<PackageFeature>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<SubscriptionFeatureUsage> SubscriptionFeatureUsages => Set<SubscriptionFeatureUsage>();
    public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();
    public DbSet<UsageAuditLog> UsageAuditLogs => Set<UsageAuditLog>();

    public DbSet<ClinicOnboarding> ClinicOnboardings => Set<ClinicOnboarding>();
    public DbSet<ClinicDocument> ClinicDocuments => Set<ClinicDocument>();
    public DbSet<UsageRecord> UsageRecords => Set<UsageRecord>();

    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<SubscriptionNotification> SubscriptionNotifications => Set<SubscriptionNotification>();
    public DbSet<TaxConfiguration> TaxConfigurations => Set<TaxConfiguration>();
    public DbSet<SubscriptionAddOn> SubscriptionAddOns => Set<SubscriptionAddOn>();

    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<Treatment> Treatments => Set<Treatment>();
    public DbSet<TreatmentPhase> TreatmentPhases => Set<TreatmentPhase>();
    public DbSet<Modality> Modalities => Set<Modality>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<BodyRegionCategory> BodyRegionCategories => Set<BodyRegionCategory>();
    public DbSet<ClinicLibraryOverride> ClinicLibraryOverrides => Set<ClinicLibraryOverride>();
    public DbSet<TreatmentStage> TreatmentStages => Set<TreatmentStage>();

    // ===== Clinic Portal Entities =====
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<TreatmentPlan> TreatmentPlans => Set<TreatmentPlan>();
    public DbSet<TherapySession> TherapySessions => Set<TherapySession>();
    public DbSet<ClinicBranch> ClinicBranches => Set<ClinicBranch>();
    public DbSet<ClinicWorkingHours> ClinicWorkingHours => Set<ClinicWorkingHours>();

    // ── Clinical Assessment ────────────────────────────────────────────────────
    public DbSet<Speciality> Specialities => Set<Speciality>();
    public DbSet<Diagnosis> Diagnoses => Set<Diagnosis>();
    public DbSet<PatientAssessment> PatientAssessments => Set<PatientAssessment>();
    public DbSet<AssessmentPostOp> AssessmentPostOps => Set<AssessmentPostOp>();
    public DbSet<AssessmentRedFlags> AssessmentRedFlags => Set<AssessmentRedFlags>();
    public DbSet<AssessmentSubjective> AssessmentSubjectives => Set<AssessmentSubjective>();
    public DbSet<AssessmentObjective> AssessmentObjectives => Set<AssessmentObjective>();
    public DbSet<AssessmentNeuro> AssessmentNeuros => Set<AssessmentNeuro>();
    public DbSet<AssessmentClinicalReasoning> AssessmentClinicalReasonings => Set<AssessmentClinicalReasoning>();
    public DbSet<ClinicAssessmentFieldConfig> ClinicAssessmentFieldConfigs => Set<ClinicAssessmentFieldConfig>();

    // ===== Billing =====
    public DbSet<InsuranceProvider> InsuranceProviders => Set<InsuranceProvider>();
    public DbSet<ClinicInsuranceProvider> ClinicInsuranceProviders => Set<ClinicInsuranceProvider>();
    public DbSet<PatientInsurance> PatientInsurances => Set<PatientInsurance>();
    public DbSet<InsuranceServiceRule> InsuranceServiceRules => Set<InsuranceServiceRule>();
    public DbSet<InsuranceClaim> InsuranceClaims => Set<InsuranceClaim>();
    public DbSet<Discount> Discounts => Set<Discount>();
    public DbSet<SessionPackageOffer> SessionPackageOffers => Set<SessionPackageOffer>();
    public DbSet<DiscountUsage> DiscountUsages => Set<DiscountUsage>();
    public DbSet<ClinicInvoice> ClinicInvoices => Set<ClinicInvoice>();
    public DbSet<ClinicInvoiceLineItem> ClinicInvoiceLineItems => Set<ClinicInvoiceLineItem>();
    public DbSet<ClinicPayment> ClinicPayments => Set<ClinicPayment>();
    public DbSet<InstallmentPlan> InstallmentPlans => Set<InstallmentPlan>();
    public DbSet<InstallmentSchedule> InstallmentSchedules => Set<InstallmentSchedule>();
    public DbSet<ClinicBillingPolicy> ClinicBillingPolicies => Set<ClinicBillingPolicy>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (_auditInterceptor != null)
        {
            optionsBuilder.AddInterceptors(_auditInterceptor);
        }
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.ClinicId);
            entity.HasIndex(e => new { e.ResetTokenSelector, e.ResetTokenExpiry })
                .HasDatabaseName("IX_ApplicationUser_ResetToken");
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.ProfileImageUrl).HasMaxLength(500);

            entity.HasOne<Clinic>()
                .WithMany()
                .HasForeignKey(e => e.ClinicId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasQueryFilter(u => _tenantContext == null || !_tenantContext.TenantId.HasValue || u.TenantId == _tenantContext.TenantId);
        });

        builder.Entity<Clinic>().HasQueryFilter(e => !e.IsDeleted && (_tenantContext == null || !_tenantContext.TenantId.HasValue || e.Id == _tenantContext.TenantId));

        builder.Entity<Subscription>().HasQueryFilter(e => _tenantContext == null || !_tenantContext.TenantId.HasValue || e.ClinicId == _tenantContext.TenantId);
        builder.Entity<Invoice>().HasQueryFilter(e => _tenantContext == null || !_tenantContext.TenantId.HasValue || e.ClinicId == _tenantContext.TenantId);
        builder.Entity<Payment>().HasQueryFilter(e => _tenantContext == null || !_tenantContext.TenantId.HasValue || e.ClinicId == _tenantContext.TenantId);
        builder.Entity<UsageRecord>().HasQueryFilter(e => _tenantContext == null || !_tenantContext.TenantId.HasValue || e.ClinicId == _tenantContext.TenantId);
        builder.Entity<ClinicDocument>().HasQueryFilter(e => _tenantContext == null || !_tenantContext.TenantId.HasValue || e.ClinicId == _tenantContext.TenantId);
        builder.Entity<ClinicOnboarding>().HasQueryFilter(e => _tenantContext == null || !_tenantContext.TenantId.HasValue || e.ClinicId == _tenantContext.TenantId);
        builder.Entity<UsageHistory>().HasQueryFilter(e => _tenantContext == null || !_tenantContext.TenantId.HasValue || e.ClinicId == _tenantContext.TenantId);
        builder.Entity<UsageAuditLog>().HasQueryFilter(e => _tenantContext == null || !_tenantContext.TenantId.HasValue || e.ClinicId == _tenantContext.TenantId);
        builder.Entity<ClinicLibraryOverride>().HasQueryFilter(e => !e.IsDeleted && (_tenantContext == null || !_tenantContext.TenantId.HasValue || e.ClinicId == _tenantContext.TenantId));

        builder.Entity<Treatment>().HasQueryFilter(e => !e.IsDeleted && (_tenantContext == null || !_tenantContext.TenantId.HasValue || e.ClinicId == null || e.ClinicId == _tenantContext.TenantId));
        builder.Entity<Exercise>().HasQueryFilter(e => !e.IsDeleted && (_tenantContext == null || !_tenantContext.TenantId.HasValue || e.ClinicId == null || e.ClinicId == _tenantContext.TenantId));
        builder.Entity<Modality>().HasQueryFilter(e => !e.IsDeleted && (_tenantContext == null || !_tenantContext.TenantId.HasValue || e.ClinicId == null || e.ClinicId == _tenantContext.TenantId));
        builder.Entity<Device>().HasQueryFilter(e => !e.IsDeleted && (_tenantContext == null || !_tenantContext.TenantId.HasValue || e.ClinicId == null || e.ClinicId == _tenantContext.TenantId));
        builder.Entity<Assessment>().HasQueryFilter(e => !e.IsDeleted && (_tenantContext == null || !_tenantContext.TenantId.HasValue || e.ClinicId == null || e.ClinicId == _tenantContext.TenantId));
        builder.Entity<TreatmentPhase>().HasQueryFilter(e => !e.IsDeleted && (_tenantContext == null || !_tenantContext.TenantId.HasValue || e.ClinicId == null || e.ClinicId == _tenantContext.TenantId));

        builder.Entity<TreatmentStage>().HasQueryFilter(e => !e.IsDeleted && (_tenantContext == null || !_tenantContext.TenantId.HasValue || e.TenantId == _tenantContext.TenantId));

        // Standalone soft-delete filters (no ClinicId)
        builder.Entity<Feature>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<FeatureCategory>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Package>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<InvoiceLineItem>().HasQueryFilter(e => !e.IsDeleted);

        builder.Entity<SubscriptionAddOn>().HasQueryFilter(e => _tenantContext == null || !_tenantContext.TenantId.HasValue || e.ClinicId == _tenantContext.TenantId);
        builder.Entity<SubscriptionFeatureUsage>().HasQueryFilter(e => _tenantContext == null || !_tenantContext.TenantId.HasValue || e.ClinicId == _tenantContext.TenantId);
        builder.Entity<SubscriptionNotification>().HasQueryFilter(e => _tenantContext == null || !_tenantContext.TenantId.HasValue || e.ClinicId == _tenantContext.TenantId);

        // ===== Clinic Portal Query Filters (multi-tenancy + soft delete) =====
        builder.Entity<Patient>(entity =>
        {
            entity.HasQueryFilter(e => !e.IsDeleted && (_tenantContext == null || !_tenantContext.TenantId.HasValue || e.ClinicId == _tenantContext.TenantId));
            entity.HasIndex(e => e.ClinicId);
            entity.HasIndex(e => new { e.ClinicId, e.Status });
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.HasMany(e => e.Appointments).WithOne(a => a.Patient).HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(e => e.TreatmentPlans).WithOne(t => t.Patient).HasForeignKey(t => t.PatientId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Appointment>(entity =>
        {
            entity.HasQueryFilter(e => !e.IsDeleted && (_tenantContext == null || !_tenantContext.TenantId.HasValue || e.ClinicId == _tenantContext.TenantId));
            entity.HasIndex(e => e.ClinicId);
            entity.HasIndex(e => new { e.ClinicId, e.StartTime });
            entity.HasIndex(e => new { e.ClinicId, e.PatientId });
            entity.Property(e => e.Title).HasMaxLength(200);
        });

        builder.Entity<TreatmentPlan>(entity =>
        {
            entity.HasQueryFilter(e => !e.IsDeleted && (_tenantContext == null || !_tenantContext.TenantId.HasValue || e.ClinicId == _tenantContext.TenantId));
            entity.HasIndex(e => e.ClinicId);
            entity.HasIndex(e => new { e.ClinicId, e.PatientId });
            entity.Property(e => e.Title).HasMaxLength(300);
            entity.HasMany(e => e.Sessions).WithOne(s => s.TreatmentPlan).HasForeignKey(s => s.TreatmentPlanId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(e => e.Appointments).WithOne(a => a.TreatmentPlan).HasForeignKey(a => a.TreatmentPlanId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<TherapySession>(entity =>
        {
            entity.HasQueryFilter(e => !e.IsDeleted && (_tenantContext == null || !_tenantContext.TenantId.HasValue || e.ClinicId == _tenantContext.TenantId));
            entity.HasIndex(e => e.ClinicId);
            entity.HasIndex(e => new { e.ClinicId, e.TreatmentPlanId });
            entity.HasIndex(e => new { e.ClinicId, e.PatientId });
        });

        builder.Entity<ClinicBranch>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.HasIndex(b => b.ClinicId);
            entity.Property(b => b.Name).IsRequired().HasMaxLength(200);
            entity.Property(b => b.NameArabic).HasMaxLength(200);
            entity.Property(b => b.Phone).HasMaxLength(30);
            entity.Property(b => b.Email).HasMaxLength(200);
            entity.Property(b => b.Address).HasMaxLength(500);
            entity.Property(b => b.City).HasMaxLength(100);
        });

        builder.Entity<ClinicWorkingHours>(entity =>
        {
            entity.HasKey(w => w.Id);
            entity.HasIndex(w => new { w.ClinicId, w.DayOfWeek }).IsUnique();
            entity.HasIndex(w => w.ClinicId);
            entity.Property(w => w.OpenTime).HasMaxLength(5);
            entity.Property(w => w.CloseTime).HasMaxLength(5);
        });

        // ── Clinical Assessment ────────────────────────────────────────────────
        builder.Entity<Speciality>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
            entity.Property(e => e.NameEn).IsRequired().HasMaxLength(100);
            entity.Property(e => e.NameAr).IsRequired().HasMaxLength(100);
            entity.Property(e => e.IcdChapters).HasMaxLength(50);
            entity.HasIndex(e => e.Code).IsUnique();
        });

        builder.Entity<Diagnosis>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.IcdCode).IsRequired().HasMaxLength(20);
            entity.Property(e => e.NameEn).IsRequired().HasMaxLength(300);
            entity.Property(e => e.NameAr).IsRequired().HasMaxLength(300);
            entity.HasIndex(e => new { e.IcdCode, e.ClinicId }).IsUnique();
            entity.HasIndex(e => e.SpecialityId);
            entity.HasOne(e => e.Speciality).WithMany(s => s.Diagnoses)
                  .HasForeignKey(e => e.SpecialityId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.BodyRegion).WithMany()
                  .HasForeignKey(e => e.BodyRegionCategoryId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<PatientAssessment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.AppointmentId);
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.ClinicId);
            entity.HasOne(e => e.PostOp).WithOne(p => p.Assessment)
                  .HasForeignKey<AssessmentPostOp>(p => p.AssessmentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.RedFlags).WithOne(r => r.Assessment)
                  .HasForeignKey<AssessmentRedFlags>(r => r.AssessmentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Subjective).WithOne(s => s.Assessment)
                  .HasForeignKey<AssessmentSubjective>(s => s.AssessmentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Objective).WithOne(o => o.Assessment)
                  .HasForeignKey<AssessmentObjective>(o => o.AssessmentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Neuro).WithOne(n => n.Assessment)
                  .HasForeignKey<AssessmentNeuro>(n => n.AssessmentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ClinicalReasoning).WithOne(c => c.Assessment)
                  .HasForeignKey<AssessmentClinicalReasoning>(c => c.AssessmentId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ClinicAssessmentFieldConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ClinicId, e.StepNumber, e.FieldKey }).IsUnique();
        });

        builder.Entity<ApplicationRole>(entity =>
        {
            entity.HasIndex(e => e.TenantId);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        builder.Entity<Permission>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Resource).HasMaxLength(50);
            entity.Property(e => e.Action).HasMaxLength(50);
        });

        builder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(rp => new { rp.RoleId, rp.PermissionId });
            entity.Property(rp => rp.RoleId).HasMaxLength(450);

            entity.HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ApplicationUserRole>(entity =>
        {
            entity.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.IsRevoked, e.ExpiresAt });

            entity.Property(e => e.Token).HasMaxLength(500).IsRequired();
            entity.Property(e => e.RevocationReason).HasMaxLength(500);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<OtpPasswordResetToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).HasMaxLength(450).IsRequired();
            entity.Property(e => e.TokenHash).HasMaxLength(128).IsRequired();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.TokenHash);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Clinic>(entity =>
        {
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.IsDeleted, e.Status })
                .HasDatabaseName("IX_Clinic_IsDeleted_Status");

            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.NameArabic).HasMaxLength(200);
            entity.Property(e => e.Slug).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Phone).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.BanReason).HasMaxLength(500);
            entity.Property(e => e.BannedBy).HasMaxLength(450);

            entity.HasOne(e => e.CurrentSubscription)
                .WithOne()
                .HasForeignKey<Clinic>(e => e.CurrentSubscriptionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<UsageHistory>(entity =>
        {
            entity.HasIndex(e => e.ClinicId);
            entity.HasIndex(e => new { e.ClinicId, e.MetricType, e.RecordedAt });

            entity.HasOne(e => e.Clinic)
                .WithMany(c => c.UsageHistories)
                .HasForeignKey(e => e.ClinicId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<FeatureCategory>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.ParentCategoryId);

            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Code).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Icon).HasMaxLength(100);

            entity.HasOne(e => e.ParentCategory)
                .WithMany(p => p.SubCategories)
                .HasForeignKey(e => e.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Feature>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.CategoryId);

            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Code).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Features)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<FeaturePricing>(entity =>
        {
            entity.HasIndex(e => e.FeatureId);

            entity.Property(e => e.BasePrice).HasPrecision(18, 2);
            entity.Property(e => e.PerUnitPrice).HasPrecision(18, 4);

            entity.HasOne(fp => fp.Feature)
                .WithMany(f => f.PricingHistory)
                .HasForeignKey(fp => fp.FeatureId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Package>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.IsPublic);
            entity.HasIndex(e => e.IsCustom);
            entity.HasIndex(e => e.ForClinicId);

            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Code).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.MonthlyPrice).HasPrecision(18, 2);
            entity.Property(e => e.YearlyPrice).HasPrecision(18, 2);
            entity.Property(e => e.CalculatedMonthlyPrice).HasPrecision(18, 2);
            entity.Property(e => e.CalculatedYearlyPrice).HasPrecision(18, 2);
        });

        builder.Entity<PackageFeature>(entity =>
        {
            entity.HasIndex(e => new { e.PackageId, e.FeatureId }).IsUnique();

            entity.HasOne(pf => pf.Package)
                .WithMany(p => p.Features)
                .HasForeignKey(pf => pf.PackageId)
                .HasPrincipalKey(p => p.Id)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pf => pf.Feature)
                .WithMany(f => f.PackageFeatures)
                .HasForeignKey(pf => pf.FeatureId)
                .HasPrincipalKey(f => f.Id)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Subscription>(entity =>
        {
            entity.Property<uint>("xmin").IsRowVersion();
            entity.HasIndex(e => e.ClinicId);
            entity.HasIndex(e => e.PackageId);
            entity.HasIndex(e => new { e.ClinicId, e.Status });

            entity.Property(e => e.PriceSnapshot).HasColumnType("jsonb");
            entity.Property(e => e.PaymentProvider).HasMaxLength(50);
            entity.Property(e => e.ProviderSubscriptionId).HasMaxLength(500);
            entity.Property(e => e.CancelReason).HasMaxLength(500);

            entity.HasOne(s => s.Package)
                .WithMany(p => p.Subscriptions)
                .HasForeignKey(s => s.PackageId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SubscriptionFeatureUsage>(entity =>
        {
            entity.HasIndex(e => new { e.SubscriptionId, e.FeatureId }).IsUnique();

            entity.HasOne(sf => sf.Subscription)
                .WithMany(s => s.FeatureUsage)
                .HasForeignKey(sf => sf.SubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(sf => sf.Feature)
                .WithMany()
                .HasForeignKey(sf => sf.FeatureId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<WebhookEvent>(entity =>
        {
            entity.HasIndex(e => e.Provider);
            entity.HasIndex(e => e.Processed);
            entity.HasIndex(e => e.SubscriptionId);

            entity.Property(e => e.Provider).HasMaxLength(50).IsRequired();
            entity.Property(e => e.EventType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Payload).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.ProcessingError).HasMaxLength(1000);
        });

        builder.Entity<ClinicOnboarding>(entity =>
        {
            entity.HasIndex(e => e.ClinicId).IsUnique();

            entity.HasOne(e => e.Clinic)
                .WithOne(c => c.Onboarding)
                .HasForeignKey<ClinicOnboarding>(e => e.ClinicId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ClinicDocument>(entity =>
        {
            entity.HasIndex(e => e.ClinicId);
            entity.HasIndex(e => e.DocumentType);
            entity.HasIndex(e => new { e.ClinicId, e.DocumentType });

            entity.Property(e => e.StorageUrl).HasMaxLength(500).IsRequired();
            entity.Property(e => e.PublicUrl).HasMaxLength(500);
            entity.Property(e => e.RejectionReason).HasMaxLength(1000);

            entity.HasOne(e => e.Clinic)
                .WithMany(c => c.Documents)
                .HasForeignKey(e => e.ClinicId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UsageRecord>(entity =>
        {
            entity.HasIndex(e => e.ClinicId);
            entity.HasIndex(e => e.Metric);
            entity.HasIndex(e => new { e.ClinicId, e.Metric, e.Period }).IsUnique();

            entity.HasOne(e => e.Clinic)
                .WithMany(c => c.UsageRecords)
                .HasForeignKey(e => e.ClinicId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Invoice>(entity =>
        {
            entity.Property<uint>("xmin").IsRowVersion();
            entity.HasIndex(e => e.ClinicId);
            entity.HasIndex(e => e.SubscriptionId);
            entity.HasIndex(e => e.InvoiceNumber).IsUnique();
            entity.HasIndex(e => e.DueDate);
            entity.HasIndex(e => new { e.ClinicId, e.DueDate });

            entity.Property(e => e.InvoiceNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.TaxAmount).HasPrecision(18, 2);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.PaidVia).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasOne(e => e.Clinic)
                .WithMany()
                .HasForeignKey(e => e.ClinicId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Subscription)
                .WithMany()
                .HasForeignKey(e => e.SubscriptionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Payment>(entity =>
        {
            entity.Property<uint>("xmin").IsRowVersion();
            entity.HasIndex(e => e.ClinicId);
            entity.HasIndex(e => e.InvoiceId);
            entity.HasIndex(e => e.Provider);
            entity.HasIndex(e => e.ProviderTransactionId);
            entity.HasIndex(e => e.Status);

            entity.Property(e => e.Currency).HasMaxLength(3).IsRequired();
            entity.Property(e => e.Provider).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ProviderTransactionId).HasMaxLength(200);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.FailureReason).HasMaxLength(1000);
            entity.Property(e => e.Metadata).HasColumnType("jsonb");

            entity.HasOne(e => e.Clinic)
                .WithMany()
                .HasForeignKey(e => e.ClinicId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Invoice)
                .WithMany(i => i.Payments)
                .HasForeignKey(e => e.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TaxConfiguration>(entity =>
        {
            entity.HasIndex(e => new { e.CountryCode, e.EffectiveDate }).IsDescending(false, true);
            entity.HasIndex(e => e.IsDefault);

            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CountryCode).HasMaxLength(2);
            entity.Property(e => e.TaxRate).HasPrecision(5, 2);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
        });

        builder.Entity<SubscriptionNotification>(entity =>
        {
            entity.HasIndex(e => e.SubscriptionId);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Channel);
            entity.HasIndex(e => e.IsSent);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.SubscriptionId, e.Type, e.IsSent });

            entity.Property(e => e.Subject).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Message).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.Recipient).HasMaxLength(500);
            entity.Property(e => e.Error).HasMaxLength(1000);

            entity.HasOne(e => e.Subscription)
                .WithMany()
                .HasForeignKey(e => e.SubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<OtpCode>(entity =>
        {
            entity.HasIndex(e => new { e.Contact, e.Purpose, e.IsUsed, e.ExpiresAt })
                .HasDatabaseName("IX_OtpCode_Lookup");
        });

        builder.Entity<Treatment>().HasIndex(e => new { e.ClinicId, e.IsDeleted })
            .HasDatabaseName("IX_Treatment_ClinicId_IsDeleted");
        builder.Entity<Exercise>().HasIndex(e => new { e.ClinicId, e.IsDeleted })
            .HasDatabaseName("IX_Exercise_ClinicId_IsDeleted");
        builder.Entity<Modality>().HasIndex(e => new { e.ClinicId, e.IsDeleted })
            .HasDatabaseName("IX_Modality_ClinicId_IsDeleted");
        builder.Entity<Assessment>().HasIndex(e => new { e.ClinicId, e.IsDeleted })
            .HasDatabaseName("IX_Assessment_ClinicId_IsDeleted");
        builder.Entity<Device>().HasIndex(e => new { e.ClinicId, e.IsDeleted })
            .HasDatabaseName("IX_Device_ClinicId_IsDeleted");

        // ===== Billing Model Config =====

        builder.Entity<InsuranceProvider>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.NameArabic).HasMaxLength(200);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.HasIndex(e => e.IsActive);
        });

        builder.Entity<ClinicInsuranceProvider>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ClinicId);
            entity.HasIndex(e => new { e.ClinicId, e.InsuranceProviderId }).IsUnique();
            entity.Property(e => e.DefaultCoveragePercent).HasColumnType("decimal(5,2)");
            entity.HasQueryFilter(e => _tenantContext == null || !_tenantContext.TenantId.HasValue || e.ClinicId == _tenantContext.TenantId);

            entity.HasOne(e => e.Provider)
                .WithMany(p => p.ClinicProviders)
                .HasForeignKey(e => e.InsuranceProviderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PatientInsurance>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PatientId);
            entity.Property(e => e.CoveragePercent).HasColumnType("decimal(5,2)");
            entity.Property(e => e.MaxAnnualCoverageAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.PolicyNumber).HasMaxLength(100);
            entity.Property(e => e.MembershipId).HasMaxLength(100);
            entity.Property(e => e.HolderName).HasMaxLength(200);

            entity.HasOne(e => e.ClinicInsuranceProvider)
                .WithMany(c => c.PatientPolicies)
                .HasForeignKey(e => e.ClinicInsuranceProviderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<InsuranceServiceRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CoverageValue).HasColumnType("decimal(18,2)");

            entity.HasOne(e => e.ClinicInsuranceProvider)
                .WithMany(c => c.ServiceRules)
                .HasForeignKey(e => e.ClinicInsuranceProviderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<InsuranceClaim>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ClinicId);
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.Status);
            entity.Property(e => e.ClaimNumber).HasMaxLength(100);
            entity.Property(e => e.ApprovedAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.PaidAmount).HasColumnType("decimal(18,2)");
            entity.HasQueryFilter(e => _tenantContext == null || !_tenantContext.TenantId.HasValue || e.ClinicId == _tenantContext.TenantId);

            entity.HasOne(e => e.PatientInsurance)
                .WithMany(p => p.Claims)
                .HasForeignKey(e => e.PatientInsuranceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Discount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ClinicId);
            entity.HasIndex(e => new { e.ClinicId, e.Code });
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.NameArabic).HasMaxLength(200);
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Value).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalValueGiven).HasColumnType("decimal(18,2)");
            entity.HasQueryFilter(e => _tenantContext == null || !_tenantContext.TenantId.HasValue || e.ClinicId == _tenantContext.TenantId);
        });

        builder.Entity<SessionPackageOffer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Discount)
                .WithOne(d => d.PackageOffer)
                .HasForeignKey<SessionPackageOffer>(e => e.DiscountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<DiscountUsage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DiscountId);
            entity.HasIndex(e => e.PatientId);
            entity.Property(e => e.AmountApplied).HasColumnType("decimal(18,2)");
            entity.Property(e => e.AppliedByUserId).HasMaxLength(450);

            entity.HasOne(e => e.Discount)
                .WithMany(d => d.Usages)
                .HasForeignKey(e => e.DiscountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ClinicInvoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ClinicId);
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => new { e.ClinicId, e.InvoiceNumber }).IsUnique();
            entity.Property(e => e.InvoiceNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Currency).HasMaxLength(10);
            entity.Property(e => e.SubTotal).HasColumnType("decimal(18,2)");
            entity.Property(e => e.InsuranceCoverageAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TaxAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalDue).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalPaid).HasColumnType("decimal(18,2)");
            entity.HasQueryFilter(e => _tenantContext == null || !_tenantContext.TenantId.HasValue || e.ClinicId == _tenantContext.TenantId);
        });

        builder.Entity<ClinicInvoiceLineItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).HasMaxLength(500).IsRequired();
            entity.Property(e => e.DescriptionArabic).HasMaxLength(500);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.InsuranceCoverageAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.LineTotal).HasColumnType("decimal(18,2)");

            entity.HasOne(e => e.Invoice)
                .WithMany(i => i.LineItems)
                .HasForeignKey(e => e.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ClinicPayment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ClinicId);
            entity.HasIndex(e => e.InvoiceId);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TransactionReference).HasMaxLength(200);
            entity.Property(e => e.PaymentGateway).HasMaxLength(50);
            entity.Property(e => e.RecordedByUserId).HasMaxLength(450);
            entity.HasQueryFilter(e => _tenantContext == null || !_tenantContext.TenantId.HasValue || e.ClinicId == _tenantContext.TenantId);

            entity.HasOne(e => e.Invoice)
                .WithMany(i => i.Payments)
                .HasForeignKey(e => e.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<InstallmentPlan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");

            entity.HasOne(e => e.Invoice)
                .WithOne(i => i.InstallmentPlan)
                .HasForeignKey<InstallmentPlan>(e => e.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<InstallmentSchedule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");

            entity.HasOne(e => e.Plan)
                .WithMany(p => p.Schedule)
                .HasForeignKey(e => e.InstallmentPlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ClinicBillingPolicy>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ClinicId).IsUnique();
            entity.Property(e => e.DefaultCurrency).HasMaxLength(10);
            entity.Property(e => e.TaxRatePercent).HasColumnType("decimal(5,2)");
            entity.Property(e => e.InvoicePrefix).HasMaxLength(20);
        });

    }
}
