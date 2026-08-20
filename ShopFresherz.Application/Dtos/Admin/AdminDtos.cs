namespace ShopFresherz.Application.Dtos.Admin;

using ShopFresherz.Domain.Enums;

/// <summary>Dashboard statistics summary for the admin panel.</summary>
public sealed class DashboardStatsDto
{
    /// <summary>Gets or sets today's paid revenue.</summary>
    public decimal RevenueToday { get; set; }

    /// <summary>Gets or sets the current week's paid revenue.</summary>
    public decimal RevenueThisWeek { get; set; }

    /// <summary>Gets or sets the current month's paid revenue.</summary>
    public decimal RevenueThisMonth { get; set; }

    /// <summary>Gets or sets the number of orders created today.</summary>
    public int OrdersToday { get; set; }

    /// <summary>Gets or sets the number of processing orders.</summary>
    public int ProcessingOrders { get; set; }

    /// <summary>Gets or sets the number of shipped orders.</summary>
    public int ShippedOrders { get; set; }

    /// <summary>Gets or sets the number of users created today.</summary>
    public int NewUsersToday { get; set; }

    /// <summary>Gets or sets the total number of active products.</summary>
    public int TotalProducts { get; set; }

    /// <summary>Gets or sets the number of low-stock products.</summary>
    public int LowStockCount { get; set; }

    /// <summary>Gets or sets the last 10 orders.</summary>
    public IReadOnlyList<RecentOrderDto> RecentOrders { get; set; } = [];

    /// <summary>Gets or sets the total revenue from all paid orders.</summary>
    public decimal TotalRevenue { get; set; }

    /// <summary>Gets or sets the total number of orders placed.</summary>
    public int TotalOrders { get; set; }

    /// <summary>Gets or sets the total number of registered customers.</summary>
    public int TotalCustomers { get; set; }

    /// <summary>Gets or sets the number of orders in Pending or AwaitingPayment state.</summary>
    public int PendingOrders { get; set; }

    /// <summary>Gets or sets the number of products with AvailableQty ≤ 5.</summary>
    public int LowStockProducts { get; set; }

    /// <summary>Gets or sets the total revenue for the current calendar month.</summary>
    public decimal MonthlyRevenue { get; set; }

    /// <summary>Gets or sets the number of orders placed today.</summary>
    public int TodayOrders { get; set; }
}

/// <summary>Compact recent order row for the admin dashboard.</summary>
public sealed class RecentOrderDto
{
    /// <summary>Gets or sets the order number.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the customer email.</summary>
    public string CustomerEmail { get; set; } = string.Empty;

    /// <summary>Gets or sets the order total.</summary>
    public decimal Total { get; set; }

    /// <summary>Gets or sets the order status.</summary>
    public OrderStatus Status { get; set; }

    /// <summary>Gets or sets when the order was created.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>Admin-facing user row.</summary>
public sealed class AdminUserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsVerified { get; set; }
    public int LoyaltyPoints { get; set; }
    public int OrderCount { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Low-stock inventory row.</summary>
public sealed class LowStockDto
{
    public Guid Id { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int StockQty { get; set; }
    public int ReservedQty { get; set; }
    public int AvailableQty { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
}

/// <summary>Request payload for admin order status updates.</summary>
public sealed class UpdateOrderStatusRequest
{
    /// <summary>
    /// Gets or sets the target status. Nullable so a missing/omitted value in the request
    /// body is rejected explicitly rather than silently binding to <see cref="OrderStatus.Pending"/>
    /// (enum value 0).
    /// </summary>
    public OrderStatus? NewStatus { get; set; }
    public string? TrackingNumber { get; set; }
}

/// <summary>Request payload for manual loyalty points adjustments.</summary>
public sealed class AdjustLoyaltyRequest
{
    public int Points { get; set; }
    public string Reason { get; set; } = string.Empty;
}

/// <summary>Analytics data for admin panel.</summary>
public sealed class AnalyticsDto
{
    /// <summary>Gets or sets today's paid revenue.</summary>
    public decimal RevenueToday { get; set; }

    /// <summary>Gets or sets the current week's paid revenue.</summary>
    public decimal RevenueThisWeek { get; set; }

    /// <summary>Gets or sets the current month's paid revenue.</summary>
    public decimal RevenueThisMonth { get; set; }

    /// <summary>Gets or sets the number of orders created today.</summary>
    public int OrdersToday { get; set; }

    /// <summary>Gets or sets the number of processing orders.</summary>
    public int ProcessingOrders { get; set; }

    /// <summary>Gets or sets the number of shipped orders.</summary>
    public int ShippedOrders { get; set; }

    /// <summary>Gets or sets the number of users created today.</summary>
    public int NewUsersToday { get; set; }

    /// <summary>Gets or sets the total number of active products.</summary>
    public int TotalProducts { get; set; }

    /// <summary>Gets or sets the number of low-stock products.</summary>
    public int LowStockCount { get; set; }

    /// <summary>Gets or sets the last 10 orders.</summary>
    public IReadOnlyList<RecentOrderDto> RecentOrders { get; set; } = [];

    /// <summary>Gets or sets the total revenue from all paid orders.</summary>
    public decimal TotalRevenue { get; set; }

    /// <summary>Gets or sets the total number of orders placed.</summary>
    public int TotalOrders { get; set; }

    /// <summary>Gets or sets the total number of registered customers.</summary>
    public int TotalCustomers { get; set; }

    /// <summary>Gets or sets the number of orders in Pending or AwaitingPayment state.</summary>
    public int PendingOrders { get; set; }

    /// <summary>Gets or sets the number of products with AvailableQty ≤ 5.</summary>
    public int LowStockProducts { get; set; }

    /// <summary>Gets or sets the total revenue for the current calendar month.</summary>
    public decimal MonthlyRevenue { get; set; }

    /// <summary>Gets or sets the number of orders placed today.</summary>
    public int TodayOrders { get; set; }
}
