using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ReadersRealm.Api.Migrations
{
    /// <inheritdoc />
    public partial class SplitBooksFromLibraryEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Isbn",
                table: "Books",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MatchKey",
                table: "Books",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                @"UPDATE ""Books""
                  SET ""MatchKey"" = lower(regexp_replace(""Title"" || '|' || ""Author"", '[^[:alnum:]|]', '', 'g'));"
            );

            migrationBuilder.CreateTable(
                name: "LibraryEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Shelf = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Offer = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AddedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BookId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibraryEntries_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LibraryEntries_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                @"INSERT INTO ""LibraryEntries"" (""BookId"", ""UserId"", ""Shelf"", ""Offer"", ""AddedDate"")
                  SELECT ""Id"", ""UserId"", ""Shelf"", ""Offer"", now() FROM ""Books"";"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_BorrowRequests_Books_BookId",
                table: "BorrowRequests");

            migrationBuilder.DropIndex(
                name: "IX_BorrowRequests_BookId",
                table: "BorrowRequests");

            migrationBuilder.AddColumn<int>(
                name: "LibraryEntryId",
                table: "BorrowRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                @"UPDATE ""BorrowRequests"" br
                  SET ""LibraryEntryId"" = e.""Id""
                  FROM ""LibraryEntries"" e
                  WHERE e.""BookId"" = br.""BookId"" AND e.""UserId"" = br.""ToUserId"";"
            );

            migrationBuilder.Sql(@"DELETE FROM ""BorrowRequests"" WHERE ""LibraryEntryId"" = 0;");

            migrationBuilder.DropColumn(
                name: "BookId",
                table: "BorrowRequests");

            migrationBuilder.Sql(
                @"INSERT INTO ""Reviews"" (""BookId"", ""UserId"", ""Rating"", ""Text"", ""ContainsSpoiler"", ""Date"")
                  SELECT b.""Id"", b.""UserId"", b.""Rating"", '', false, now()
                  FROM ""Books"" b
                  WHERE b.""Rating"" IS NOT NULL
                    AND b.""UserId"" <> ''
                    AND NOT EXISTS (
                        SELECT 1 FROM ""Reviews"" r
                        WHERE r.""BookId"" = b.""Id"" AND r.""UserId"" = b.""UserId""
                    );"
            );

            migrationBuilder.Sql(
                @"UPDATE ""LibraryEntries"" e
                  SET ""BookId"" = c.keep_id
                  FROM (SELECT ""Id"", MIN(""Id"") OVER (PARTITION BY ""MatchKey"") AS keep_id FROM ""Books"") c
                  WHERE e.""BookId"" = c.""Id"" AND c.keep_id <> c.""Id"";"
            );

            migrationBuilder.Sql(
                @"UPDATE ""Reviews"" r
                  SET ""BookId"" = c.keep_id
                  FROM (SELECT ""Id"", MIN(""Id"") OVER (PARTITION BY ""MatchKey"") AS keep_id FROM ""Books"") c
                  WHERE r.""BookId"" = c.""Id"" AND c.keep_id <> c.""Id"";"
            );

            migrationBuilder.Sql(
                @"DELETE FROM ""LibraryEntries"" a USING ""LibraryEntries"" b
                  WHERE a.""Id"" > b.""Id"" AND a.""UserId"" = b.""UserId"" AND a.""BookId"" = b.""BookId"";"
            );

            migrationBuilder.Sql(
                @"DELETE FROM ""Reviews"" a USING ""Reviews"" b
                  WHERE a.""Id"" > b.""Id"" AND a.""UserId"" = b.""UserId"" AND a.""BookId"" = b.""BookId"";"
            );

            migrationBuilder.Sql(
                @"DELETE FROM ""Books"" b
                  WHERE b.""Id"" <> (SELECT MIN(b2.""Id"") FROM ""Books"" b2 WHERE b2.""MatchKey"" = b.""MatchKey"");"
            );

            migrationBuilder.Sql(
                @"DELETE FROM ""BorrowRequests"" br
                  WHERE NOT EXISTS (SELECT 1 FROM ""LibraryEntries"" e WHERE e.""Id"" = br.""LibraryEntryId"");"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_Books_AspNetUsers_UserId",
                table: "Books");

            migrationBuilder.DropIndex(
                name: "IX_Books_UserId",
                table: "Books");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_BookId",
                table: "Reviews");

            migrationBuilder.DropColumn(name: "Offer", table: "Books");
            migrationBuilder.DropColumn(name: "Rating", table: "Books");
            migrationBuilder.DropColumn(name: "Shelf", table: "Books");
            migrationBuilder.DropColumn(name: "UserId", table: "Books");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_BookId_UserId",
                table: "Reviews",
                columns: new[] { "BookId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Books_Isbn",
                table: "Books",
                column: "Isbn");

            migrationBuilder.CreateIndex(
                name: "IX_Books_MatchKey",
                table: "Books",
                column: "MatchKey");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryEntries_BookId",
                table: "LibraryEntries",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryEntries_UserId_BookId",
                table: "LibraryEntries",
                columns: new[] { "UserId", "BookId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BorrowRequests_LibraryEntryId",
                table: "BorrowRequests",
                column: "LibraryEntryId");

            migrationBuilder.AddForeignKey(
                name: "FK_BorrowRequests_LibraryEntries_LibraryEntryId",
                table: "BorrowRequests",
                column: "LibraryEntryId",
                principalTable: "LibraryEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BorrowRequests_LibraryEntries_LibraryEntryId",
                table: "BorrowRequests");

            migrationBuilder.DropIndex(
                name: "IX_BorrowRequests_LibraryEntryId",
                table: "BorrowRequests");

            migrationBuilder.DropTable(
                name: "LibraryEntries");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_BookId_UserId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Books_Isbn",
                table: "Books");

            migrationBuilder.DropIndex(
                name: "IX_Books_MatchKey",
                table: "Books");

            migrationBuilder.DropColumn(name: "Isbn", table: "Books");
            migrationBuilder.DropColumn(name: "MatchKey", table: "Books");

            migrationBuilder.DropColumn(
                name: "LibraryEntryId",
                table: "BorrowRequests");

            migrationBuilder.AddColumn<int>(
                name: "BookId",
                table: "BorrowRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Offer",
                table: "Books",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Rating",
                table: "Books",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Shelf",
                table: "Books",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Books",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_BookId",
                table: "Reviews",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_Books_UserId",
                table: "Books",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowRequests_BookId",
                table: "BorrowRequests",
                column: "BookId");

            migrationBuilder.AddForeignKey(
                name: "FK_Books_AspNetUsers_UserId",
                table: "Books",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BorrowRequests_Books_BookId",
                table: "BorrowRequests",
                column: "BookId",
                principalTable: "Books",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
