using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kyvo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationClientPostLogoutRedirectUris : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "post_logout_redirect_uris",
                table: "application_clients",
                type: "json",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "post_logout_redirect_uris",
                table: "application_clients");
        }
    }
}
