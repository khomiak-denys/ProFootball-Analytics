using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ProFootball.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveGenerationRunsAndAnalyticsFactDaily : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("drop procedure if exists public.sp_rebuild_analytics_fact_daily(date, date, text, int);");

            migrationBuilder.DropTable(
                name: "analytics_fact_daily");

            migrationBuilder.DropTable(
                name: "generation_runs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "analytics_fact_daily",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateKey = table.Column<DateOnly>(type: "date", nullable: false),
                    LeagueId = table.Column<int>(type: "integer", nullable: false),
                    MetricKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MetricValue = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    Season = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_analytics_fact_daily", x => x.Id);
                    table.CheckConstraint("CK_analytics_fact_daily_metric_value_nonnegative", "\"MetricValue\" >= 0");
                    table.ForeignKey(
                        name: "FK_analytics_fact_daily_leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "generation_runs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FinishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GeneratorVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Mode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Profile = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Seed = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_generation_runs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_analytics_fact_daily_DateKey_Season_LeagueId_MetricKey",
                table: "analytics_fact_daily",
                columns: new[] { "DateKey", "Season", "LeagueId", "MetricKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_analytics_fact_daily_LeagueId",
                table: "analytics_fact_daily",
                column: "LeagueId");

            migrationBuilder.CreateIndex(
                name: "IX_analytics_fact_daily_MetricKey",
                table: "analytics_fact_daily",
                column: "MetricKey");

            migrationBuilder.CreateIndex(
                name: "IX_generation_runs_Profile_Mode_StartedAtUtc",
                table: "generation_runs",
                columns: new[] { "Profile", "Mode", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_generation_runs_StartedAtUtc",
                table: "generation_runs",
                column: "StartedAtUtc");
        }
    }
}
