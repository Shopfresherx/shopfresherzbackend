namespace ShopFresherz.Application.Dtos.FeaturedSections;

/// <summary>Featured section card DTO returned by public and admin endpoints.</summary>
public sealed class FeaturedSectionDto
{
    /// <summary>Gets or sets the card ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the section name ("middle" or "bottom").</summary>
    public string Section { get; set; } = string.Empty;

    /// <summary>Gets or sets the linked product ID.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Gets or sets the product name.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the product short description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the product slug (for /product/{slug} links).</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Gets or sets the first product image URL.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Gets or sets the formatted current price (e.g. "₦4,500").</summary>
    public string? Price { get; set; }

    /// <summary>Gets or sets the discount badge text (e.g. "36% OFF").</summary>
    public string? Badge { get; set; }

    /// <summary>Gets or sets the promo tag label (e.g. "SUMMER SALES").</summary>
    public string? Tag { get; set; }

    /// <summary>Gets or sets the call-to-action button label (e.g. "SHOP NOW").</summary>
    public string? CtaText { get; set; }
}

/// <summary>Request payload for adding a product to a homepage section (admin).</summary>
public sealed class CreateFeaturedSectionRequest
{
    /// <summary>Gets or sets the section ("middle" or "bottom").</summary>
    public string Section { get; set; } = string.Empty;

    /// <summary>Gets or sets the product to feature.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Gets or sets the promo tag label (e.g. "SUMMER SALES"). Optional.</summary>
    public string? Tag { get; set; }

    /// <summary>Gets or sets the call-to-action button label (e.g. "SHOP NOW"). Optional.</summary>
    public string? CtaText { get; set; }
}

/// <summary>Request payload for updating a featured section card (admin).</summary>
public sealed class UpdateFeaturedSectionRequest
{
    /// <summary>Gets or sets the replacement product ID. When provided, product data is refreshed.</summary>
    public Guid? ProductId { get; set; }

    /// <summary>Gets or sets the updated promo tag label.</summary>
    public string? Tag { get; set; }

    /// <summary>Gets or sets the updated call-to-action button label.</summary>
    public string? CtaText { get; set; }

    /// <summary>Gets or sets the updated active flag.</summary>
    public bool? IsActive { get; set; }
}
