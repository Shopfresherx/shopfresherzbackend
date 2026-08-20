namespace ShopFresherz.Domain.Interfaces.Services;

/// <summary>Validates a Google ID token and returns its trusted identity claims.</summary>
public interface IGoogleTokenValidator
{
    bool IsConfigured { get; }

    Task<GoogleIdentity?> ValidateAsync(
        string idToken,
        CancellationToken cancellationToken = default);
}

public sealed record GoogleIdentity(
    string Subject,
    string Email,
    string? GivenName,
    string? FamilyName,
    bool EmailVerified);

/// <summary>Raised when Google rejects an ID token as invalid.</summary>
public sealed class InvalidGoogleTokenException : Exception
{
    public InvalidGoogleTokenException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
