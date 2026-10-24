using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wordrise.Modules.ContentAuthoring.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDraftDiscard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiscardedFrom",
                schema: "authoring",
                table: "Drafts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscardedFrom",
                schema: "authoring",
                table: "Drafts");
        }
    }
}
