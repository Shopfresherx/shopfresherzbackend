namespace ShopFresherz.Infrastructure.Configuration;

/// <summary>Configuration options for Termii SMS delivery.</summary>
public sealed class TermiiOptions
{
    public string ApiKey { get; init; } = string.Empty;

    public string SenderId { get; init; } = "ShopFresherz";
}
