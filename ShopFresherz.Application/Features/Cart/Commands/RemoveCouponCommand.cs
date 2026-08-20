using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Domain.Interfaces;
using CartEntity = ShopFresherz.Domain.Entities.Cart;

namespace ShopFresherz.Application.Features.Cart.Commands;

/// <summary>Command for removing an applied coupon from the active cart.</summary>
public sealed record RemoveCouponCommand(Guid? UserId, string? SessionId)
    : IRequest<Result<bool>>;

/// <summary>Handler for <see cref="RemoveCouponCommand"/>.</summary>
public sealed class RemoveCouponCommandHandler
    : IRequestHandler<RemoveCouponCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;

    public RemoveCouponCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        RemoveCouponCommand command,
        CancellationToken cancellationToken)
    {
        CartEntity? cart = command.UserId.HasValue
            ? await _uow.Carts.GetByUserIdAsync(command.UserId.Value, cancellationToken)
            : !string.IsNullOrWhiteSpace(command.SessionId)
                ? await _uow.Carts.GetBySessionIdAsync(command.SessionId, cancellationToken)
                : null;

        if (cart is null)
        {
            return Error.NotFound("Cart");
        }

        cart.CouponCode = null;
        _uow.Carts.Update(cart);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
