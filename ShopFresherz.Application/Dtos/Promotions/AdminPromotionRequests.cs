using System.ComponentModel.DataAnnotations;

namespace ShopFresherz.Application.Dtos.Promotions;

// ── Hero Banner ───────────────────────────────────────────────────────────────

/// <summary>
/// Payload for adding a new hero carousel banner linked to an existing product.
/// The banner's title, image and price are auto-populated from the product record.
/// </summary>
public sealed record SetHeroBannerRequest(
    [Required] Guid ProductId,
    [MaxLength(200)] string? Tag,
    [MaxLength(100)] string? Badge,
    [MaxLength(100)] string? CtaText,
    int SortOrder = 1,
    [MaxLength(1000)] string? ImageUrl = null,
    [MaxLength(1000)] string? Description = null);

// ── Best Deal ─────────────────────────────────────────────────────────────────

/// <summary>
/// Payload to set (replace) the single "Best Deal" featured product.
/// Name, image, rating, pricing and description are auto-populated from the product.
/// </summary>
public sealed record SetBestDealRequest(
    [Required] Guid ProductId,
    [MaxLength(100)] string? Badge,
    int SortOrder = 1,
    [MaxLength(1000)] string? ImageUrl = null,
    [MaxLength(1000)] string? VideoUrl = null,
    [MaxLength(1000)] string? Description = null);

// ── Promo Banner ──────────────────────────────────────────────────────────────

/// <summary>
/// Payload to set (replace) the full-width promotional section banner.
/// This section is not tied to a product — all fields are provided manually.
/// </summary>
public sealed record SetPromoBannerRequest(
    [Required, MaxLength(500)] string Title,
    [Required, MaxLength(500)] string Subtitle,
    [Required, MaxLength(100)] string CtaText,
    [Required, MaxLength(1000)] string ImageUrl,
    [MaxLength(300)] string? ImageAlt,
    [MaxLength(100)] string? Badge);

// ── Computer Accessories Promo ────────────────────────────────────────────────

/// <summary>
/// Payload to set (replace) the Computer Accessories featured product card.
/// Title, image and pricing are auto-populated from the product.
/// </summary>
public sealed record SetAccessoriesPromoRequest(
    [Required] Guid ProductId,
    [Required, MaxLength(100)] string CtaText,
    [MaxLength(1000)] string? ImageUrl = null,
    [MaxLength(1000)] string? Description = null);

// ── Laptop / Bottom Promo ─────────────────────────────────────────────────────

/// <summary>
/// Payload to set (replace) the bottom promo card shown below the Computer Accessories section.
/// Title, subtitle, image and pricing are auto-populated from the product.
/// </summary>
public sealed record SetLaptopPromoRequest(
    [Required] Guid ProductId,
    [Required, MaxLength(100)] string CtaText,
    [MaxLength(1000)] string? ImageUrl = null,
    [MaxLength(1000)] string? Description = null);

// ── Flash Sale ────────────────────────────────────────────────────────────────

/// <summary>
/// Payload for adding a new flash-sale card. ProductId is optional: when supplied,
/// title/price are auto-populated from the product; when omitted, Title is required
/// and all display fields are provided manually.
/// </summary>
public sealed record SetFlashSaleRequest(
    Guid? ProductId,
    [MaxLength(500)] string? Title,
    [MaxLength(200)] string? Tag,
    [MaxLength(100)] string? Badge,
    [MaxLength(100)] string? CtaText,
    [MaxLength(1000)] string? ImageUrl,
    [MaxLength(1000)] string? VideoUrl = null,
    int SortOrder = 1,
    [MaxLength(1000)] string? Description = null);

/// <summary>Partial update for a flash-sale card. Supply a new ProductId to refresh product data.</summary>
public sealed record UpdateFlashSaleRequest(
    Guid? ProductId,
    [MaxLength(500)] string? Title,
    [MaxLength(200)] string? Tag,
    [MaxLength(100)] string? Badge,
    [MaxLength(100)] string? CtaText,
    [MaxLength(1000)] string? ImageUrl,
    [MaxLength(1000)] string? VideoUrl = null,
    int? SortOrder = null,
    bool? IsActive = null,
    [MaxLength(1000)] string? Description = null);

// ── Store Promo Banner ────────────────────────────────────────────────────────

/// <summary>
/// Payload to set (replace) the storefront promo banner placement. ProductId is optional:
/// when supplied, Title/ImageUrl default from the product unless explicitly overridden.
/// </summary>
public sealed record SetStorePromoBannerRequest(
    Guid? ProductId,
    [MaxLength(500)] string? Title,
    [MaxLength(500)] string? Subtitle,
    [Required, MaxLength(100)] string CtaText,
    [MaxLength(1000)] string? ImageUrl,
    [MaxLength(300)] string? ImageAlt,
    [MaxLength(100)] string? Badge,
    [MaxLength(200)] string? Tag,
    [MaxLength(1000)] string? Description = null);

/// <summary>Partial update for the storefront promo banner placement.</summary>
public sealed record UpdateStorePromoBannerRequest(
    Guid? ProductId,
    [MaxLength(500)] string? Title,
    [MaxLength(500)] string? Subtitle,
    [MaxLength(100)] string? CtaText,
    [MaxLength(1000)] string? ImageUrl,
    [MaxLength(300)] string? ImageAlt,
    [MaxLength(100)] string? Badge,
    [MaxLength(200)] string? Tag,
    [MaxLength(1000)] string? Description = null);

// ═══════════════════════════════════════════════════════════════════════════════
//  UPDATE REQUEST PAYLOADS  (all fields optional — only provided fields change)
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>Partial update for a hero carousel banner. Supply a new ProductId to refresh product data.</summary>
public sealed record UpdateHeroBannerRequest(
    Guid? ProductId,
    [MaxLength(200)] string? Tag,
    [MaxLength(100)] string? Badge,
    [MaxLength(100)] string? CtaText,
    int? SortOrder,
    bool? IsActive,
    [MaxLength(1000)] string? ImageUrl = null,
    [MaxLength(1000)] string? Description = null);

/// <summary>Partial update for the Best Deal card. Supply a new ProductId to swap the featured product.</summary>
public sealed record UpdateBestDealRequest(
    Guid? ProductId,
    [MaxLength(1000)] string? ImageUrl = null,
    [MaxLength(1000)] string? VideoUrl = null,
    [MaxLength(1000)] string? Description = null);

/// <summary>Partial update for the full-width promo banner (content-only, no product link).</summary>
public sealed record UpdatePromoBannerRequest(
    [MaxLength(500)] string? Title,
    [MaxLength(500)] string? Subtitle,
    [MaxLength(100)] string? CtaText,
    [MaxLength(1000)] string? ImageUrl,
    [MaxLength(300)] string? ImageAlt,
    [MaxLength(100)] string? Badge);

/// <summary>Partial update for the Computer Accessories featured product card.</summary>
public sealed record UpdateAccessoriesPromoRequest(
    Guid? ProductId,
    [MaxLength(100)] string? CtaText,
    [MaxLength(1000)] string? ImageUrl = null,
    [MaxLength(1000)] string? Description = null);

/// <summary>Partial update for the bottom laptop/promo card.</summary>
public sealed record UpdateLaptopPromoRequest(
    Guid? ProductId,
    [MaxLength(100)] string? CtaText,
    [MaxLength(1000)] string? ImageUrl = null,
    [MaxLength(1000)] string? Description = null);
