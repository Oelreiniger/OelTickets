using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OelTicketsBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedAndDeletedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "statuses",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "statuses",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "projects",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "projects",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "project_memberships",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "project_memberships",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "statuses");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "statuses");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "project_memberships");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "project_memberships");
        }
    }
}
