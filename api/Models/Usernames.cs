using System.Text.RegularExpressions;

namespace ReadersRealm.Api.Models;

public static partial class Usernames
{
    public const int Shortest = 5;
    public const int Longest = 20;

    public const string RulesMessage =
        "Username must be 5 to 20 characters, using only letters, numbers, dots and underscores.";

    [GeneratedRegex("^[a-zA-Z0-9._]{5,20}$")]
    private static partial Regex Pattern();

    [GeneratedRegex("[^a-z0-9._]")]
    private static partial Regex NotAllowed();

    public static bool IsValid(string userName) => Pattern().IsMatch(userName);

    public static string StartingPoint(string name, string email)
    {
        var fromName = NotAllowed().Replace(name.ToLowerInvariant(), "");
        var fromEmail = NotAllowed().Replace(email.Split('@')[0].ToLowerInvariant(), "");
        var candidate = fromName.Length >= Shortest ? fromName : fromEmail;

        if (candidate.Length < Shortest)
        {
            candidate += "reader";
        }

        return candidate.Length > Longest ? candidate[..Longest] : candidate;
    }

    public static IEnumerable<string> Candidates(string startingPoint)
    {
        yield return startingPoint;

        for (var n = 2; ; n++)
        {
            var suffix = n.ToString();
            var room = Longest - suffix.Length;
            var stem = startingPoint.Length > room ? startingPoint[..room] : startingPoint;
            yield return stem + suffix;
        }
    }
}
