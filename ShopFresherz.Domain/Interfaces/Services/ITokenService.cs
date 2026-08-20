using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Domain.Interfaces.Services;

/// <summary>
/// Contract for JWT RS256 access token and refresh token generation/validation.
/// Access tokens expire in 15 minutes; refresh tokens expire in 7 days.
/// </summary>
public interface ITokenService
{
    /// <summary>Generates a signed RS256 JWT access token for the given user.</summary>
    string GenerateAccessToken(User user);

    /// <summary>Generates a cryptographically secure refresh token string.</summary>
    string GenerateRefreshToken();

    /// <summary>Extracts the user ID claim from an expired access token (used during refresh).</summary>
    Guid? GetUserIdFromExpiredToken(string token);
}
