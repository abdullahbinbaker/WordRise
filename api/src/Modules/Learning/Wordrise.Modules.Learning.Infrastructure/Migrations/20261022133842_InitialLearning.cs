using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wordrise.Modules.Learning.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialLearning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "learning");

            migrationBuilder.CreateTable(
                name: "Learners",
                schema: "learning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DailyGoal = table.Column<int>(type: "integer", nullable: false),
                    CurrentStreak = table.Column<int>(type: "integer", nullable: false),
                    LongestStreak = table.Column<int>(type: "integer", nullable: false),
                    LastStudyDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Learners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lessons",
                schema: "learning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Scenario = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    GrammarPoint = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Content = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lessons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WordProgress",
                schema: "learning",
                columns: table => new
                {
                    LearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    WordId = table.Column<Guid>(type: "uuid", nullable: false),
                    LessonId = table.Column<Guid>(type: "uuid", nullable: false),
                    English = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Arabic = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IntervalDays = table.Column<int>(type: "integer", nullable: false),
                    DueOn = table.Column<DateOnly>(type: "date", nullable: false),
                    ReviewCount = table.Column<int>(type: "integer", nullable: false),
                    LastRating = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WordProgress", x => new { x.LearnerId, x.WordId });
                });

            migrationBuilder.CreateTable(
                name: "LessonCompletions",
                schema: "learning",
                columns: table => new
                {
                    LessonId = table.Column<Guid>(type: "uuid", nullable: false),
                    LearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletedOn = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonCompletions", x => new { x.LearnerId, x.LessonId });
                    table.ForeignKey(
                        name: "FK_LessonCompletions_Learners_LearnerId",
                        column: x => x.LearnerId,
                        principalSchema: "learning",
                        principalTable: "Learners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_Number",
                schema: "learning",
                table: "Lessons",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WordProgress_LearnerId_DueOn",
                schema: "learning",
                table: "WordProgress",
                columns: new[] { "LearnerId", "DueOn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LessonCompletions",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "Lessons",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "WordProgress",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "Learners",
                schema: "learning");
        }
    }
}
