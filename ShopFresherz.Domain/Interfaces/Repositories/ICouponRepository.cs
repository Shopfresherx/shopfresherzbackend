using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Domain.Interfaces.Repositories;

/// <summary>Repository contract for Coupon persistence operations.</summary>
public interface ICouponRepository
{
    /// <summary>Retrieves a coupon by its integer ID.</summary>
    Task<Coupon?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves an active coupon by its code (case-insensitive).</summary>
    Task<Coupon?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>Checks whether a coupon code exists.</summary>
    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>Returns the number of times a user has used a specific coupon.</summary>
    Task<int> GetUserUsageCountAsync(int couponId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Returns all coupons ordered by creation date descending.</summary>
    Task<IReadOnlyList<Coupon>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a new coupon.</summary>
    Task AddAsync(Coupon coupon, CancellationToken cancellationToken = default);

    /// <summary>Marks a coupon as modified.</summary>
    void Update(Coupon coupon);
}
