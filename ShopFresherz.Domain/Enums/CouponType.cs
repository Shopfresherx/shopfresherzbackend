namespace ShopFresherz.Domain.Enums;

/// <summary>Defines how a coupon discount is calculated.</summary>
public enum CouponType
{
    /// <summary>Discount is a percentage of the order subtotal.</summary>
    Percentage = 0,

    /// <summary>Discount is a fixed naira amount.</summary>
    Fixed = 1
}
