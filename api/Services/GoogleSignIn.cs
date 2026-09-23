using Google.Apis.Auth;

namespace ReadersRealm.Api.Services;

public record GoogleIdentity(string Subject, string Email, bool EmailVerified, string Name);

public interface IGoogleTokenValidator
{
    bool IsConfigured { get; }
    Task<GoogleIdentity?> ValidateAsync(string idToken);
}

public class GoogleTokenValidator(IConfiguration configuration, ILogger<GoogleTokenValidator> logger)
    : IGoogleTokenValidator
{
    private readonly string? _clientId = configuration["Google:ClientId"];

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_clientId);

    public async Task<GoogleIdentity?> ValidateAsync(string idToken)
    {
        if (!IsConfigured)
        {
            return null;
        }

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken,
                new GoogleJsonWebSignature.ValidationSettings { Audience = [_clientId!] }
            );

            return new GoogleIdentity(
                payload.Subject,
                payload.Email ?? string.Empty,
                payload.EmailVerified,
                payload.Name ?? string.Empty
            );
        }
        catch (InvalidJwtException ex)
        {
            logger.LogInformation(ex, "Rejected a Google sign-in token");
            return null;
        }
    }
}
