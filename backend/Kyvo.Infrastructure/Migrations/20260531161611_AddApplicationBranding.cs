using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kyvo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationBranding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "branding_enabled",
                table: "applications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "branding_logo_path",
                table: "applications",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "branding_primary_color",
                table: "applications",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "branding_secondary_color",
                table: "applications",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "branding_enabled",
                table: "applications");

            migrationBuilder.DropColumn(
                name: "branding_logo_path",
                table: "applications");

            migrationBuilder.DropColumn(
                name: "branding_primary_color",
                table: "applications");

            migrationBuilder.DropColumn(
                name: "branding_secondary_color",
                table: "applications");
        }
    }
}
