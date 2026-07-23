using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReadersRealm.Api.Data;
using ReadersRealm.Api.Models;
using ReadersRealm.Api.Services;

namespace ReadersRealm.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BooksController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IBookLookup _lookup;

    public BooksController(AppDbContext context, IBookLookup lookup)
    {
        _context = context;
        _lookup = lookup;
    }

    [HttpGet("lookup/{isbn}")]
    public async Task<IActionResult> Lookup(string isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
        {
            return BadRequest(new { message = "Enter an ISBN." });
        }

        var result = await _lookup.LookupAsync(isbn);

        if (result == null)
        {
            return NotFound(
                new { message = "No book found for that ISBN. You can enter the details by hand." }
            );
        }

        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetMyBooks()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var books = await _context
            .Books.Where(b => b.UserId == userId)
            .Select(b => ToResponse(b))
            .ToListAsync();

        return Ok(books);
    }

    [HttpGet("browse")]
    [AllowAnonymous]
    public async Task<IActionResult> Browse()
    {
        var books = await _context
            .Books.Where(b =>
                b.Offer == BookOffer.AvailableToBorrow || b.Offer == BookOffer.ForSale
            )
            .Select(b => new BookResponse
            {
                Id = b.Id,
                Title = b.Title,
                Author = b.Author,
                CoverUrl = b.CoverUrl,
                Shelf = b.Shelf,
                Offer = b.Offer,
                Rating = b.Rating,
                UserId = b.UserId,
                SellerVintedUrl = b.User.VintedUrl,
                OwnerName = b.User.DisplayName,
                OwnerUserName = b.User.UserName!,
            })
            .ToListAsync();

        return Ok(books);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetBook(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var book = await _context
            .Books.Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (book == null)
        {
            return NotFound(new { message = "Book not found" });
        }

        var isPrivateShelf = book.Shelf == BookShelf.Tbr || book.Shelf == BookShelf.Dnf;
        if (book.UserId != userId && isPrivateShelf && book.Offer == BookOffer.None)
        {
            return NotFound(new { message = "Book not found" });
        }

        var response = ToResponse(book);
        response.SellerVintedUrl = book.User?.VintedUrl ?? string.Empty;
        response.OwnerName = book.User?.DisplayName ?? string.Empty;
        response.OwnerUserName = book.User?.UserName ?? string.Empty;
        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> AddBook([FromBody] AddBookRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var validationError = ValidateStates(request);
        if (validationError != null)
        {
            return BadRequest(new { message = validationError });
        }

        var book = new Book
        {
            Title = request.Title,
            Author = request.Author,
            CoverUrl = request.CoverUrl,
            Shelf = request.Shelf,
            Offer = request.Offer,
            Rating = request.Rating,
            UserId = userId!,
        };

        _context.Books.Add(book);
        await _context.SaveChangesAsync();

        return Ok(ToResponse(book));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBook(int id, [FromBody] AddBookRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var validationError = ValidateStates(request);
        if (validationError != null)
        {
            return BadRequest(new { message = validationError });
        }

        var book = await _context.Books.FindAsync(id);

        if (book == null)
        {
            return NotFound(new { message = "Book not found" });
        }

        if (book.UserId != userId)
        {
            return Forbid();
        }

        book.Title = request.Title;
        book.Author = request.Author;
        book.CoverUrl = request.CoverUrl;
        book.Shelf = request.Shelf;
        book.Offer = request.Offer;
        book.Rating = request.Rating;

        await _context.SaveChangesAsync();

        return Ok(ToResponse(book));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBook(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var book = await _context.Books.FindAsync(id);

        if (book == null)
        {
            return NotFound(new { message = "Book not found" });
        }

        if (book.UserId != userId)
        {
            return Forbid();
        }

        _context.Books.Remove(book);

        await _context.SaveChangesAsync();

        return Ok();
    }

    private static string? ValidateStates(AddBookRequest request)
    {
        if (!BookShelf.All.Contains(request.Shelf))
        {
            return $"Shelf must be one of: {string.Join(", ", BookShelf.All)}";
        }

        if (!BookOffer.All.Contains(request.Offer))
        {
            return $"Offer must be one of: {string.Join(", ", BookOffer.All)}";
        }

        return null;
    }

    private static BookResponse ToResponse(Book b) =>
        new()
        {
            Id = b.Id,
            Title = b.Title,
            Author = b.Author,
            CoverUrl = b.CoverUrl,
            Shelf = b.Shelf,
            Offer = b.Offer,
            Rating = b.Rating,
            UserId = b.UserId,
        };
}

public class AddBookRequest
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string CoverUrl { get; set; } = string.Empty;
    public string Shelf { get; set; } = string.Empty;
    public string Offer { get; set; } = BookOffer.None;
    public int? Rating { get; set; }
}

public class BookResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string CoverUrl { get; set; } = string.Empty;
    public string Shelf { get; set; } = string.Empty;
    public string Offer { get; set; } = "none";
    public int? Rating { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string SellerVintedUrl { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerUserName { get; set; } = string.Empty;
}
