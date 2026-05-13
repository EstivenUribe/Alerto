using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Alerto.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCitizenConfirmations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "alert_citizen_confirmations",
                schema: "alerto",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AlertId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfirmedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alert_citizen_confirmations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_alert_citizen_confirmations_AlertId",
                schema: "alerto",
                table: "alert_citizen_confirmations",
                column: "AlertId");

            migrationBuilder.CreateIndex(
                name: "IX_alert_citizen_confirmations_AlertId_ConfirmedByUserId",
                schema: "alerto",
                table: "alert_citizen_confirmations",
                columns: new[] { "AlertId", "ConfirmedByUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alert_citizen_confirmations",
                schema: "alerto");
        }
    }
}
