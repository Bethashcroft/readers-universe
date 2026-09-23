using ReadersRealm.Api.Services;

namespace ReadersRealm.Api.Tests;

public class FakeGoogleTokenValidator : IGoogleTokenValidator
{
    public bool IsConfigured => true;

    public static string TokenFor(
        string subject,
        string email,
        string name,
        bool verified = true
    ) => string.Join('|', subject, email, verified ? "verified" : "unverified", name);

    public Task<GoogleIdentity?> ValidateAsync(string idToken)
    {
        var parts = idToken.Split('|');

        if (parts.Length != 4)
        {
            return Task.FromResult<GoogleIdentity?>(null);
        }

        return Task.FromResult<GoogleIdentity?>(
            new GoogleIdentity(parts[0], parts[1], parts[2] == "verified", parts[3])
        );
    }
}
