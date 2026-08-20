using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;
using CartEntity = ShopFresherz.Domain.Entities.Cart;
using CartItemEntity = ShopFresherz.Domain.Entities.CartItem;

namespace ShopFresherz.Application.Features.Cart.Commands;

/// <summary>Command for removing a single line item from the cart.</summary>
/// <param name="UserId">The authenticated user's ID (null for guests).</param>
/// <param name="SessionId">The guest session ID.</param>
/// <param name="CartItemId">The cart item to remove.</param>
public sealed record RemoveFromCartCommand(Guid? UserId, string? SessionId, Guid CartItemId)
    : IRequest<Result<bool>>;

/// <summary>Handler for <see cref="RemoveFromCartCommand"/>.</summary>
public sealed class RemoveFromCartCommandHandler : IRequestHandler<RemoveFromCartCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;

    /// <summary>Initialises the handler.</summary>
    public RemoveFromCartCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        RemoveFromCartCommand command,
        CancellationToken cancellationToken)
    {
        CartEntity? cart = command.UserId.HasValue
            ? await _uow.Carts.GetByUserIdAsync(command.UserId.Value, cancellationToken)
            : command.SessionId is not null
                ? await _uow.Carts.GetBySessionIdAsync(command.SessionId, cancellationToken)
                : null;

        if (cart is null)
        {
            return Error.NotFound("Cart");
        }

        CartItemEntity? item = cart.Items.FirstOrDefault(i => i.Id == command.CartItemId);
        if (item is null)
        {
            return Error.NotFound("Cart item");
        }

        cart.Items.Remove(item);
        _uow.Carts.Update(cart);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
