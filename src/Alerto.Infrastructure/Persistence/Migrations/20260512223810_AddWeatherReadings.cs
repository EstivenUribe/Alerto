using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Alerto.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWeatherReadings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "weather_readings",
                schema: "alerto",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    Longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    PrecipitationMmPerHour = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    PrecipitationProbabilityPercent = table.Column<int>(type: "integer", nullable: false),
                    WeatherCode = table.Column<int>(type: "integer", nullable: false),
                    RiskLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    HourlyForecastJson = table.Column<string>(type: "jsonb", nullable: false),
                    AutoAlertCreated = table.Column<bool>(type: "boolean", nullable: false),
                    AutoAlertId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weather_readings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_weather_readings_CreatedAtUtc",
                schema: "alerto",
                table: "weather_readings",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_weather_readings_Latitude_Longitude_CreatedAtUtc",
                schema: "alerto",
                table: "weather_readings",
                columns: new[] { "Latitude", "Longitude", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "weather_readings",
                schema: "alerto");
        }
    }
}
