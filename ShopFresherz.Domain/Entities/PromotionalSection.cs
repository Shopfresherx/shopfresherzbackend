using ShopFresherz.Domain.Common;

namespace ShopFresherz.Domain.Entities;

/// <summary>
/// A promotional content record for one of the storefront's marketing sections.
/// Each row holds a <see cref="SectionKey"/> that groups it to a named UI zone
/// (e.g. "hero", "best-deal", "promo-banner") and a JSON payload matching the
/// shape expected by that zone's front-end component.
/// </summary>
public sealed class PromotionalSection : BaseEntity
{
    /// <summary>
    /// Identifies which UI section this record belongs to.
    /// Valid values: "hero" | "best-deal" | "promo-banner" |
    ///               "accessories-promo" | "laptop-promo"
    /// </summary>
    public string SectionKey { get; set; } = string.Empty;

    /// <summary>
    /// The card / content type within the section.
    /// Used when a section can contain heterogeneous card types
    /// (e.g. "feature-card" vs "discount-card" inside "accessories-promo").
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>Front-end friendly identifier (e.g. "promo-1", "best-deal-1").</summary>
    public string SlugId { get; set; } = string.Empty;

    /// <summary>The linked product's URL slug (populated from Product.Slug on admin write).</summary>
    public string? Slug { get; set; }

    /// <summary>The product linked to this promotion, when the content is product-backed.</summary>
    public Guid? ProductId { get; set; }

    // ── Shared text fields ────────────────────────────────────────────────────

    /// <summary>Small label shown above the title (e.g. "SUMMER SALES").</summary>
    public string? Tag { get; set; }

    /// <summary>Short promotional badge text (e.g. "29% OFF").</summary>
    public string? Badge { get; set; }

    /// <summary>Primary heading / product name.</summary>
    public string? Title { get; set; }

    /// <summary>Secondary heading or supporting subtitle.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Body description text.</summary>
    public string? Description { get; set; }

    /// <summary>Bold headline text (used in discount-card type).</summary>
    public string? Headline { get; set; }

    // ── CTA fields ────────────────────────────────────────────────────────────

    /// <summary>Call-to-action button label.</summary>
    public string? CtaText { get; set; }

    /// <summary>Button text (alias when CtaText is used for a link and this for a button).</summary>
    public string? ButtonText { get; set; }

    // ── Image fields ──────────────────────────────────────────────────────────

    /// <summary>CDN URL of the primary image.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>CDN URL of an optional background/preview video (currently used by Best Deal).</summary>
    public string? VideoUrl { get; set; }

    /// <summary>Alt text for the image (SEO / accessibility).</summary>
    public string? ImageAlt { get; set; }

    // ── Pricing fields ────────────────────────────────────────────────────────

    /// <summary>Formatted price string shown on hero banners (e.g. "₦95,000").</summary>
    public string? PriceText { get; set; }

    /// <summary>Small label preceding a price value (e.g. "Only for:").</summary>
    public string? PriceLabel { get; set; }

    /// <summary>Formatted price value (e.g. "₦299,000").</summary>
    public string? PriceValue { get; set; }

    /// <summary>Savings badge shown near the price (e.g. "Save up to ₦20,000").</summary>
    public string? PriceBadge { get; set; }

    /// <summary>Original (before-sale) price as a formatted string.</summary>
    public string? OriginalPriceText { get; set; }

    /// <summary>Sale / discounted price as a formatted string.</summary>
    public string? SalePriceText { get; set; }

    /// <summary>Product rating (0–5 scale).</summary>
    public decimal? Rating { get; set; }

    // ── Display control ───────────────────────────────────────────────────────

    /// <summary>Position of this record within its section (lower = first).</summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>Whether this record is currently displayed on the storefront.</summary>
    public bool IsActive { get; set; } = true;
}
