using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ProFootball.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSyntheticAnalyticsEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "analytics_fact_daily",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateKey = table.Column<DateOnly>(type: "date", nullable: false),
                    Season = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    LeagueId = table.Column<int>(type: "integer", nullable: false),
                    MetricKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MetricValue = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
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
                    GeneratorVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Seed = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Profile = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Mode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_generation_runs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "match_events",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MatchId = table.Column<int>(type: "integer", nullable: false),
                    Minute = table.Column<short>(type: "smallint", nullable: false),
                    EventType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    AssistPlayerId = table.Column<int>(type: "integer", nullable: true),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_match_events", x => x.Id);
                    table.CheckConstraint("CK_match_events_minute_range", "\"Minute\" >= 1 AND \"Minute\" <= 130");
                    table.ForeignKey(
                        name: "FK_match_events_matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_match_events_players_AssistPlayerId",
                        column: x => x.AssistPlayerId,
                        principalTable: "players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_match_events_players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_match_events_teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "player_match_stats",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MatchId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    Minutes = table.Column<short>(type: "smallint", nullable: false),
                    Shots = table.Column<short>(type: "smallint", nullable: false),
                    Passes = table.Column<short>(type: "smallint", nullable: false),
                    Tackles = table.Column<short>(type: "smallint", nullable: false),
                    Goals = table.Column<short>(type: "smallint", nullable: false),
                    Assists = table.Column<short>(type: "smallint", nullable: false),
                    Xg = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_match_stats", x => x.Id);
                    table.CheckConstraint("CK_player_match_stats_assists_nonnegative", "\"Assists\" >= 0");
                    table.CheckConstraint("CK_player_match_stats_goals_nonnegative", "\"Goals\" >= 0");
                    table.CheckConstraint("CK_player_match_stats_minutes_nonnegative", "\"Minutes\" >= 0");
                    table.CheckConstraint("CK_player_match_stats_passes_nonnegative", "\"Passes\" >= 0");
                    table.CheckConstraint("CK_player_match_stats_shots_nonnegative", "\"Shots\" >= 0");
                    table.CheckConstraint("CK_player_match_stats_tackles_nonnegative", "\"Tackles\" >= 0");
                    table.CheckConstraint("CK_player_match_stats_xg_nonnegative", "\"Xg\" >= 0");
                    table.ForeignKey(
                        name: "FK_player_match_stats_matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_player_match_stats_players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_player_match_stats_teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "team_season_stats",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Season = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    LeagueId = table.Column<int>(type: "integer", nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    Matches = table.Column<int>(type: "integer", nullable: false),
                    Wins = table.Column<int>(type: "integer", nullable: false),
                    Draws = table.Column<int>(type: "integer", nullable: false),
                    Losses = table.Column<int>(type: "integer", nullable: false),
                    GoalsFor = table.Column<int>(type: "integer", nullable: false),
                    GoalsAgainst = table.Column<int>(type: "integer", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_season_stats", x => x.Id);
                    table.CheckConstraint("CK_team_season_stats_draws_nonnegative", "\"Draws\" >= 0");
                    table.CheckConstraint("CK_team_season_stats_goals_against_nonnegative", "\"GoalsAgainst\" >= 0");
                    table.CheckConstraint("CK_team_season_stats_goals_for_nonnegative", "\"GoalsFor\" >= 0");
                    table.CheckConstraint("CK_team_season_stats_losses_nonnegative", "\"Losses\" >= 0");
                    table.CheckConstraint("CK_team_season_stats_matches_nonnegative", "\"Matches\" >= 0");
                    table.CheckConstraint("CK_team_season_stats_points_nonnegative", "\"Points\" >= 0");
                    table.CheckConstraint("CK_team_season_stats_wins_nonnegative", "\"Wins\" >= 0");
                    table.ForeignKey(
                        name: "FK_team_season_stats_leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_team_season_stats_teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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

            migrationBuilder.CreateIndex(
                name: "IX_match_events_AssistPlayerId",
                table: "match_events",
                column: "AssistPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_match_events_EventType",
                table: "match_events",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_match_events_MatchId",
                table: "match_events",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_match_events_MatchId_Minute",
                table: "match_events",
                columns: new[] { "MatchId", "Minute" });

            migrationBuilder.CreateIndex(
                name: "IX_match_events_PlayerId",
                table: "match_events",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_match_events_TeamId",
                table: "match_events",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_player_match_stats_MatchId",
                table: "player_match_stats",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_player_match_stats_MatchId_PlayerId",
                table: "player_match_stats",
                columns: new[] { "MatchId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_player_match_stats_PlayerId",
                table: "player_match_stats",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_player_match_stats_TeamId",
                table: "player_match_stats",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_team_season_stats_LeagueId",
                table: "team_season_stats",
                column: "LeagueId");

            migrationBuilder.CreateIndex(
                name: "IX_team_season_stats_Season_LeagueId_TeamId",
                table: "team_season_stats",
                columns: new[] { "Season", "LeagueId", "TeamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_team_season_stats_TeamId",
                table: "team_season_stats",
                column: "TeamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "analytics_fact_daily");

            migrationBuilder.DropTable(
                name: "generation_runs");

            migrationBuilder.DropTable(
                name: "match_events");

            migrationBuilder.DropTable(
                name: "player_match_stats");

            migrationBuilder.DropTable(
                name: "team_season_stats");
        }
    }
}
