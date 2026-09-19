using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProFootball.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialMvpSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "countries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_countries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "football_clubs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    FoundedYear = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_football_clubs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "players",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    PlayerApiId = table.Column<int>(type: "integer", nullable: false),
                    PlayerFifaApiId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Birthday = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Height = table.Column<int>(type: "integer", nullable: true),
                    Weight = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_players", x => x.Id);
                    table.UniqueConstraint("AK_players_PlayerApiId", x => x.PlayerApiId);
                });

            migrationBuilder.CreateTable(
                name: "teams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    TeamApiId = table.Column<int>(type: "integer", nullable: false),
                    TeamFifaApiId = table.Column<int>(type: "integer", nullable: true),
                    LongName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ShortName = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams", x => x.Id);
                    table.UniqueConstraint("AK_teams_TeamApiId", x => x.TeamApiId);
                });

            migrationBuilder.CreateTable(
                name: "leagues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    CountryId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leagues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_leagues_countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "player_attributes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    PlayerApiId = table.Column<int>(type: "integer", nullable: false),
                    PlayerFifaApiId = table.Column<int>(type: "integer", nullable: true),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OverallRating = table.Column<int>(type: "integer", nullable: true),
                    Potential = table.Column<int>(type: "integer", nullable: true),
                    PreferredFoot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    AttackingWorkRate = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    DefensiveWorkRate = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_attributes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_player_attributes_players_PlayerApiId",
                        column: x => x.PlayerApiId,
                        principalTable: "players",
                        principalColumn: "PlayerApiId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "team_attributes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    TeamApiId = table.Column<int>(type: "integer", nullable: false),
                    TeamFifaApiId = table.Column<int>(type: "integer", nullable: true),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BuildUpPlaySpeed = table.Column<int>(type: "integer", nullable: true),
                    BuildUpPlayPassing = table.Column<int>(type: "integer", nullable: true),
                    ChanceCreationPassing = table.Column<int>(type: "integer", nullable: true),
                    DefencePressure = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_attributes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_team_attributes_teams_TeamApiId",
                        column: x => x.TeamApiId,
                        principalTable: "teams",
                        principalColumn: "TeamApiId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "matches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    CountryId = table.Column<int>(type: "integer", nullable: false),
                    LeagueId = table.Column<int>(type: "integer", nullable: false),
                    Season = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MatchApiId = table.Column<int>(type: "integer", nullable: false),
                    HomeTeamApiId = table.Column<int>(type: "integer", nullable: false),
                    AwayTeamApiId = table.Column<int>(type: "integer", nullable: false),
                    HomeTeamGoal = table.Column<int>(type: "integer", nullable: true),
                    AwayTeamGoal = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_matches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_matches_countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_matches_leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_countries_Name",
                table: "countries",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_leagues_CountryId",
                table: "leagues",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_leagues_Name",
                table: "leagues",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_matches_AwayTeamApiId",
                table: "matches",
                column: "AwayTeamApiId");

            migrationBuilder.CreateIndex(
                name: "IX_matches_CountryId",
                table: "matches",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_matches_Date",
                table: "matches",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_matches_HomeTeamApiId",
                table: "matches",
                column: "HomeTeamApiId");

            migrationBuilder.CreateIndex(
                name: "IX_matches_LeagueId",
                table: "matches",
                column: "LeagueId");

            migrationBuilder.CreateIndex(
                name: "IX_matches_MatchApiId",
                table: "matches",
                column: "MatchApiId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_matches_Season",
                table: "matches",
                column: "Season");

            migrationBuilder.CreateIndex(
                name: "IX_player_attributes_Date",
                table: "player_attributes",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_player_attributes_OverallRating",
                table: "player_attributes",
                column: "OverallRating");

            migrationBuilder.CreateIndex(
                name: "IX_player_attributes_PlayerApiId",
                table: "player_attributes",
                column: "PlayerApiId");

            migrationBuilder.CreateIndex(
                name: "IX_player_attributes_PlayerApiId_Date",
                table: "player_attributes",
                columns: new[] { "PlayerApiId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_player_attributes_Potential",
                table: "player_attributes",
                column: "Potential");

            migrationBuilder.CreateIndex(
                name: "IX_players_Name",
                table: "players",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_players_PlayerApiId",
                table: "players",
                column: "PlayerApiId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_players_PlayerFifaApiId",
                table: "players",
                column: "PlayerFifaApiId");

            migrationBuilder.CreateIndex(
                name: "IX_team_attributes_Date",
                table: "team_attributes",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_team_attributes_TeamApiId",
                table: "team_attributes",
                column: "TeamApiId");

            migrationBuilder.CreateIndex(
                name: "IX_team_attributes_TeamApiId_Date",
                table: "team_attributes",
                columns: new[] { "TeamApiId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_teams_LongName",
                table: "teams",
                column: "LongName");

            migrationBuilder.CreateIndex(
                name: "IX_teams_TeamApiId",
                table: "teams",
                column: "TeamApiId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_teams_TeamFifaApiId",
                table: "teams",
                column: "TeamFifaApiId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "football_clubs");

            migrationBuilder.DropTable(
                name: "matches");

            migrationBuilder.DropTable(
                name: "player_attributes");

            migrationBuilder.DropTable(
                name: "team_attributes");

            migrationBuilder.DropTable(
                name: "leagues");

            migrationBuilder.DropTable(
                name: "players");

            migrationBuilder.DropTable(
                name: "teams");

            migrationBuilder.DropTable(
                name: "countries");
        }
    }
}
