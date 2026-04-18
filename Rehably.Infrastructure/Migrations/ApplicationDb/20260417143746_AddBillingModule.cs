using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rehably.Infrastructure.Migrations.ApplicationDb
{
    /// <inheritdoc />
    public partial class AddBillingModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClinicBillingPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefaultPaymentTiming = table.Column<int>(type: "integer", nullable: false),
                    AllowInstallments = table.Column<bool>(type: "boolean", nullable: false),
                    AllowDiscountStackWithInsurance = table.Column<bool>(type: "boolean", nullable: false),
                    AllowMultipleDiscounts = table.Column<bool>(type: "boolean", nullable: false),
                    RequirePreAuthForInsurance = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultCurrency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TaxRatePercent = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    InvoicePrefix = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NextInvoiceNumber = table.Column<int>(type: "integer", nullable: false),
                    AutoGenerateInvoice = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicBillingPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClinicInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    TreatmentPlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    InsuranceCoverageAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalDue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalPaid = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicInvoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Discounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NameArabic = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AppliesTo = table.Column<int>(type: "integer", nullable: false),
                    ApplicationMethod = table.Column<int>(type: "integer", nullable: false),
                    AutoCondition = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MaxUsageTotal = table.Column<int>(type: "integer", nullable: true),
                    MaxUsagePerPatient = table.Column<int>(type: "integer", nullable: true),
                    UsageCount = table.Column<int>(type: "integer", nullable: false),
                    TotalValueGiven = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Discounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InsuranceProviders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NameArabic = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    IsGlobal = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceProviders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClinicInvoiceLineItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DescriptionArabic = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    InsuranceCoverageAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ServiceType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicInvoiceLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicInvoiceLineItems_ClinicInvoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "ClinicInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClinicPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Method = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TransactionReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PaymentGateway = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    RecordedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicPayments_ClinicInvoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "ClinicInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InstallmentPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NumberOfInstallments = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstallmentPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstallmentPlans_ClinicInvoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "ClinicInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiscountUsages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscountId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    TreatmentPlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    AmountApplied = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AppliedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscountUsages_Discounts_DiscountId",
                        column: x => x.DiscountId,
                        principalTable: "Discounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SessionPackageOffers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscountId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionsToPurchase = table.Column<int>(type: "integer", nullable: false),
                    SessionsFree = table.Column<int>(type: "integer", nullable: false),
                    ValidForServiceType = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionPackageOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionPackageOffers_Discounts_DiscountId",
                        column: x => x.DiscountId,
                        principalTable: "Discounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClinicInsuranceProviders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uuid", nullable: false),
                    InsuranceProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreAuthRequired = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultCoveragePercent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicInsuranceProviders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicInsuranceProviders_InsuranceProviders_InsuranceProvid~",
                        column: x => x.InsuranceProviderId,
                        principalTable: "InsuranceProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InstallmentSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstallmentPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstallmentSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstallmentSchedules_InstallmentPlans_InstallmentPlanId",
                        column: x => x.InstallmentPlanId,
                        principalTable: "InstallmentPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InsuranceServiceRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicInsuranceProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceType = table.Column<int>(type: "integer", nullable: false),
                    CoverageType = table.Column<int>(type: "integer", nullable: false),
                    CoverageValue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceServiceRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InsuranceServiceRules_ClinicInsuranceProviders_ClinicInsura~",
                        column: x => x.ClinicInsuranceProviderId,
                        principalTable: "ClinicInsuranceProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientInsurances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicInsuranceProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MembershipId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    HolderName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CoveragePercent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    MaxAnnualCoverageAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientInsurances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientInsurances_ClinicInsuranceProviders_ClinicInsuranceP~",
                        column: x => x.ClinicInsuranceProviderId,
                        principalTable: "ClinicInsuranceProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InsuranceClaims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientInsuranceId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClaimNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    RejectedReason = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClinicInvoiceId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InsuranceClaims_ClinicInvoices_ClinicInvoiceId",
                        column: x => x.ClinicInvoiceId,
                        principalTable: "ClinicInvoices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InsuranceClaims_PatientInsurances_PatientInsuranceId",
                        column: x => x.PatientInsuranceId,
                        principalTable: "PatientInsurances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 17, 14, 37, 43, 685, DateTimeKind.Utc).AddTicks(9611));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 17, 14, 37, 43, 686, DateTimeKind.Utc).AddTicks(1077));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000003"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 17, 14, 37, 43, 686, DateTimeKind.Utc).AddTicks(1082));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000004"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 17, 14, 37, 43, 686, DateTimeKind.Utc).AddTicks(1084));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000005"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 17, 14, 37, 43, 686, DateTimeKind.Utc).AddTicks(1086));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000006"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 17, 14, 37, 43, 686, DateTimeKind.Utc).AddTicks(1088));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000007"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 17, 14, 37, 43, 686, DateTimeKind.Utc).AddTicks(1095));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000008"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 17, 14, 37, 43, 686, DateTimeKind.Utc).AddTicks(1097));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000009"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 17, 14, 37, 43, 686, DateTimeKind.Utc).AddTicks(1099));

            migrationBuilder.CreateIndex(
                name: "IX_ClinicBillingPolicies_ClinicId",
                table: "ClinicBillingPolicies",
                column: "ClinicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicInsuranceProviders_ClinicId",
                table: "ClinicInsuranceProviders",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicInsuranceProviders_ClinicId_InsuranceProviderId",
                table: "ClinicInsuranceProviders",
                columns: new[] { "ClinicId", "InsuranceProviderId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicInsuranceProviders_InsuranceProviderId",
                table: "ClinicInsuranceProviders",
                column: "InsuranceProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicInvoiceLineItems_InvoiceId",
                table: "ClinicInvoiceLineItems",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicInvoices_ClinicId",
                table: "ClinicInvoices",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicInvoices_ClinicId_InvoiceNumber",
                table: "ClinicInvoices",
                columns: new[] { "ClinicId", "InvoiceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicInvoices_PatientId",
                table: "ClinicInvoices",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicPayments_ClinicId",
                table: "ClinicPayments",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicPayments_InvoiceId",
                table: "ClinicPayments",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Discounts_ClinicId",
                table: "Discounts",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_Discounts_ClinicId_Code",
                table: "Discounts",
                columns: new[] { "ClinicId", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_DiscountUsages_DiscountId",
                table: "DiscountUsages",
                column: "DiscountId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountUsages_PatientId",
                table: "DiscountUsages",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_InstallmentPlans_InvoiceId",
                table: "InstallmentPlans",
                column: "InvoiceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstallmentSchedules_InstallmentPlanId",
                table: "InstallmentSchedules",
                column: "InstallmentPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_ClinicId",
                table: "InsuranceClaims",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_ClinicInvoiceId",
                table: "InsuranceClaims",
                column: "ClinicInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_PatientId",
                table: "InsuranceClaims",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_PatientInsuranceId",
                table: "InsuranceClaims",
                column: "PatientInsuranceId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_Status",
                table: "InsuranceClaims",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceProviders_IsActive",
                table: "InsuranceProviders",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceServiceRules_ClinicInsuranceProviderId",
                table: "InsuranceServiceRules",
                column: "ClinicInsuranceProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientInsurances_ClinicInsuranceProviderId",
                table: "PatientInsurances",
                column: "ClinicInsuranceProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientInsurances_PatientId",
                table: "PatientInsurances",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionPackageOffers_DiscountId",
                table: "SessionPackageOffers",
                column: "DiscountId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicBillingPolicies");

            migrationBuilder.DropTable(
                name: "ClinicInvoiceLineItems");

            migrationBuilder.DropTable(
                name: "ClinicPayments");

            migrationBuilder.DropTable(
                name: "DiscountUsages");

            migrationBuilder.DropTable(
                name: "InstallmentSchedules");

            migrationBuilder.DropTable(
                name: "InsuranceClaims");

            migrationBuilder.DropTable(
                name: "InsuranceServiceRules");

            migrationBuilder.DropTable(
                name: "SessionPackageOffers");

            migrationBuilder.DropTable(
                name: "InstallmentPlans");

            migrationBuilder.DropTable(
                name: "PatientInsurances");

            migrationBuilder.DropTable(
                name: "Discounts");

            migrationBuilder.DropTable(
                name: "ClinicInvoices");

            migrationBuilder.DropTable(
                name: "ClinicInsuranceProviders");

            migrationBuilder.DropTable(
                name: "InsuranceProviders");

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 16, 21, 25, 24, 607, DateTimeKind.Utc).AddTicks(9128));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 16, 21, 25, 24, 608, DateTimeKind.Utc).AddTicks(468));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000003"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 16, 21, 25, 24, 608, DateTimeKind.Utc).AddTicks(475));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000004"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 16, 21, 25, 24, 608, DateTimeKind.Utc).AddTicks(477));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000005"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 16, 21, 25, 24, 608, DateTimeKind.Utc).AddTicks(480));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000006"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 16, 21, 25, 24, 608, DateTimeKind.Utc).AddTicks(482));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000007"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 16, 21, 25, 24, 608, DateTimeKind.Utc).AddTicks(493));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000008"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 16, 21, 25, 24, 608, DateTimeKind.Utc).AddTicks(509));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000009"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 16, 21, 25, 24, 608, DateTimeKind.Utc).AddTicks(512));
        }
    }
}
