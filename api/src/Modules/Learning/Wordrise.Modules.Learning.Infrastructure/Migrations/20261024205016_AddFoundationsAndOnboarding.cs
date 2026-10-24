using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wordrise.Modules.Learning.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFoundationsAndOnboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Lessons_Number",
                schema: "learning",
                table: "Lessons");

            migrationBuilder.AddColumn<int>(
                name: "GrammarStep",
                schema: "learning",
                table: "Lessons",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Track",
                schema: "learning",
                table: "Lessons",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Main");

            migrationBuilder.AddColumn<bool>(
                name: "KnowsLetters",
                schema: "learning",
                table: "Learners",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "OnboardingDone",
                schema: "learning",
                table: "Learners",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "StartStep",
                schema: "learning",
                table: "Learners",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_Track_Number",
                schema: "learning",
                table: "Lessons",
                columns: new[] { "Track", "Number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Lessons_Track_Number",
                schema: "learning",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "GrammarStep",
                schema: "learning",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "Track",
                schema: "learning",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "KnowsLetters",
                schema: "learning",
                table: "Learners");

            migrationBuilder.DropColumn(
                name: "OnboardingDone",
                schema: "learning",
                table: "Learners");

            migrationBuilder.DropColumn(
                name: "StartStep",
                schema: "learning",
                table: "Learners");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_Number",
                schema: "learning",
                table: "Lessons",
                column: "Number",
                unique: true);
        }
    }
}
