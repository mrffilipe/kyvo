using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kyvo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationBrandingHeroCopy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "branding_hero_subtitle",
                table: "applications",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "branding_hero_title",
                table: "applications",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "branding_hero_subtitle",
                table: "applications");

            migrationBuilder.DropColumn(
                name: "branding_hero_title",
                table: "applications");
        }
    }
}
