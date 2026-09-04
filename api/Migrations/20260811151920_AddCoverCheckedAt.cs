using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReadersRealm.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCoverCheckedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CoverCheckedAt",
                table: "Books",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverCheckedAt",
                table: "Books");
        }
    }
}
