using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReadersRealm.Api.Migrations
{
    /// <inheritdoc />
    public partial class LinkOlderPostsToFinishes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "Activities" AS a
                SET "ReadingSessionId" = (
                    SELECT r."Id"
                    FROM "ReadingSessions" AS r
                    INNER JOIN "LibraryEntries" AS e ON e."Id" = r."LibraryEntryId"
                    WHERE e."UserId" = a."UserId"
                      AND e."BookId" = a."BookId"
                      AND r."FinishedDate" > a."Date" - INTERVAL '2 days'
                      AND r."FinishedDate" <= a."Date" + INTERVAL '1 day'
                    ORDER BY ABS(EXTRACT(EPOCH FROM (r."FinishedDate" - a."Date")))
                    LIMIT 1
                )
                WHERE a."Type" = 'finished'
                  AND a."ReadingSessionId" IS NULL;
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
