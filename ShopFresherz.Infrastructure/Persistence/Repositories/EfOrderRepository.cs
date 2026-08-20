using Microsoft.EntityFrameworkCore;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Enums;
using ShopFresherz.Domain.Interfaces.Repositories;

namespace ShopFresherz.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IOrderRepository"/>.</summary>
internal sealed class EfOrderRepository : IOrderRepository
{
    private readonly ShopFresherzDbContext _context;

    /// <summary>Initialises a new instance of <see cref="EfOrderRepository"/>.</summary>
    public EfOrderRepository(ShopFresherzDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.Coupon)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Order?> GetByPaymentReferenceAsync(string reference, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.PaymentReference == reference, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Order?> GetByTxRefAsync(string txRef, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.TxRef == txRef, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> GetUserOrdersAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Order> query = _context.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt);

        int total = await query.CountAsync(cancellationToken);
        List<Order> items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> GetAllAsync(
        int page,
        int pageSize,
        string? status,
        string? paymentStatus,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Order> query = _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.User)
            .OrderByDescending(o => o.CreatedAt);

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse(status, ignoreCase: true, out OrderStatus orderStatus))
        {
            query = query.Where(o => o.Status == orderStatus);
        }

        if (!string.IsNullOrWhiteSpace(paymentStatus) &&
            Enum.TryParse(paymentStatus, ignoreCase: true, out PaymentStatus parsedPaymentStatus))
        {
            query = query.Where(o => o.PaymentStatus == parsedPaymentStatus);
        }

        if (from.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= to.Value);
        }

        int total = await query.CountAsync(cancellationToken);
        List<Order> items = await query
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

        return await _context.Orders
            .AsNoTracking()
            .CountAsync(o => o.CreatedAt >= today && o.CreatedAt < tomorrow, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<decimal> SumRevenueAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.PaymentStatus == PaymentStatus.Paid && o.CreatedAt >= from && o.CreatedAt < to)
            .SumAsync(o => (decimal?)o.Total, cancellationToken) ?? 0m;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Order>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .AsNoTracking()
            .Include(o => o.User)
            .OrderByDescending(o => o.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Order>> GetByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Where(o => o.Status == status)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Order>> GetExpiredAwaitingPaymentAsync(
        DateTime cutoff,
        CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .Where(o =>
                (o.Status == OrderStatus.AwaitingPayment || o.Status == OrderStatus.Draft) &&
                o.CreatedAt < cutoff)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> TryClaimForVerificationAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Orders SET PaymentStatus = 'Verifying' WHERE Id = {orderId} AND PaymentStatus = 'Unpaid'",
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken = default)
    {
        string yearPrefix = $"SFZ-{DateTime.UtcNow:yyyy}-";

        // Derive the next sequence number from the highest suffix actually in use this year,
        // not a row count: a count-based scheme regenerates already-used numbers as soon as
        // any order for the year is deleted (the count drops but past numbers were never freed),
        // which caused every checkout to fail with a duplicate-key error once test orders were
        // cleaned up in production.
        List<string> existingNumbers = await _context.Orders
            .IgnoreQueryFilters()
            .Where(o => o.OrderNumber.StartsWith(yearPrefix))
            .Select(o => o.OrderNumber)
            .ToListAsync(cancellationToken);

        int maxSequence = 0;
        foreach (string number in existingNumbers)
        {
            string suffix = number.Length > yearPrefix.Length ? number[yearPrefix.Length..] : string.Empty;
            if (int.TryParse(suffix, out int sequence) && sequence > maxSequence)
            {
                maxSequence = sequence;
            }
        }

        return $"{yearPrefix}{(maxSequence + 1):D5}";
    }

    /// <inheritdoc />
    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await _context.Orders.AddAsync(order, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(Order order)
    {
        _context.Orders.Update(order);
    }
}
