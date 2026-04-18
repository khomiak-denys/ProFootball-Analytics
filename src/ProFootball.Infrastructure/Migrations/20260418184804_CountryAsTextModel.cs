using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProFootball.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CountryAsTextModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_leagues_countries_CountryId",
                table: "leagues");

            migrationBuilder.DropForeignKey(
                name: "FK_matches_countries_CountryId",
                table: "matches");

            migrationBuilder.DropTable(
                name: "countries");

            migrationBuilder.DropIndex(
                name: "IX_matches_CountryId",
                table: "matches");

            migrationBuilder.DropIndex(
                name: "IX_leagues_CountryId",
                table: "leagues");

            migrationBuilder.DropColumn(
                name: "CountryId",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "CountryId",
                table: "leagues");

            migrationBuilder.AddColumn<string>(
                name: "CountryName",
                table: "matches",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CountryName",
                table: "leagues",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_matches_CountryName",
                table: "matches",
                column: "CountryName");

            migrationBuilder.CreateIndex(
                name: "IX_leagues_CountryName",
                table: "leagues",
                column: "CountryName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_matches_CountryName",
                table: "matches");

            migrationBuilder.DropIndex(
                name: "IX_leagues_CountryName",
                table: "leagues");

            migrationBuilder.DropColumn(
                name: "CountryName",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "CountryName",
                table: "leagues");

            migrationBuilder.AddColumn<int>(
                name: "CountryId",
                table: "matches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CountryId",
                table: "leagues",
                type: "integer",
                nullable: false,
                defaultValue: 0);

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

            migrationBuilder.CreateIndex(
                name: "IX_matches_CountryId",
                table: "matches",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_leagues_CountryId",
                table: "leagues",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_countries_Name",
                table: "countries",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_leagues_countries_CountryId",
                table: "leagues",
                column: "CountryId",
                principalTable: "countries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_matches_countries_CountryId",
                table: "matches",
                column: "CountryId",
                principalTable: "countries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
