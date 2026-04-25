using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rehably.Infrastructure.Migrations.ApplicationDb
{
    /// <inheritdoc />
    public partial class AddClinicSpeciality : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClinicSpecialities",
                columns: table => new
                {
                    ClinicId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecialityId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicSpecialities", x => new { x.ClinicId, x.SpecialityId });
                    table.ForeignKey(
                        name: "FK_ClinicSpecialities_Specialities_SpecialityId",
                        column: x => x.SpecialityId,
                        principalTable: "Specialities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 25, 11, 11, 0, 254, DateTimeKind.Utc).AddTicks(1901));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 25, 11, 11, 0, 254, DateTimeKind.Utc).AddTicks(3790));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000003"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 25, 11, 11, 0, 254, DateTimeKind.Utc).AddTicks(3807));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000004"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 25, 11, 11, 0, 254, DateTimeKind.Utc).AddTicks(3810));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000005"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 25, 11, 11, 0, 254, DateTimeKind.Utc).AddTicks(3812));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000006"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 25, 11, 11, 0, 254, DateTimeKind.Utc).AddTicks(3814));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000007"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 25, 11, 11, 0, 254, DateTimeKind.Utc).AddTicks(3816));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000008"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 25, 11, 11, 0, 254, DateTimeKind.Utc).AddTicks(3818));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000009"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 25, 11, 11, 0, 254, DateTimeKind.Utc).AddTicks(3821));

            migrationBuilder.CreateIndex(
                name: "IX_ClinicSpecialities_SpecialityId",
                table: "ClinicSpecialities",
                column: "SpecialityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicSpecialities");

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 25, 10, 57, 4, 331, DateTimeKind.Utc).AddTicks(4981));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 25, 10, 57, 4, 331, DateTimeKind.Utc).AddTicks(6077));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000003"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 25, 10, 57, 4, 331, DateTimeKind.Utc).AddTicks(6081));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000004"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 25, 10, 57, 4, 331, DateTimeKind.Utc).AddTicks(6092));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000005"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 25, 10, 57, 4, 331, DateTimeKind.Utc).AddTicks(6094));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000006"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 25, 10, 57, 4, 331, DateTimeKind.Utc).AddTicks(6096));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000007"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 25, 10, 57, 4, 331, DateTimeKind.Utc).AddTicks(6098));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000008"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 25, 10, 57, 4, 331, DateTimeKind.Utc).AddTicks(6100));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000009"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 25, 10, 57, 4, 331, DateTimeKind.Utc).AddTicks(6102));
        }
    }
}
