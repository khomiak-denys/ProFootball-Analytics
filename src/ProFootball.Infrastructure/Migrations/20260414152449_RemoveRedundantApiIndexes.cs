using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProFootball.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRedundantApiIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_teams_TeamApiId",
                table: "teams");

            migrationBuilder.DropIndex(
                name: "IX_players_PlayerApiId",
                table: "players");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_teams_TeamApiId",
                table: "teams",
                column: "TeamApiId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_players_PlayerApiId",
                table: "players",
                column: "PlayerApiId",
                unique: true);
        }
    }
}
