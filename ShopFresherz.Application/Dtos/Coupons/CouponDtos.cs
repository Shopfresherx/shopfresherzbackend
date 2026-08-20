using ShopFresherz.Domain.Enums;

namespace ShopFresherz.Application.Dtos.Coupons;

/// <summary>Coupon validation result DTO.</summary>
public sealed class CouponValidationDto
{
    public bool IsValid { get; set; }
    public string? Code { get; set; }
    public CouponType? Type { get; set; }
    public decimal? Value { get; set; }
    public decimal? DiscountAmount { get; set; }
    public string? Message { get; set; }
}

/// <summary>Request payload for creating a coupon.</summary>
public sealed class CreateCouponRequest
{
    public string Code { get; set; } = string.Empty;
    public CouponType Type { get; set; }
    public decimal Value { get; set; }
    public decimal? MinimumOrderAmount { get; set; }
    public int? MaxUses { get; set; }
    public int? MaxUsesPerUser { get; set; }
    public bool IsStackable { get; set; } = false;
    public DateTime? ExpiresAt { get; set; }
    public Guid? RestrictToProductId { get; set; }
    public int? RestrictToCategoryId { get; set; }
}

/// <summary>Request payload for updating coupon limits and lifecycle.</summary>
public sealed class UpdateCouponRequest
{
    public bool? IsActive { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? MaxUses { get; set; }
    public int? MaxUsesPerUser { get; set; }
}

/// <summary>Full coupon summary DTO returned by admin list endpoint.</summary>
public sealed class CouponDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public CouponType Type { get; set; }
    public decimal Value { get; set; }
    public decimal? MinimumOrderAmount { get; set; }
    public int? MaxUses { get; set; }
    public int UsedCount { get; set; }
    public int? MaxUsesPerUser { get; set; }
    public bool IsStackable { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public Guid? RestrictToProductId { get; set; }
    public int? RestrictToCategoryId { get; set; }
    public DateTime CreatedAt { get; set; }
}
