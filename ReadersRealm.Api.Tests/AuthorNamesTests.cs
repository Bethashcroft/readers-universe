using ReadersRealm.Api.Models;

namespace ReadersRealm.Api.Tests;

public class AuthorNamesTests
{
    [Theory]
    [InlineData("Agatha Christie", "christie agatha")]
    [InlineData("R.F. Kuang", "kuang r.f.")]
    [InlineData("Helen   Sarah Fields", "fields helen sarah")]
    [InlineData("Ursula K. Le Guin", "le guin ursula k.")]
    [InlineData("Ludwig van Beethoven", "van beethoven ludwig")]
    [InlineData("Martin Luther King Jr.", "king martin luther jr.")]
    [InlineData("Martin Luther King, Jr.", "king martin luther jr.")]
    [InlineData("Da Chen", "chen da")]
    [InlineData("Homer", "homer")]
    [InlineData("  ", "")]
    public void SortsBySurnameFirst(string author, string expected)
    {
        Assert.Equal(expected, AuthorNames.SortKey(author));
    }

    [Fact]
    public void ABookKeepsItsSortNameInStepWithItsAuthor()
    {
        var book = new Book { Author = "Agatha Christie" };

        Assert.Equal("christie agatha", book.AuthorSort);

        book.Author = "Ursula K. Le Guin";

        Assert.Equal("le guin ursula k.", book.AuthorSort);
    }
}
