using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Admin;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Enums;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Admin.Queries;

/// <summary>Handler for <see cref="GetAnalyticsQuery"/>.</summary>
public sealed class GetAnalyticsQueryHandler
    : IRequestHandler<GetAnalyticsQuery, Result<AnalyticsDto>>
{
    private readonly IUnitOfWork _uow;

    public GetAnalyticsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<AnalyticsDto>> Handle(
        GetAnalyticsQuery query,
        CancellationToken cancellationToken)
    {
        DateTime today = DateTime.UtcNow.Date;
        DateTime tomorrow = today.AddDays(1);
        DateTime weekStart = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        DateTime monthStart = new(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        IReadOnlyList<Order> pending = await _uow.Orders.GetByStatusAsync(OrderStatus.Pending, cancellationToken);
        IReadOnlyList<Order> awaitingPayment = await _uow.Orders.GetByStatusAsync(OrderStatus.AwaitingPayment, cancellationToken);
        IReadOnlyList<Order> processing = await _uow.Orders.GetByStatusAsync(OrderStatus.Processing, cancellationToken);
        IReadOnlyList<Order> shipped = await _uow.Orders.GetByStatusAsync(OrderStatus.Shipped, cancellationToken);
        IReadOnlyList<Product> lowStock = await _uow.Products.GetLowStockAsync(5, cancellationToken);
        (IReadOnlyList<Product> _, int productTotal) = await _uow.Products.GetPagedAsync(
            1, 1, cancellationToken: cancellationToken);
        IReadOnlyList<Order> recentOrders = await _uow.Orders.GetRecentAsync(10, cancellationToken);

        decimal revenueToday = await _uow.Orders.SumRevenueAsync(today, tomorrow, cancellationToken);
        decimal revenueWeek = await _uow.Orders.SumRevenueAsync(weekStart, tomorrow, cancellationToken);
        decimal revenueMonth = await _uow.Orders.SumRevenueAsync(monthStart, tomorrow, cancellationToken);
        int ordersToday = await _uow.Orders.CountTodayAsync(cancellationToken);

        AnalyticsDto dto = new()
        {
            RevenueToday = revenueToday,
            RevenueThisWeek = revenueWeek,
            RevenueThisMonth = revenueMonth,
            OrdersToday = ordersToday,
            PendingOrders = pending.Count + awaitingPayment.Count,
            ProcessingOrders = processing.Count,
            ShippedOrders = shipped.Count,
            NewUsersToday = await _uow.Users.CountTodayAsync(cancellationToken),
            TotalProducts = productTotal,
            LowStockCount = lowStock.Count,
            RecentOrders = recentOrders.Select(o => new RecentOrderDto
            {
                OrderNumber = o.OrderNumber,
                CustomerEmail = o.User?.Email ?? o.GuestEmail ?? string.Empty,
                Total = o.Total,
                Status = o.Status,
                CreatedAt = o.CreatedAt,
            }).ToList(),
            TotalRevenue = revenueMonth,
            TotalOrders = pending.Count + awaitingPayment.Count + processing.Count + shipped.Count,
            TotalCustomers = (await _uow.Users.GetAllAsync(1, 1, null, cancellationToken)).TotalCount,

            LowStockProducts = lowStock.Count,
            MonthlyRevenue = revenueMonth,
            TodayOrders = ordersToday,
        };

        return Result<AnalyticsDto>.Success(dto);
    }
}