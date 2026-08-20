using Microsoft.EntityFrameworkCore;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces.Repositories;

namespace ShopFresherz.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IUserRepository"/>.</summary>
internal sealed class EfUserRepository : IUserRepository
{
    private readonly ShopFresherzDbContext _context;

    /// <summary>Initialises a new instance of <see cref="EfUserRepository"/>.</summary>
    public EfUserRepository(ShopFresherzDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Phone == phone, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> GetByGoogleIdAsync(string googleId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.GoogleId == googleId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AnyAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> PhoneExistsAsync(string phone, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AnyAsync(u => u.Phone == phone, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetAllAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        IQueryable<User> query = _context.Users
            .AsNoTracking()
            .Include(u => u.Orders)
            .OrderByDescending(u => u.CreatedAt);

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.ToLowerInvariant();
            query = query.Where(u =>
                u.Email.Contains(term) ||
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term) ||
                (u.Phone != null && u.Phone.Contains(term)));
        }

        int total = await query.CountAsync(cancellationToken);
        List<User> items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    /// <inheritdoc />
    public async Task<int> CountTodayAsync(CancellationToken cancellationToken = default)
    {
        DateTime today = DateTime.UtcNow.Date;
        DateTime tomorrow = today.AddDays(1);

        return await _context.Users
            .AsNoTracking()
            .CountAsync(u => u.CreatedAt >= today && u.CreatedAt < tomorrow, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(User user)
    {
        _context.Users.Update(user);
    }
}
