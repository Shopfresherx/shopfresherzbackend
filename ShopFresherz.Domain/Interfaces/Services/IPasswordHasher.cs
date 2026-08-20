namespace ShopFresherz.Domain.Interfaces.Services;

/// <summary>Contract for BCrypt password hashing and verification.</summary>
public interface IPasswordHasher
{
    /// <summary>Hashes a plain-text password using BCrypt cost-12.</summary>
    string Hash(string password);

    /// <summary>Verifies a plain-text password against a BCrypt hash.</summary>
    bool Verify(string password, string hash);
}
