using FluentValidation;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Cart;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;
using CartEntity = ShopFresherz.Domain.Entities.Cart;
using CartItemEntity = ShopFresherz.Domain.Entities.CartItem;

namespace ShopFresherz.Application.Features.Cart.Commands;

/// <summary>Command for updating the quantity of a specific cart line item.</summary>
/// <param name="UserId">The authenticated user's ID (null for guests).</param>
/// <param name="SessionId">The guest session ID.</param>
/// <param name="CartItemId">The cart item to update.</param>
/// <param name="Quantity">The new desired quantity.</param>
public sealed record UpdateCartItemCommand(
    Guid? UserId,
    string? SessionId,
    Guid CartItemId,
    int Quantity) : IRequest<Result<bool>>;

/// <summary>Handler for <see cref="UpdateCartItemCommand"/>.</summary>
public sealed class UpdateCartItemCommandHandler : IRequestHandler<UpdateCartItemCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;

    /// <summary>Initialises the handler.</summary>
    public UpdateCartItemCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        UpdateCartItemCommand command,
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

        if (command.Quantity <= 0)
        {
            cart.Items.Remove(item);
        }
        else
        {
            item.Quantity = Math.Min(command.Quantity, 10);
        }

        _uow.Carts.Update(cart);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}

/// <summary>Validator for <see cref="UpdateCartItemCommand"/>.</summary>
public sealed class UpdateCartItemCommandValidator : AbstractValidator<UpdateCartItemCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public UpdateCartItemCommandValidator()
    {
        RuleFor(x => x.CartItemId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
    }
}
