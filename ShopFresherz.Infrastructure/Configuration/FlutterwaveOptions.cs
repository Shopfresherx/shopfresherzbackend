namespace ShopFresherz.Infrastructure.Configuration;

/// <summary>Configuration options for Flutterwave fallback payments.</summary>
public sealed class FlutterwaveOptions
{
    public string SecretKey { get; init; } = string.Empty;

    public string PublicKey { get; init; } = string.Empty;

    public string EncryptionKey { get; init; } = string.Empty;

    public string CallbackUrl { get; init; } = "https://shopfresherz.com/order/confirmed";

    public string WebhookSecret { get; init; } = string.Empty;
}
