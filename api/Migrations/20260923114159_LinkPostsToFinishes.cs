using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReadersRealm.Api.Migrations
{
    /// <inheritdoc />
    public partial class LinkPostsToFinishes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReadingSessionId",
                table: "Activities",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Activities_ReadingSessionId",
                table: "Activities",
                column: "ReadingSessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_ReadingSessions_ReadingSessionId",
                table: "Activities",
                column: "ReadingSessionId",
                principalTable: "ReadingSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activities_ReadingSessions_ReadingSessionId",
                table: "Activities");

            migrationBuilder.DropIndex(
                name: "IX_Activities_ReadingSessionId",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "ReadingSessionId",
                table: "Activities");
        }
    }
}
