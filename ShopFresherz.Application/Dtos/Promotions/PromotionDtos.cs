namespace ShopFresherz.Application.Dtos.Promotions;

// ── Hero Banner ───────────────────────────────────────────────────────────────

/// <summary>Hero carousel banner displayed at the top of the homepage.</summary>
public sealed record HeroBannerDto(
    string Id,
    string? Tag,
    string? Badge,
    string? Title,
    string? Price,
    string? CtaText,
    string? ImageUrl,
    int SortOrder,
    string? Slug,
    string? Description = null);

/// <summary>Full hero banner representation used by admin management endpoints.</summary>
public sealed record AdminHeroBannerDto(
    Guid Id,
    Guid? ProductId,
    string SlugId,
    string? Title,
    string? Tag,
    string? Badge,
    string? CtaText,
    string? ImageUrl,
    string? Price,
    string? Slug,
    int SortOrder,
    bool IsActive,
    DateTime CreatedAt,
    string? Description = null);

// ── Best Deal Promo Card ──────────────────────────────────────────────────────

/// <summary>Featured product promo card in the "Best Deal" section.</summary>
public sealed record BestDealCardDto(
    string Id,
    string? ImageUrl,
    string? Name,
    decimal? Rating,
    string? OriginalPrice,
    string? SalePrice,
    string? Description,
    string? Badge,
    string? Slug,
    string? VideoUrl = null);

/// <summary>Full best-deal representation returned after an admin upsert.</summary>
public sealed record AdminBestDealDto(
    Guid Id,
    Guid? ProductId,
    string SlugId,
    string? Title,
    string? ImageUrl,
    string? Price,
    string? Badge,
    string? Slug,
    int SortOrder,
    bool IsActive,
    DateTime CreatedAt,
    string? Description = null,
    string? VideoUrl = null);

// ── Promotion Section Banner ──────────────────────────────────────────────────

/// <summary>Full-width promotional banner shown below the featured products grid.</summary>
public sealed record PromoBannerDto(
    string Id,
    string? Title,
    string? Subtitle,
    string? CtaText,
    string? ImageUrl,
    string? ImageAlt,
    string? Badge);

// ── Computer Accessories Promo Cards ─────────────────────────────────────────

/// <summary>Feature-card variant inside the Computer Accessories promo section.</summary>
public sealed record FeatureCardDto(
    string Id,
    string Type,
    string? Title,
    string? Subtitle,
    string? ButtonText,
    string? ImageUrl,
    string? PriceLabel,
    string? PriceValue,
    string? Slug,
    string? Description = null);

/// <summary>Discount-card variant inside the Computer Accessories promo section.</summary>
public sealed record DiscountCardDto(
    string Id,
    string Type,
    string? TagText,
    string? Headline,
    string? Description,
    string? ButtonText);

// ── Laptop Promo Card ─────────────────────────────────────────────────────────

/// <summary>Promotional card for a specific laptop product shown below the Computer Accessories section.</summary>
public sealed record LaptopPromoDto(
    string Id,
    string? Title,
    string? Subtitle,
    string? CtaText,
    string? ImageUrl,
    string? ImageAlt,
    string? PriceBadge,
    string? PriceValue,
    string? Slug,
    string? Description = null);

// ── Flash Sale ────────────────────────────────────────────────────────────────

/// <summary>Flash sale card shown in the storefront flash-sale carousel.</summary>
public sealed record FlashSaleCardDto(
    string Id,
    string? Tag,
    string? Badge,
    string? Title,
    string? Price,
    string? CtaText,
    string? ImageUrl,
    int SortOrder,
    string? Slug,
    string? Description = null,
    string? VideoUrl = null);

/// <summary>Full flash sale card representation used by admin management endpoints.</summary>
public sealed record AdminFlashSaleDto(
    Guid Id,
    Guid? ProductId,
    string SlugId,
    string? Title,
    string? Tag,
    string? Badge,
    string? CtaText,
    string? ImageUrl,
    string? Price,
    string? Slug,
    int SortOrder,
    bool IsActive,
    DateTime CreatedAt,
    string? Description = null,
    string? VideoUrl = null);

// ── Store Promo Banner ────────────────────────────────────────────────────────

/// <summary>Full-width storefront promo banner shown in an admin-manageable placement.</summary>
public sealed record StorePromoBannerDto(
    string Id,
    string? Title,
    string? Subtitle,
    string? CtaText,
    string? ImageUrl,
    string? ImageAlt,
    string? Badge,
    string? Tag,
    string? Slug,
    string? Description = null);
