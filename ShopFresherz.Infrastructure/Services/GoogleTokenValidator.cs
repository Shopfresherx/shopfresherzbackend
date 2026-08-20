using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using ShopFresherz.Domain.Interfaces.Services;
using ShopFresherz.Infrastructure.Configuration;

namespace ShopFresherz.Infrastructure.Services;

/// <summary>Validates Google ID tokens against the configured OAuth audience.</summary>
public sealed class GoogleTokenValidator : IGoogleTokenValidator
{
    private const string PlaceholderClientId = "REPLACE_WITH_GOOGLE_CLIENT_ID";
    private readonly GoogleAuthSettings _settings;

    public GoogleTokenValidator(IOptions<GoogleAuthSettings> settings)
    {
        _settings = settings.Value;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_settings.ClientId) &&
        !string.Equals(_settings.ClientId, PlaceholderClientId, StringComparison.Ordinal);

    public async Task<GoogleIdentity?> ValidateAsync(
        string idToken,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _settings.ClientId },
                });
        }
        catch (InvalidJwtException ex)
        {
            throw new InvalidGoogleTokenException("Invalid Google token.", ex);
        }

        cancellationToken.ThrowIfCancellationRequested();

        return new GoogleIdentity(
            payload.Subject ?? string.Empty,
            payload.Email?.ToLowerInvariant() ?? string.Empty,
            payload.GivenName ?? "Customer",
            payload.FamilyName ?? string.Empty,
            payload.EmailVerified);
    }
}
