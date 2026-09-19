using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProFootball.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveApiIdsAndFootballClubs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_player_attributes_players_PlayerApiId",
                table: "player_attributes");

            migrationBuilder.DropForeignKey(
                name: "FK_team_attributes_teams_TeamApiId",
                table: "team_attributes");

            migrationBuilder.DropTable(
                name: "football_clubs");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_teams_TeamApiId",
                table: "teams");

            migrationBuilder.DropIndex(
                name: "IX_teams_TeamFifaApiId",
                table: "teams");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_players_PlayerApiId",
                table: "players");

            migrationBuilder.DropIndex(
                name: "IX_players_PlayerFifaApiId",
                table: "players");

            migrationBuilder.DropIndex(
                name: "IX_matches_AwayTeamApiId",
                table: "matches");

            migrationBuilder.DropIndex(
                name: "IX_matches_MatchApiId",
                table: "matches");

            migrationBuilder.RenameColumn(
                name: "TeamApiId",
                table: "team_attributes",
                newName: "TeamId");

            migrationBuilder.RenameIndex(
                name: "IX_team_attributes_TeamApiId_Date",
                table: "team_attributes",
                newName: "IX_team_attributes_TeamId_Date");

            migrationBuilder.RenameIndex(
                name: "IX_team_attributes_TeamApiId",
                table: "team_attributes",
                newName: "IX_team_attributes_TeamId");

            migrationBuilder.RenameColumn(
                name: "PlayerApiId",
                table: "player_attributes",
                newName: "PlayerId");

            migrationBuilder.RenameIndex(
                name: "IX_player_attributes_PlayerApiId_Date",
                table: "player_attributes",
                newName: "IX_player_attributes_PlayerId_Date");

            migrationBuilder.RenameIndex(
                name: "IX_player_attributes_PlayerApiId",
                table: "player_attributes",
                newName: "IX_player_attributes_PlayerId");

            migrationBuilder.AddColumn<int>(
                name: "AwayTeamId",
                table: "matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomeTeamId",
                table: "matches",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE player_attributes pa
                SET "PlayerId" = p."Id"
                FROM players p
                WHERE pa."PlayerId" = p."PlayerApiId";
                """);

            migrationBuilder.Sql(
                """
                UPDATE team_attributes ta
                SET "TeamId" = t."Id"
                FROM teams t
                WHERE ta."TeamId" = t."TeamApiId";
                """);

            migrationBuilder.Sql(
                """
                UPDATE matches m
                SET "HomeTeamId" = t."Id"
                FROM teams t
                WHERE m."HomeTeamApiId" = t."TeamApiId";
                """);

            migrationBuilder.Sql(
                """
                UPDATE matches m
                SET "AwayTeamId" = t."Id"
                FROM teams t
                WHERE m."AwayTeamApiId" = t."TeamApiId";
                """);

            migrationBuilder.Sql(
                """
                UPDATE matches
                SET
                    "HomeTeamId" = COALESCE("HomeTeamId", "HomeTeamApiId"),
                    "AwayTeamId" = COALESCE("AwayTeamId", "AwayTeamApiId");
                """);

            migrationBuilder.AlterColumn<int>(
                name: "AwayTeamId",
                table: "matches",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "HomeTeamId",
                table: "matches",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "TeamApiId",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "TeamFifaApiId",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "TeamFifaApiId",
                table: "team_attributes");

            migrationBuilder.DropColumn(
                name: "PlayerApiId",
                table: "players");

            migrationBuilder.DropColumn(
                name: "PlayerFifaApiId",
                table: "players");

            migrationBuilder.DropColumn(
                name: "PlayerFifaApiId",
                table: "player_attributes");

            migrationBuilder.DropColumn(
                name: "AwayTeamApiId",
                table: "matches");

            migrationBuilder.RenameColumn(
                name: "MatchApiId",
                table: "matches",
                newName: "_DeprecatedMatchApiId");

            migrationBuilder.DropColumn(
                name: "_DeprecatedMatchApiId",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "HomeTeamApiId",
                table: "matches");

            migrationBuilder.CreateIndex(
                name: "IX_matches_HomeTeamId",
                table: "matches",
                column: "HomeTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_matches_AwayTeamId",
                table: "matches",
                column: "AwayTeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_player_attributes_players_PlayerId",
                table: "player_attributes",
                column: "PlayerId",
                principalTable: "players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_team_attributes_teams_TeamId",
                table: "team_attributes",
                column: "TeamId",
                principalTable: "teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_player_attributes_players_PlayerId",
                table: "player_attributes");

            migrationBuilder.DropForeignKey(
                name: "FK_team_attributes_teams_TeamId",
                table: "team_attributes");

            migrationBuilder.DropIndex(
                name: "IX_matches_HomeTeamId",
                table: "matches");

            migrationBuilder.DropIndex(
                name: "IX_matches_AwayTeamId",
                table: "matches");

            migrationBuilder.RenameColumn(
                name: "TeamId",
                table: "team_attributes",
                newName: "TeamApiId");

            migrationBuilder.RenameIndex(
                name: "IX_team_attributes_TeamId_Date",
                table: "team_attributes",
                newName: "IX_team_attributes_TeamApiId_Date");

            migrationBuilder.RenameIndex(
                name: "IX_team_attributes_TeamId",
                table: "team_attributes",
                newName: "IX_team_attributes_TeamApiId");

            migrationBuilder.RenameColumn(
                name: "PlayerId",
                table: "player_attributes",
                newName: "PlayerApiId");

            migrationBuilder.RenameIndex(
                name: "IX_player_attributes_PlayerId_Date",
                table: "player_attributes",
                newName: "IX_player_attributes_PlayerApiId_Date");

            migrationBuilder.RenameIndex(
                name: "IX_player_attributes_PlayerId",
                table: "player_attributes",
                newName: "IX_player_attributes_PlayerApiId");

            migrationBuilder.AddColumn<int>(
                name: "MatchApiId",
                table: "matches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HomeTeamApiId",
                table: "matches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TeamApiId",
                table: "teams",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TeamFifaApiId",
                table: "teams",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TeamFifaApiId",
                table: "team_attributes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlayerApiId",
                table: "players",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PlayerFifaApiId",
                table: "players",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlayerFifaApiId",
                table: "player_attributes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AwayTeamApiId",
                table: "matches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE players SET "PlayerApiId" = "Id";
                """);

            migrationBuilder.Sql(
                """
                UPDATE teams SET "TeamApiId" = "Id";
                """);

            migrationBuilder.Sql(
                """
                UPDATE player_attributes pa
                SET "PlayerApiId" = p."PlayerApiId"
                FROM players p
                WHERE pa."PlayerApiId" = p."Id";
                """);

            migrationBuilder.Sql(
                """
                UPDATE team_attributes ta
                SET "TeamApiId" = t."TeamApiId"
                FROM teams t
                WHERE ta."TeamApiId" = t."Id";
                """);

            migrationBuilder.Sql(
                """
                UPDATE matches
                SET
                    "MatchApiId" = "Id",
                    "HomeTeamApiId" = "HomeTeamId",
                    "AwayTeamApiId" = "AwayTeamId";
                """);

            migrationBuilder.DropColumn(
                name: "AwayTeamId",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "HomeTeamId",
                table: "matches");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_teams_TeamApiId",
                table: "teams",
                column: "TeamApiId");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_players_PlayerApiId",
                table: "players",
                column: "PlayerApiId");

            migrationBuilder.CreateTable(
                name: "football_clubs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FoundedYear = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_football_clubs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_teams_TeamFifaApiId",
                table: "teams",
                column: "TeamFifaApiId");

            migrationBuilder.CreateIndex(
                name: "IX_players_PlayerFifaApiId",
                table: "players",
                column: "PlayerFifaApiId");

            migrationBuilder.CreateIndex(
                name: "IX_matches_AwayTeamApiId",
                table: "matches",
                column: "AwayTeamApiId");

            migrationBuilder.CreateIndex(
                name: "IX_matches_HomeTeamApiId",
                table: "matches",
                column: "HomeTeamApiId");

            migrationBuilder.CreateIndex(
                name: "IX_matches_MatchApiId",
                table: "matches",
                column: "MatchApiId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_player_attributes_players_PlayerApiId",
                table: "player_attributes",
                column: "PlayerApiId",
                principalTable: "players",
                principalColumn: "PlayerApiId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_team_attributes_teams_TeamApiId",
                table: "team_attributes",
                column: "TeamApiId",
                principalTable: "teams",
                principalColumn: "TeamApiId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
