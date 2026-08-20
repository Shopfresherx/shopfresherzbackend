namespace ShopFresherz.Infrastructure.Configuration;

/// <summary>Google OAuth client configuration.</summary>
public sealed class GoogleAuthSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}
