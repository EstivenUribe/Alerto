using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Alerto.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AlertoDbContext))]
    [Migration("20260512190000_AddAlertSoftDelete")]
    public partial class AddAlertSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "alerto",
                table: "alerts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                schema: "alerto",
                table: "alerts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletionReason",
                schema: "alerto",
                table: "alerts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "alerto",
                table: "alerts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_alerts_IsDeleted",
                schema: "alerto",
                table: "alerts",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_alerts_IsDeleted",
                schema: "alerto",
                table: "alerts");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "alerto",
                table: "alerts");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "alerto",
                table: "alerts");

            migrationBuilder.DropColumn(
                name: "DeletionReason",
                schema: "alerto",
                table: "alerts");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "alerto",
                table: "alerts");
        }
    }
}
