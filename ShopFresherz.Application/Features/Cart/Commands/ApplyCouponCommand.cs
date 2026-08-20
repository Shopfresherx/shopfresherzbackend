using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Coupons;
using ShopFresherz.Application.Features.Coupons;
using ShopFresherz.Domain.Interfaces;
using CartEntity = ShopFresherz.Domain.Entities.Cart;

namespace ShopFresherz.Application.Features.Cart.Commands;

/// <summary>Command for applying a coupon to the active cart.</summary>
public sealed record ApplyCouponCommand(
    Guid? UserId,
    string? SessionId,
    string CouponCode) : IRequest<Result<CouponValidationDto>>;

/// <summary>Handler for <see cref="ApplyCouponCommand"/>.</summary>
public sealed class ApplyCouponCommandHandler
    : IRequestHandler<ApplyCouponCommand, Result<CouponValidationDto>>
{
    private readonly IUnitOfWork _uow;

    public ApplyCouponCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<CouponValidationDto>> Handle(
        ApplyCouponCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.CouponCode))
        {
            return Error.Validation("Coupon code is required.");
        }

        CartEntity? cart = await GetCartAsync(command.UserId, command.SessionId, cancellationToken);
        if (cart is null)
        {
            return Error.NotFound("Cart");
        }

        decimal subtotal = cart.Items.Sum(i =>
            (i.Variant?.Price ?? i.Product.Price) * i.Quantity);

        CouponValidationDto validation = await CouponValidation.ValidateAsync(
            _uow,
            command.CouponCode,
            subtotal,
            command.UserId,
            cancellationToken);

        if (validation.IsValid)
        {
            cart.CouponCode = validation.Code;
            _uow.Carts.Update(cart);
            await _uow.SaveChangesAsync(cancellationToken);
        }

        return Result<CouponValidationDto>.Success(validation);
    }

    private async Task<CartEntity?> GetCartAsync(Guid? userId, string? sessionId, CancellationToken cancellationToken)
    {
        if (userId.HasValue)
        {
            return await _uow.Carts.GetByUserIdAsync(userId.Value, cancellationToken);
        }

        return string.IsNullOrWhiteSpace(sessionId)
            ? null
            : await _uow.Carts.GetBySessionIdAsync(sessionId, cancellationToken);
    }
}
