using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Alerto.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_audit_trails_ActorId",
                schema: "alerto",
                table: "audit_trails",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_audit_trails_EntityId",
                schema: "alerto",
                table: "audit_trails",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_alerts_CreatedAtUtc",
                schema: "alerto",
                table: "alerts",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_alerts_CreatedByUserId",
                schema: "alerto",
                table: "alerts",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_alerts_GeofenceId",
                schema: "alerto",
                table: "alerts",
                column: "GeofenceId");

            migrationBuilder.CreateIndex(
                name: "IX_alerts_Status",
                schema: "alerto",
                table: "alerts",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_audit_trails_ActorId",
                schema: "alerto",
                table: "audit_trails");

            migrationBuilder.DropIndex(
                name: "IX_audit_trails_EntityId",
                schema: "alerto",
                table: "audit_trails");

            migrationBuilder.DropIndex(
                name: "IX_alerts_CreatedAtUtc",
                schema: "alerto",
                table: "alerts");

            migrationBuilder.DropIndex(
                name: "IX_alerts_CreatedByUserId",
                schema: "alerto",
                table: "alerts");

            migrationBuilder.DropIndex(
                name: "IX_alerts_GeofenceId",
                schema: "alerto",
                table: "alerts");

            migrationBuilder.DropIndex(
                name: "IX_alerts_Status",
                schema: "alerto",
                table: "alerts");
        }
    }
}
