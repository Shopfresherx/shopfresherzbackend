using Microsoft.EntityFrameworkCore;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces.Repositories;

namespace ShopFresherz.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="INotifyRequestRepository"/>.</summary>
internal sealed class EfNotifyRequestRepository : INotifyRequestRepository
{
    private readonly ShopFresherzDbContext _context;

    public EfNotifyRequestRepository(ShopFresherzDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NotifyRequest>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return await _context.NotifyRequests
            .Include(r => r.User)
            .Include(r => r.Product)
            .Where(r => !r.IsNotified && r.Product.StockQty > r.Product.ReservedQty)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(NotifyRequest request, CancellationToken cancellationToken = default)
    {
        await _context.NotifyRequests.AddAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.NotifyRequests.AnyAsync(
            r => r.UserId == userId && r.ProductId == productId && !r.IsNotified,
            cancellationToken);
    }
}
