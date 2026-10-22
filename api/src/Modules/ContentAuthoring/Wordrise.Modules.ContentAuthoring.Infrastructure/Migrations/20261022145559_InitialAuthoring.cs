using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wordrise.Modules.ContentAuthoring.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialAuthoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "authoring");

            migrationBuilder.CreateTable(
                name: "Drafts",
                schema: "authoring",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Scenario = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    GrammarStep = table.Column<int>(type: "integer", nullable: false),
                    WordCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CyclesUsed = table.Column<int>(type: "integer", nullable: false),
                    Error = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    PublishedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    PublishedLessonId = table.Column<Guid>(type: "uuid", nullable: true),
                    PublishedLessonNumber = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Feedback = table.Column<string>(type: "jsonb", nullable: true),
                    Revisions = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drafts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Drafts_Status",
                schema: "authoring",
                table: "Drafts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Drafts_UpdatedAtUtc",
                schema: "authoring",
                table: "Drafts",
                column: "UpdatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Drafts",
                schema: "authoring");
        }
    }
}
