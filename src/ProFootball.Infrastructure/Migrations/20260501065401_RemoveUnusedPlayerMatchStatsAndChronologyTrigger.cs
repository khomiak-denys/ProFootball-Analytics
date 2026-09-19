using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ProFootball.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedPlayerMatchStatsAndChronologyTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                drop trigger if exists trg_player_attributes_chronology_guard on public.player_attributes;
                drop function if exists public.fn_trg_player_attributes_chronology_guard();
                """);

            migrationBuilder.DropTable(
                name: "player_match_stats");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "player_match_stats",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Assists = table.Column<short>(type: "smallint", nullable: false),
                    Goals = table.Column<short>(type: "smallint", nullable: false),
                    MatchId = table.Column<int>(type: "integer", nullable: false),
                    Minutes = table.Column<short>(type: "smallint", nullable: false),
                    Passes = table.Column<short>(type: "smallint", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    Shots = table.Column<short>(type: "smallint", nullable: false),
                    Tackles = table.Column<short>(type: "smallint", nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
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

            migrationBuilder.Sql(
                """
                create function public.fn_trg_player_attributes_chronology_guard()
                returns trigger
                language plpgsql
                as $$
                begin
                  if exists (
                    select 1
                    from public.player_attributes pa
                    where pa."PlayerId" = new."PlayerId"
                      and pa."Date" > new."Date"
                      and pa."Id" <> coalesce(new."Id", -1)
                  ) then
                    raise exception 'Player attribute date cannot be earlier than existing newer snapshot';
                  end if;
                  return new;
                end;
                $$;
                create trigger trg_player_attributes_chronology_guard
                before insert or update on public.player_attributes
                for each row execute function public.fn_trg_player_attributes_chronology_guard();
                """);
        }
    }
}
