using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReadersRealm.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorSort : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthorSort",
                table: "Books",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthorSort",
                table: "Books");
        }
    }
}
