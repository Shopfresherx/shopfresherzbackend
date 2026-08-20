using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Enums;

namespace ShopFresherz.Domain.Interfaces.Repositories;

/// <summary>Repository contract for Order aggregate persistence operations.</summary>
public interface IOrderRepository
{
    /// <summary>Retrieves an order by its unique identifier including items.</summary>
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves an order by the human-readable order number (e.g., SFZ-2026-00001).</summary>
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);

    /// <summary>Retrieves an order by Flutterwave payment reference.</summary>
    Task<Order?> GetByPaymentReferenceAsync(string reference, CancellationToken cancellationToken = default);

    /// <summary>Retrieves an order by its inline-popup Flutterwave transaction reference (TxRef).</summary>
    Task<Order?> GetByTxRefAsync(string txRef, CancellationToken cancellationToken = default);

    /// <summary>Returns a paginated order history for a registered user.</summary>
    Task<(IReadOnlyList<Order> Items, int TotalCount)> GetUserOrdersAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Returns paginated orders matching optional admin filters.</summary>
    Task<(IReadOnlyList<Order> Items, int TotalCount)> GetAllAsync(
        int page,
        int pageSize,
        string? status,
        string? paymentStatus,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default);

    /// <summary>Counts orders created today.</summary>
    Task<int> CountTodayAsync(CancellationToken cancellationToken = default);

    /// <summary>Sums paid order revenue in the supplied UTC date range.</summary>
    Task<decimal> SumRevenueAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);

    /// <summary>Returns the most recent orders.</summary>
    Task<IReadOnlyList<Order>> GetRecentAsync(int count, CancellationToken cancellationToken = default);

    /// <summary>Returns all orders matching a given status (used by admin and background jobs).</summary>
    Task<IReadOnlyList<Order>> GetByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns orders older than the supplied cutoff that are still awaiting payment
    /// (either the legacy hosted-redirect <see cref="OrderStatus.AwaitingPayment"/> state
    /// or the two-step inline-popup <see cref="OrderStatus.Draft"/> state).
    /// </summary>
    Task<IReadOnlyList<Order>> GetExpiredAwaitingPaymentAsync(DateTime cutoff, CancellationToken cancellationToken = default);

    /// <summary>Generates the next sequential order number in SFZ-{YYYY}-{NNNNN} format.</summary>
    Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically transitions the order's <see cref="PaymentStatus"/> from Unpaid to Verifying,
    /// via a single conditional UPDATE. Returns the number of rows affected (0 or 1): 0 means
    /// another request has already claimed or completed this order's payment confirmation.
    /// </summary>
    Task<int> TryClaimForVerificationAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>Adds a new order.</summary>
    Task AddAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>Marks an order as modified.</summary>
    void Update(Order order);
}
