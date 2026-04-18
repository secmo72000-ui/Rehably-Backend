using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rehably.Infrastructure.Migrations.ApplicationDb
{
    /// <inheritdoc />
    public partial class AddPackageTierIsPopularAndFeatureIconKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPopular",
                table: "Packages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Tier",
                table: "Packages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "IconKey",
                table: "Features",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 3, 0, 9, 30, 754, DateTimeKind.Utc).AddTicks(6593));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 3, 0, 9, 30, 754, DateTimeKind.Utc).AddTicks(7553));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000003"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 3, 0, 9, 30, 754, DateTimeKind.Utc).AddTicks(7557));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000004"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 3, 0, 9, 30, 754, DateTimeKind.Utc).AddTicks(7559));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000005"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 3, 0, 9, 30, 754, DateTimeKind.Utc).AddTicks(7561));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000006"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 3, 0, 9, 30, 754, DateTimeKind.Utc).AddTicks(7563));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000007"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 3, 0, 9, 30, 754, DateTimeKind.Utc).AddTicks(7565));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000008"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 3, 0, 9, 30, 754, DateTimeKind.Utc).AddTicks(7573));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000009"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 3, 0, 9, 30, 754, DateTimeKind.Utc).AddTicks(7576));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPopular",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "Tier",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "IconKey",
                table: "Features");

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
