using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces.Services;

namespace ShopFresherz.Infrastructure.Services;

/// <summary>
/// JWT RS256 token service.
/// Reads the RSA private key PEM from configuration key <c>Jwt:PrivateKeyPem</c>
/// and the public key from <c>Jwt:PublicKeyPem</c>.
/// Access tokens expire in 15 minutes; refresh tokens are 64-byte CSPRNG strings.
/// </summary>
public sealed class TokenService : ITokenService
{
    private readonly RsaSecurityKey _privateKey;
    private readonly RsaSecurityKey _publicKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly JwtSecurityTokenHandler _handler = new();

    /// <summary>Initialises a new instance of <see cref="TokenService"/>.</summary>
    public TokenService(IConfiguration configuration)
    {
        string privateKeyPem = configuration["Jwt:PrivateKeyPem"]
            ?? throw new InvalidOperationException("Jwt:PrivateKeyPem is not configured.");
        string publicKeyPem  = configuration["Jwt:PublicKeyPem"]
            ?? throw new InvalidOperationException("Jwt:PublicKeyPem is not configured.");

        // SmarterASP environment variables cannot contain literal newlines.
        // They may be provided as "\\n"; normalize to actual newlines.
        privateKeyPem = privateKeyPem.Replace("\\n", "\n");
        publicKeyPem  = publicKeyPem.Replace("\\n", "\n");


        // Environment variables may store PEM keys with escaped newlines (\n).
        // Normalize both formats to actual newlines.
        privateKeyPem = privateKeyPem.Replace("\\n", "\n").Trim();
        publicKeyPem = publicKeyPem.Replace("\\n", "\n").Trim();

        RSA privateRsa = RSA.Create();
        privateRsa.ImportFromPem(privateKeyPem);
        _privateKey = new RsaSecurityKey(privateRsa);

        RSA publicRsa = RSA.Create();
        publicRsa.ImportFromPem(publicKeyPem);
        _publicKey = new RsaSecurityKey(publicRsa);

        _issuer = configuration["Jwt:Issuer"] ?? "ShopFresherz";
        _audience = configuration["Jwt:Audience"] ?? "ShopFresherz";
    }

    /// <inheritdoc />
    public string GenerateAccessToken(User user)
    {
        Claim[] claims =
        [
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        ];

        SigningCredentials credentials = new(
            _privateKey,
            SecurityAlgorithms.RsaSha256);

        JwtSecurityToken token = new(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials);

        return _handler.WriteToken(token);
    }

    /// <inheritdoc />
    public string GenerateRefreshToken()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    /// <inheritdoc />
    public Guid? GetUserIdFromExpiredToken(string token)
    {
        TokenValidationParameters parameters = new()
        {
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _publicKey,
            ValidateLifetime = false, // Allow expired tokens for refresh
        };

        try
        {
            ClaimsPrincipal principal = _handler.ValidateToken(token, parameters, out _);
            string? sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return Guid.TryParse(sub, out Guid id) ? id : null;
        }
        catch
        {
            return null;
        }
    }
}

