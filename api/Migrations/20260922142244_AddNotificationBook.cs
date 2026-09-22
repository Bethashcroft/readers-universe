using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReadersRealm.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationBook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BookId",
                table: "Notifications",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_BookId",
                table: "Notifications",
                column: "BookId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Books_BookId",
                table: "Notifications",
                column: "BookId",
                principalTable: "Books",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Books_BookId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_BookId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "BookId",
                table: "Notifications");
        }
    }
}
