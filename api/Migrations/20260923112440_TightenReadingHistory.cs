using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReadersRealm.Api.Migrations
{
    /// <inheritdoc />
    public partial class TightenReadingHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReadingSessions_LibraryEntryId_FinishedDate",
                table: "ReadingSessions");

            migrationBuilder.AddColumn<bool>(
                name: "ReadingsEdited",
                table: "LibraryEntries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ReadingSessions_LibraryEntryId_FinishedDate",
                table: "ReadingSessions",
                columns: new[] { "LibraryEntryId", "FinishedDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReadingSessions_LibraryEntryId_FinishedDate",
                table: "ReadingSessions");

            migrationBuilder.DropColumn(
                name: "ReadingsEdited",
                table: "LibraryEntries");

            migrationBuilder.CreateIndex(
                name: "IX_ReadingSessions_LibraryEntryId_FinishedDate",
                table: "ReadingSessions",
                columns: new[] { "LibraryEntryId", "FinishedDate" });
        }
    }
}
