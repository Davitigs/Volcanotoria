using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HazardDashboard.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "SeismicEvents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Magnitude = table.Column<double>(type: "double precision", nullable: false),
                    Location = table.Column<Point>(type: "geography (point, 4326)", nullable: false),
                    DepthKm = table.Column<double>(type: "double precision", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Place = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    IngestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeismicEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Volcanoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<Point>(type: "geography (point, 4326)", nullable: false),
                    ElevationMeters = table.Column<double>(type: "double precision", nullable: false),
                    Country = table.Column<string>(type: "text", nullable: false),
                    LastEruptionYear = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Volcanoes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SeismicEvents_Location",
                table: "SeismicEvents",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_SeismicEvents_OccurredAt",
                table: "SeismicEvents",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_Volcanoes_Location",
                table: "Volcanoes",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "gist");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SeismicEvents");

            migrationBuilder.DropTable(
                name: "Volcanoes");
        }
    }
}
