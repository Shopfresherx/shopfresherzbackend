using System.Text.Json;
using ShopFresherz.Application.Common;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Enums;
using ShopFresherz.Domain.Interfaces;
using ShopFresherz.Domain.Interfaces.Services;

namespace ShopFresherz.Application.Features.Payments.Services;

/// <summary>Applies the once-only side effects of a confirmed order payment.</summary>
public interface IOrderPaymentConfirmationService
{
    Task<Result<bool>> CompleteAsync(
        Order order,
        string? customerEmail,
        OrderStatus completedStatus,
        CancellationToken cancellationToken);
}

/// <summary>Applies the once-only side effects of a confirmed order payment.</summary>
public sealed class OrderPaymentConfirmationService : IOrderPaymentConfirmationService
{
    private readonly IUnitOfWork _uow;
    private readonly IEmailService _email;
    private readonly ISmsService _sms;

    public OrderPaymentConfirmationService(IUnitOfWork uow, IEmailService email, ISmsService sms)
    {
        _uow = uow;
        _email = email;
        _sms = sms;
    }

    public async Task<Result<bool>> CompleteAsync(
        Order order,
        string? customerEmail,
        OrderStatus completedStatus,
        CancellationToken cancellationToken)
    {
        if (order.PaymentStatus == PaymentStatus.Paid)
        {
            return Result<bool>.Success(true);
        }

        foreach (OrderItem item in order.Items)
        {
            Product? product = await _uow.Products.GetByIdAsync(item.ProductId, cancellationToken);
            if (product is null) continue;

            if (item.VariantId.HasValue)
            {
                ProductVariant? variant = product.Variants.FirstOrDefault(v => v.Id == item.VariantId);
                if (variant is not null)
                {
                    variant.StockQty = Math.Max(0, variant.StockQty - item.Quantity);
                    variant.ReservedQty = Math.Max(0, variant.ReservedQty - item.Quantity);
                    product.SoldCount += item.Quantity;
                }
            }
            else
            {
                product.StockQty = Math.Max(0, product.StockQty - item.Quantity);
                product.ReservedQty = Math.Max(0, product.ReservedQty - item.Quantity);
                product.SoldCount += item.Quantity;
            }

            _uow.Products.Update(product);
        }

        User? orderUser = null;
        if (order.UserId.HasValue)
        {
            orderUser = await _uow.Users.GetByIdAsync(order.UserId.Value, cancellationToken);
            if (orderUser is not null)
            {
                int pointsEarned = (int)Math.Floor(order.Total / 100m);
                orderUser.LoyaltyPoints += pointsEarned;

                await _uow.LoyaltyTransactions.AddAsync(new LoyaltyTransaction
                {
                    UserId = orderUser.Id,
                    EventType = LoyaltyEventType.Earned,
                    Points = pointsEarned,
                    Description = $"Earned from order {order.OrderNumber}",
                    OrderId = order.Id,
                    ExpiresAt = DateTime.UtcNow.AddYears(1),
                }, cancellationToken);

                _uow.Users.Update(orderUser);
            }
        }

        order.Status = completedStatus;
        order.PaymentStatus = PaymentStatus.Paid;
        _uow.Orders.Update(order);
        await _uow.SaveChangesAsync(cancellationToken);

        string recipientEmail = orderUser?.Email ?? customerEmail ?? order.GuestEmail ?? string.Empty;
        string? phone = orderUser?.Phone ?? ExtractPhoneFromAddressJson(order.DeliveryAddressJson);
        await _email.SendOrderConfirmationAsync(
            recipientEmail,
            orderUser?.FirstName ?? "Customer",
            order.OrderNumber,
            order.Total,
            order.PaymentMethod?.ToString() ?? "Card",
            order.DeliveryMethod,
            order.EstimatedDelivery,
            phone,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(orderUser?.Phone))
        {
            await _sms.SendOrderUpdateAsync(
                orderUser.Phone,
                order.OrderNumber,
                "confirmed & being processed",
                cancellationToken);
        }

        return Result<bool>.Success(true);
    }

    /// <summary>Extracts the "Phone" property from a checkout delivery-address JSON snapshot, if present.</summary>
    private static string? ExtractPhoneFromAddressJson(string deliveryAddressJson)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(deliveryAddressJson);
            return doc.RootElement.TryGetProperty("Phone", out JsonElement phone)
                ? phone.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
