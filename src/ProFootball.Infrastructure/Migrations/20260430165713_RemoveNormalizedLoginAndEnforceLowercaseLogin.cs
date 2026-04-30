using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProFootball.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveNormalizedLoginAndEnforceLowercaseLogin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_app_users_NormalizedLogin",
                table: "app_users");

            migrationBuilder.DropColumn(
                name: "NormalizedLogin",
                table: "app_users");

            migrationBuilder.CreateIndex(
                name: "IX_app_users_Login",
                table: "app_users",
                column: "Login",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_app_users_login_lowercase",
                table: "app_users",
                sql: "\"Login\" = lower(\"Login\")");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_app_users_Login",
                table: "app_users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_app_users_login_lowercase",
                table: "app_users");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedLogin",
                table: "app_users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_app_users_NormalizedLogin",
                table: "app_users",
                column: "NormalizedLogin",
                unique: true);
        }
    }
}
