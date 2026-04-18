using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rehably.Infrastructure.Migrations.ApplicationDb
{
    /// <inheritdoc />
    public partial class BackendReviewFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TreatmentId",
                table: "TreatmentPhases",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Subscriptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Subscriptions",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<Guid>(
                name: "ClinicId",
                table: "SubscriptionNotifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClinicId",
                table: "SubscriptionFeatureUsages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClinicId",
                table: "SubscriptionAddOns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Payments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Payments",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Invoices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Invoices",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 17, 1, 3, 53, 545, DateTimeKind.Utc).AddTicks(1898));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 17, 1, 3, 53, 545, DateTimeKind.Utc).AddTicks(3064));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000003"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 17, 1, 3, 53, 545, DateTimeKind.Utc).AddTicks(3069));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000004"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 17, 1, 3, 53, 545, DateTimeKind.Utc).AddTicks(3071));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000005"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 17, 1, 3, 53, 545, DateTimeKind.Utc).AddTicks(3073));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000006"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 17, 1, 3, 53, 545, DateTimeKind.Utc).AddTicks(3076));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000007"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 17, 1, 3, 53, 545, DateTimeKind.Utc).AddTicks(3078));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000008"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 17, 1, 3, 53, 545, DateTimeKind.Utc).AddTicks(3080));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000009"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 17, 1, 3, 53, 545, DateTimeKind.Utc).AddTicks(3085));

            migrationBuilder.CreateIndex(
                name: "IX_Treatment_ClinicId_IsDeleted",
                table: "Treatments",
                columns: new[] { "ClinicId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentPhases_TreatmentId",
                table: "TreatmentPhases",
                column: "TreatmentId");

            migrationBuilder.CreateIndex(
                name: "IX_OtpCode_Lookup",
                table: "OtpCodes",
                columns: new[] { "Contact", "Purpose", "IsUsed", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Modality_ClinicId_IsDeleted",
                table: "Modalities",
                columns: new[] { "ClinicId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Exercise_ClinicId_IsDeleted",
                table: "Exercises",
                columns: new[] { "ClinicId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Device_ClinicId_IsDeleted",
                table: "Devices",
                columns: new[] { "ClinicId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Assessment_ClinicId_IsDeleted",
                table: "Assessments",
                columns: new[] { "ClinicId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUser_ResetToken",
                table: "AspNetUsers",
                columns: new[] { "ResetTokenSelector", "ResetTokenExpiry" });

            migrationBuilder.AddForeignKey(
                name: "FK_TreatmentPhases_Treatments_TreatmentId",
                table: "TreatmentPhases",
                column: "TreatmentId",
                principalTable: "Treatments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // --- Custom SQL: Backfill ClinicId from Subscriptions ---
            migrationBuilder.Sql("""
                UPDATE "SubscriptionAddOns" SET "ClinicId" = s."ClinicId"
                FROM "Subscriptions" s WHERE s."Id" = "SubscriptionAddOns"."SubscriptionId";
                """);

            migrationBuilder.Sql("""
                UPDATE "SubscriptionFeatureUsages" SET "ClinicId" = s."ClinicId"
                FROM "Subscriptions" s WHERE s."Id" = "SubscriptionFeatureUsages"."SubscriptionId";
                """);

            migrationBuilder.Sql("""
                UPDATE "SubscriptionNotifications" SET "ClinicId" = s."ClinicId"
                FROM "Subscriptions" s WHERE s."Id" = "SubscriptionNotifications"."SubscriptionId";
                """);

            // --- Custom SQL: Invoice number sequence ---
            migrationBuilder.Sql("""
                CREATE SEQUENCE IF NOT EXISTS invoice_number_seq START WITH 1 INCREMENT BY 1;
                SELECT setval('invoice_number_seq', GREATEST(1, COALESCE(
                    (SELECT MAX(CAST(NULLIF(SPLIT_PART("InvoiceNumber", '-', 3), '') AS INTEGER))
                     FROM "Invoices" WHERE "InvoiceNumber" LIKE 'INV-%'), 1)));
                """);

            // --- Custom SQL: Fix RLS policy on TreatmentStages ---
            migrationBuilder.Sql("""
                DROP POLICY IF EXISTS "TreatmentStages_tenant_isolation" ON "TreatmentStages";
                CREATE POLICY "TreatmentStages_tenant_isolation" ON "TreatmentStages"
                    USING ("TenantId"::text = current_setting('app.current_tenant_id', true)::text
                           OR current_setting('app.current_tenant_id', true) = '')
                    WITH CHECK ("TenantId"::text = current_setting('app.current_tenant_id', true)::text
                                OR current_setting('app.current_tenant_id', true) = '');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // --- Reverse custom SQL ---
            migrationBuilder.Sql("""
                DROP POLICY IF EXISTS "TreatmentStages_tenant_isolation" ON "TreatmentStages";
                CREATE POLICY "TreatmentStages_tenant_isolation" ON "TreatmentStages"
                    USING (true) WITH CHECK (true);
                """);

            migrationBuilder.Sql("""DROP SEQUENCE IF EXISTS invoice_number_seq;""");

            migrationBuilder.DropForeignKey(
                name: "FK_TreatmentPhases_Treatments_TreatmentId",
                table: "TreatmentPhases");

            migrationBuilder.DropIndex(
                name: "IX_Treatment_ClinicId_IsDeleted",
                table: "Treatments");

            migrationBuilder.DropIndex(
                name: "IX_TreatmentPhases_TreatmentId",
                table: "TreatmentPhases");

            migrationBuilder.DropIndex(
                name: "IX_OtpCode_Lookup",
                table: "OtpCodes");

            migrationBuilder.DropIndex(
                name: "IX_Modality_ClinicId_IsDeleted",
                table: "Modalities");

            migrationBuilder.DropIndex(
                name: "IX_Exercise_ClinicId_IsDeleted",
                table: "Exercises");

            migrationBuilder.DropIndex(
                name: "IX_Device_ClinicId_IsDeleted",
                table: "Devices");

            migrationBuilder.DropIndex(
                name: "IX_Assessment_ClinicId_IsDeleted",
                table: "Assessments");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationUser_ResetToken",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TreatmentId",
                table: "TreatmentPhases");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "SubscriptionNotifications");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "SubscriptionFeatureUsages");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "SubscriptionAddOns");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Invoices");

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 3, 0, 12, 4, 241, DateTimeKind.Utc).AddTicks(5408));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 3, 0, 12, 4, 241, DateTimeKind.Utc).AddTicks(6292));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000003"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 3, 0, 12, 4, 241, DateTimeKind.Utc).AddTicks(6297));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000004"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 3, 0, 12, 4, 241, DateTimeKind.Utc).AddTicks(6299));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000005"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 3, 0, 12, 4, 241, DateTimeKind.Utc).AddTicks(6301));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000006"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 3, 0, 12, 4, 241, DateTimeKind.Utc).AddTicks(6303));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000007"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 3, 0, 12, 4, 241, DateTimeKind.Utc).AddTicks(6306));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000008"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 3, 0, 12, 4, 241, DateTimeKind.Utc).AddTicks(6316));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000009"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 3, 0, 12, 4, 241, DateTimeKind.Utc).AddTicks(6318));
        }
    }
}
