using Microsoft.EntityFrameworkCore;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces.Repositories;

namespace ShopFresherz.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="ICouponRepository"/>.</summary>
internal sealed class EfCouponRepository : ICouponRepository
{
    private readonly ShopFresherzDbContext _context;

    /// <summary>Initialises a new instance of <see cref="EfCouponRepository"/>.</summary>
    public EfCouponRepository(ShopFresherzDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Coupon?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Coupons
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Coupon?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        string normalised = code.ToUpperInvariant();

        return await _context.Coupons
            .FirstOrDefaultAsync(c => c.Code == normalised, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default)
    {
        string normalised = code.ToUpperInvariant();

        return await _context.Coupons
            .AsNoTracking()
            .AnyAsync(c => c.Code == normalised, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetUserUsageCountAsync(int couponId, Guid userId, CancellationToken cancellationToken = default)
    {
        // Count the number of orders this user has placed that used this coupon.
        return await _context.Orders
            .CountAsync(o => o.CouponId == couponId && o.UserId == userId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Coupon>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Coupons
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Coupon coupon, CancellationToken cancellationToken = default)
    {
        await _context.Coupons.AddAsync(coupon, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(Coupon coupon)
    {
        _context.Coupons.Update(coupon);
    }
}
