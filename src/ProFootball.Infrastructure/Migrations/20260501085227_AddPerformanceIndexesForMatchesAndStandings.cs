using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProFootball.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexesForMatchesAndStandings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_team_season_stats_LeagueId_Season_Points",
                table: "team_season_stats",
                columns: new[] { "LeagueId", "Season", "Points" });

            migrationBuilder.CreateIndex(
                name: "IX_matches_LeagueId_Season_AwayTeamId",
                table: "matches",
                columns: new[] { "LeagueId", "Season", "AwayTeamId" });

            migrationBuilder.CreateIndex(
                name: "IX_matches_LeagueId_Season_Date",
                table: "matches",
                columns: new[] { "LeagueId", "Season", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_matches_LeagueId_Season_HomeTeamId",
                table: "matches",
                columns: new[] { "LeagueId", "Season", "HomeTeamId" });

            migrationBuilder.CreateIndex(
                name: "IX_match_events_MatchId_TeamId_EventType",
                table: "match_events",
                columns: new[] { "MatchId", "TeamId", "EventType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_team_season_stats_LeagueId_Season_Points",
                table: "team_season_stats");

            migrationBuilder.DropIndex(
                name: "IX_matches_LeagueId_Season_AwayTeamId",
                table: "matches");

            migrationBuilder.DropIndex(
                name: "IX_matches_LeagueId_Season_Date",
                table: "matches");

            migrationBuilder.DropIndex(
                name: "IX_matches_LeagueId_Season_HomeTeamId",
                table: "matches");

            migrationBuilder.DropIndex(
                name: "IX_match_events_MatchId_TeamId_EventType",
                table: "match_events");
        }
    }
}
