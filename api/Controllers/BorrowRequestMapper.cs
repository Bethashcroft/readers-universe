using ReadersRealm.Api.Models;

namespace ReadersRealm.Api.Controllers;

public static class BorrowRequestMapper
{
    public static IQueryable<BorrowRequestResponse> ToResponses(
        this IQueryable<BorrowRequest> requests
    ) =>
        requests.Select(r => new BorrowRequestResponse
        {
            Id = r.Id,
            BookId = r.LibraryEntry.BookId,
            BookTitle = r.LibraryEntry.Book.Title,
            FromUserId = r.FromUserId,
            FromUserName = r.FromUser.DisplayName,
            ToUserId = r.ToUserId,
            ToUserName = r.ToUser.DisplayName,
            Status = r.Status,
            Message = r.Message,
            Date = r.Date,
        });
}
