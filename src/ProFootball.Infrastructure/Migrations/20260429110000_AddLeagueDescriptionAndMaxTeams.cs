using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProFootball.Infrastructure.Migrations;

public partial class AddLeagueDescriptionAndMaxTeams : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "description",
            table: "leagues",
            type: "character varying(2000)",
            maxLength: 2000,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "max_teams",
            table: "leagues",
            type: "integer",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "description",
            table: "leagues");

        migrationBuilder.DropColumn(
            name: "max_teams",
            table: "leagues");
    }
}
