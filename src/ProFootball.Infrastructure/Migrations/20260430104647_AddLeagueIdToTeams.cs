using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProFootball.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLeagueIdToTeams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "league_id",
                table: "teams",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_teams_league_id",
                table: "teams",
                column: "league_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_teams_league_id",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "league_id",
                table: "teams");
        }
    }
}
