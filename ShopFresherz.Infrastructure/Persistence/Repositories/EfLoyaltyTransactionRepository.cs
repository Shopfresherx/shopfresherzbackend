using Microsoft.EntityFrameworkCore;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces.Repositories;

namespace ShopFresherz.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="ILoyaltyTransactionRepository"/>.</summary>
internal sealed class EfLoyaltyTransactionRepository : ILoyaltyTransactionRepository
{
    private readonly ShopFresherzDbContext _context;

    public EfLoyaltyTransactionRepository(ShopFresherzDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task AddAsync(LoyaltyTransaction transaction, CancellationToken cancellationToken = default)
    {
        await _context.LoyaltyTransactions.AddAsync(transaction, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<LoyaltyTransaction> Items, int TotalCount)> GetByUserIdAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<LoyaltyTransaction> query = _context.LoyaltyTransactions
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt);

        int total = await query.CountAsync(cancellationToken);
        List<LoyaltyTransaction> items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
