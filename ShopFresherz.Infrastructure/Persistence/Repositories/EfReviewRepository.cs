using Microsoft.EntityFrameworkCore;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces.Repositories;

namespace ShopFresherz.Infrastructure.Persistence.Repositories;
/// <summary>EF Core implementation of <see cref="IReviewRepository"/>.</summary>
internal sealed class EfReviewRepository : IReviewRepository
{
    private readonly ShopFresherzDbContext _context;

    /// <summary>Initialises a new instance of <see cref="EfReviewRepository"/>.</summary>
    public EfReviewRepository(ShopFresherzDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Review?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Reviews
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Review> Items, int TotalCount)> GetByProductAsync(
        Guid productId,
        int page,
        int pageSize,
        int? starFilter = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Review> query = _context.Reviews
            .Include(r => r.User)
            .Where(r => r.ProductId == productId && r.IsApproved);

        if (starFilter.HasValue)
        {
            query = query.Where(r => r.Rating == starFilter.Value);
        }

        query = query.OrderByDescending(r => r.CreatedAt);

        int total = await query.CountAsync(cancellationToken);
        List<Review> items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Review>> GetPendingApprovalAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Reviews
            .Include(r => r.User)
            .Include(r => r.Product)
            .Where(r => !r.IsApproved)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Returns paginated reviews for admin (all reviews, regardless of approval status).</summary>
    public async Task<(IReadOnlyList<Review> Items, int TotalCount)> GetAllPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Review> query = _context.Reviews
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt);

        int total = await query.CountAsync(cancellationToken);
        List<Review> items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    /// <inheritdoc />
    public async Task<bool> UserHasReviewedAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.Reviews
            .AnyAsync(r => r.UserId == userId && r.ProductId == productId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Review review, CancellationToken cancellationToken = default)
    {
        await _context.Reviews.AddAsync(review, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(Review review)
    {
        _context.Reviews.Update(review);
    }
}
