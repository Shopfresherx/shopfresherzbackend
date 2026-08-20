namespace ShopFresherz.Infrastructure.Configuration;

/// <summary>Configuration for Brevo transactional email delivery.</summary>
public sealed class BrevoSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "ShopFresherz";
}
