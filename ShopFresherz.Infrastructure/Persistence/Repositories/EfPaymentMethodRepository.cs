using Microsoft.EntityFrameworkCore;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces.Repositories;

namespace ShopFresherz.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IPaymentMethodRepository"/>.</summary>
internal sealed class EfPaymentMethodRepository : IPaymentMethodRepository
{
    private readonly ShopFresherzDbContext _context;

    /// <summary>Initialises a new instance of <see cref="EfPaymentMethodRepository"/>.</summary>
    public EfPaymentMethodRepository(ShopFresherzDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<SavedCard?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SavedCards
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SavedCard>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.SavedCards
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.IsDefault)
            .ThenByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(SavedCard card, CancellationToken cancellationToken = default)
    {
        await _context.SavedCards.AddAsync(card, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(SavedCard card)
    {
        _context.SavedCards.Update(card);
    }

    /// <inheritdoc />
    public void Delete(SavedCard card)
    {
        card.DeletedAt = DateTime.UtcNow;
        _context.SavedCards.Update(card);
    }
}
