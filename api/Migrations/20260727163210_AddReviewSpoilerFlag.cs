using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReadersRealm.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewSpoilerFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ContainsSpoiler",
                table: "Reviews",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContainsSpoiler",
                table: "Reviews");
        }
    }
}
