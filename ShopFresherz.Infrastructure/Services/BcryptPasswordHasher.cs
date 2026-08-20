using ShopFresherz.Domain.Interfaces.Services;

namespace ShopFresherz.Infrastructure.Services;

/// <summary>
/// BCrypt password hashing implementation using work factor 12.
/// Uses the BCrypt.Net-Next library internally.
/// </summary>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    /// <summary>Hashes a plain-text password using BCrypt with work factor 12.</summary>
    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    /// <summary>Verifies a plain-text password against a stored BCrypt hash.</summary>
    public bool Verify(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
