using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wordrise.Modules.ContentAuthoring.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBriefLanguages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NativeLanguage",
                schema: "authoring",
                table: "Drafts",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "ar");

            migrationBuilder.AddColumn<string>(
                name: "TargetLanguage",
                schema: "authoring",
                table: "Drafts",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "en");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NativeLanguage",
                schema: "authoring",
                table: "Drafts");

            migrationBuilder.DropColumn(
                name: "TargetLanguage",
                schema: "authoring",
                table: "Drafts");
        }
    }
}
