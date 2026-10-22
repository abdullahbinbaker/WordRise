using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wordrise.Modules.Learning.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GenericLanguageNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "English",
                schema: "learning",
                table: "WordProgress",
                newName: "Target");

            migrationBuilder.RenameColumn(
                name: "Arabic",
                schema: "learning",
                table: "WordProgress",
                newName: "Native");

            migrationBuilder.AddColumn<string>(
                name: "NativeLanguage",
                schema: "learning",
                table: "Lessons",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "ar");

            migrationBuilder.AddColumn<string>(
                name: "TargetLanguage",
                schema: "learning",
                table: "Lessons",
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
                schema: "learning",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "TargetLanguage",
                schema: "learning",
                table: "Lessons");

            migrationBuilder.RenameColumn(
                name: "Target",
                schema: "learning",
                table: "WordProgress",
                newName: "English");

            migrationBuilder.RenameColumn(
                name: "Native",
                schema: "learning",
                table: "WordProgress",
                newName: "Arabic");
        }
    }
}
