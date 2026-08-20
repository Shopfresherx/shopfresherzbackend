using ShopFresherz.Domain.Common;

namespace ShopFresherz.Domain.Entities;

/// <summary>
/// Represents a product brand (Apple, Samsung, Romoss, etc.).
/// </summary>
public class Brand : BaseEntity
{
    /// <summary>Gets or sets the brand's display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the URL-safe slug used in /brand/{slug} routes.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Gets or sets the CDN URL for the brand's logo image.</summary>
    public string? LogoUrl { get; set; }

    /// <summary>Gets or sets a value indicating whether this brand is visible on the storefront.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets the collection of products belonging to this brand.</summary>
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
