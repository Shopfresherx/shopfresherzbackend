using ShopFresherz.Domain.Common;

namespace ShopFresherz.Domain.Entities;

/// <summary>
/// A featured product card displayed in the middle or bottom section of the homepage.
/// Each record links to an existing product; title, description, slug and imageUrl are
/// denormalised from the product at creation / update time.
/// </summary>
public class FeaturedSection : BaseEntity
{
    /// <summary>Gets or sets the section this card belongs to ("middle" or "bottom").</summary>
    public string Section { get; set; } = string.Empty;

    /// <summary>Gets or sets the product ID this card is linked to.</summary>
    public Guid ProductId { get; set; }

    // ── Denormalised product fields (populated from the product on write) ──────

    /// <summary>Gets or sets the product name (cached from Product.Name).</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the product short description (cached from Product.ShortDescription).</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the product slug (cached from Product.Slug).</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Gets or sets the first product image URL (cached from Product.ImageUrls[0]).</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Gets or sets the formatted current price (e.g. "₦4,500").</summary>
    public string? Price { get; set; }

    /// <summary>Gets or sets the discount badge text (e.g. "36% OFF"), if applicable.</summary>
    public string? Badge { get; set; }

    // ── Promo-specific fields (provided by admin) ─────────────────────────────

    /// <summary>Gets or sets the section tag label (e.g. "SUMMER SALES").</summary>
    public string? Tag { get; set; }

    /// <summary>Gets or sets the call-to-action button label (e.g. "SHOP NOW").</summary>
    public string? CtaText { get; set; }

    /// <summary>Gets or sets the display sort order.</summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>Gets or sets whether this card is currently active.</summary>
    public bool IsActive { get; set; } = true;
}
