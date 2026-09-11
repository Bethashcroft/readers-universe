using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ReadersRealm.Api.Models;

public class Book
{
    public int Id { get; set; }

    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Author { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string CoverUrl { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Isbn { get; set; } = string.Empty;

    [MaxLength(500)]
    public string MatchKey { get; set; } = string.Empty;

    public DateTime? CoverCheckedAt { get; set; }

    public static string BuildMatchKey(string title, string author)
    {
        var key = $"{Squash(title)}|{Squash(author)}";

        return key.Length <= 500 ? key : key[..500];
    }

    private static string Squash(string text)
    {
        var builder = new StringBuilder();

        foreach (var c in text)
        {
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(char.ToLowerInvariant(c));
            }
        }

        return builder.ToString();
    }

    public static string CleanIsbn(string isbn) =>
        new(isbn.Where(char.IsLetterOrDigit).ToArray());
}
