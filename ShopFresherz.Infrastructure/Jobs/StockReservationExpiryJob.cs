using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Enums;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Infrastructure.Jobs;

/// <summary>
/// Releases reserved stock for orders older than 30 minutes that are still awaiting payment,
/// including abandoned two-step inline-popup Draft orders that never call confirm-order.
/// </summary>
public sealed class StockReservationExpiryJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StockReservationExpiryJob> _logger;

    public StockReservationExpiryJob(
        IServiceScopeFactory scopeFactory,
        ILogger<StockReservationExpiryJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>Executes the stock reservation expiry job.</summary>
    public async Task ExecuteAsync()
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IUnitOfWork uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        DateTime cutoff = DateTime.UtcNow.AddMinutes(-30);
        IReadOnlyList<Order> expiredOrders =
            await uow.Orders.GetExpiredAwaitingPaymentAsync(cutoff, CancellationToken.None);

        int released = 0;
        foreach (Order order in expiredOrders)
        {
            foreach (OrderItem item in order.Items)
            {
                Product? product = await uow.Products.GetByIdAsync(item.ProductId, CancellationToken.None);
                if (product is null)
                {
                    continue;
                }

                if (item.VariantId.HasValue)
                {
                    ProductVariant? variant = product.Variants.FirstOrDefault(v => v.Id == item.VariantId.Value);
                    if (variant is not null)
                    {
                        variant.ReservedQty = Math.Max(0, variant.ReservedQty - item.Quantity);
                    }
                }
                else
                {
                    product.ReservedQty = Math.Max(0, product.ReservedQty - item.Quantity);
                }

                uow.Products.Update(product);
            }

            order.Status = OrderStatus.Cancelled;
            uow.Orders.Update(order);
            released++;
        }

        if (released > 0)
        {
            await uow.SaveChangesAsync(CancellationToken.None);
            _logger.LogInformation(
                "StockReservationExpiryJob cancelled {Count} expired orders and released stock.",
                released);
        }
    }
}
