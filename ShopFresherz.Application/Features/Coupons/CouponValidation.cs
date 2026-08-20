using ShopFresherz.Application.Dtos.Coupons;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Enums;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Coupons;

internal static class CouponValidation
{
    public static async Task<CouponValidationDto> ValidateAsync(
        IUnitOfWork uow,
        string code,
        decimal orderSubtotal,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        string normalised = code.Trim().ToUpperInvariant();
        Coupon? coupon = await uow.Coupons.GetByCodeAsync(normalised, cancellationToken);

        if (coupon is null)
        {
            return Invalid("Coupon was not found.");
        }

        if (!coupon.IsActive)
        {
            return Invalid("Coupon is not active.", coupon);
        }

        if (coupon.ExpiresAt.HasValue && coupon.ExpiresAt.Value <= DateTime.UtcNow)
        {
            return Invalid("Coupon has expired.", coupon);
        }

        if (coupon.MaxUses.HasValue && coupon.UsedCount >= coupon.MaxUses.Value)
        {
            return Invalid("Coupon usage limit has been reached.", coupon);
        }

        if (coupon.MinimumOrderAmount.HasValue && orderSubtotal < coupon.MinimumOrderAmount.Value)
        {
            return Invalid($"Minimum order amount is {coupon.MinimumOrderAmount.Value:N2}.", coupon);
        }

        if (userId.HasValue && coupon.MaxUsesPerUser.HasValue)
        {
            int usageCount = await uow.Coupons.GetUserUsageCountAsync(
                coupon.Id,
                userId.Value,
                cancellationToken);

            if (usageCount >= coupon.MaxUsesPerUser.Value)
            {
                return Invalid("You have already used this coupon the maximum number of times.", coupon);
            }
        }

        decimal discount = coupon.Type switch
        {
            CouponType.Percentage => Math.Min(orderSubtotal * coupon.Value / 100m, orderSubtotal),
            CouponType.Fixed => Math.Min(coupon.Value, orderSubtotal),
            _ => 0m,
        };

        return new CouponValidationDto
        {
            IsValid = true,
            Code = coupon.Code,
            Type = coupon.Type,
            Value = coupon.Value,
            DiscountAmount = discount,
            Message = "Coupon is valid.",
        };
    }

    private static CouponValidationDto Invalid(string message, Coupon? coupon = null) => new()
    {
        IsValid = false,
        Code = coupon?.Code,
        Type = coupon?.Type,
        Value = coupon?.Value,
        DiscountAmount = 0m,
        Message = message,
    };
}
