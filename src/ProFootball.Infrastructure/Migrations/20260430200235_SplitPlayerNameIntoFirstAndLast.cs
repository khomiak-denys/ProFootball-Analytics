using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProFootball.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitPlayerNameIntoFirstAndLast : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "players",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "players",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                update players
                set "FirstName" = trim(split_part("Name", ' ', 1)),
                    "LastName" = trim(
                        case
                            when strpos(trim("Name"), ' ') > 0
                                then substr(trim("Name"), strpos(trim("Name"), ' ') + 1)
                            else trim("Name")
                        end
                    );
                """);

            migrationBuilder.Sql("""
                update players
                set "FirstName" = "LastName"
                where "FirstName" = '';
                """);

            migrationBuilder.Sql("drop view if exists public.vw_player_latest_attributes;");

            migrationBuilder.DropIndex(
                name: "IX_players_Name",
                table: "players");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "players");

            migrationBuilder.CreateIndex(
                name: "IX_players_LastName_FirstName",
                table: "players",
                columns: new[] { "LastName", "FirstName" });

            migrationBuilder.Sql("""
                create or replace view public.vw_player_latest_attributes as
                select distinct on (pa."PlayerId")
                    pa."PlayerId" as player_id,
                    trim(p."FirstName" || ' ' || p."LastName") as player_name,
                    pa."Date" as latest_date,
                    pa."OverallRating" as overall_rating,
                    pa."Potential" as potential,
                    pa."PreferredFoot" as preferred_foot
                from player_attributes pa
                join players p on p."Id" = pa."PlayerId"
                order by pa."PlayerId", pa."Date" desc, pa."Id" desc;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_players_LastName_FirstName",
                table: "players");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "players");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "players");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "players",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                update players
                set "Name" = trim("FirstName" || ' ' || "LastName");
                """);

            migrationBuilder.CreateIndex(
                name: "IX_players_Name",
                table: "players",
                column: "Name");

            migrationBuilder.Sql("""
                create or replace view public.vw_player_latest_attributes as
                select distinct on (pa."PlayerId")
                    pa."PlayerId" as player_id,
                    p."Name" as player_name,
                    pa."Date" as latest_date,
                    pa."OverallRating" as overall_rating,
                    pa."Potential" as potential,
                    pa."PreferredFoot" as preferred_foot
                from player_attributes pa
                join players p on p."Id" = pa."PlayerId"
                order by pa."PlayerId", pa."Date" desc, pa."Id" desc;
                """);
        }
    }
}
