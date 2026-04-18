using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rehably.Infrastructure.Migrations.ApplicationDb
{
    /// <inheritdoc />
    public partial class AddTreatmentStages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TreatmentStages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BodyRegionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NameArabic = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    MinWeeks = table.Column<int>(type: "integer", nullable: true),
                    MaxWeeks = table.Column<int>(type: "integer", nullable: true),
                    MinSessions = table.Column<int>(type: "integer", nullable: true),
                    MaxSessions = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentStages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TreatmentStages_BodyRegionCategories_BodyRegionId",
                        column: x => x.BodyRegionId,
                        principalTable: "BodyRegionCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentStages_BodyRegionId",
                table: "TreatmentStages",
                column: "BodyRegionId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentStages_IsDeleted",
                table: "TreatmentStages",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentStages_TenantId",
                table: "TreatmentStages",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentStages_TenantId_Code",
                table: "TreatmentStages",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.Sql(@"
                ALTER TABLE ""TreatmentStages"" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE ""TreatmentStages"" FORCE ROW LEVEL SECURITY;
                CREATE POLICY ""TreatmentStages_tenant_isolation"" ON ""TreatmentStages""
                    USING (true)
                    WITH CHECK (true);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP POLICY IF EXISTS ""TreatmentStages_tenant_isolation"" ON ""TreatmentStages"";
                ALTER TABLE ""TreatmentStages"" DISABLE ROW LEVEL SECURITY;
            ");

            migrationBuilder.DropTable(
                name: "TreatmentStages");

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 23, 2, 58, 796, DateTimeKind.Utc).AddTicks(6426));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 23, 2, 58, 796, DateTimeKind.Utc).AddTicks(7299));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000003"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 23, 2, 58, 796, DateTimeKind.Utc).AddTicks(7307));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000004"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 23, 2, 58, 796, DateTimeKind.Utc).AddTicks(7309));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000005"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 23, 2, 58, 796, DateTimeKind.Utc).AddTicks(7312));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000006"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 23, 2, 58, 796, DateTimeKind.Utc).AddTicks(7315));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000007"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 23, 2, 58, 796, DateTimeKind.Utc).AddTicks(7317));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000008"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 23, 2, 58, 796, DateTimeKind.Utc).AddTicks(7334));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000009"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 2, 23, 2, 58, 796, DateTimeKind.Utc).AddTicks(7337));
        }
    }
}
