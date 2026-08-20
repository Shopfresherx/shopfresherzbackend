using FluentValidation;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Cart;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;
using CartEntity = ShopFresherz.Domain.Entities.Cart;
using CartItemEntity = ShopFresherz.Domain.Entities.CartItem;

namespace ShopFresherz.Application.Features.Cart.Commands;

/// <summary>Command for adding a product (or variant) to a cart.</summary>
/// <param name="UserId">The authenticated user's ID (null for guests).</param>
/// <param name="SessionId">The guest browser session ID (null for authenticated users).</param>
/// <param name="Request">The add-to-cart payload.</param>
public sealed record AddToCartCommand(Guid? UserId, string? SessionId, AddToCartRequest Request)
    : IRequest<Result<CartDto>>;

/// <summary>Handler for <see cref="AddToCartCommand"/>.</summary>
public sealed class AddToCartCommandHandler : IRequestHandler<AddToCartCommand, Result<CartDto>>
{
    private readonly IUnitOfWork _uow;

    /// <summary>Initialises the handler.</summary>
    public AddToCartCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<CartDto>> Handle(
        AddToCartCommand command,
        CancellationToken cancellationToken)
    {
        Product? product = await _uow.Products.GetByIdAsync(command.Request.ProductId, cancellationToken);
        if (product is null || !product.IsActive)
        {
            return Error.NotFound("Product");
        }

        int available = command.Request.VariantId.HasValue
            ? product.Variants.FirstOrDefault(v => v.Id == command.Request.VariantId)?.AvailableQty ?? 0
            : product.AvailableQty;

        if (available < command.Request.Quantity)
        {
            return Error.Validation($"Only {available} units are available in stock.");
        }

        CartEntity? cart = command.UserId.HasValue
            ? await _uow.Carts.GetByUserIdAsync(command.UserId.Value, cancellationToken)
            : command.SessionId is not null
                ? await _uow.Carts.GetBySessionIdAsync(command.SessionId, cancellationToken)
                : null;

        if (cart is null)
        {
            cart = new CartEntity
            {
                UserId    = command.UserId,
                SessionId = command.SessionId,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            await _uow.Carts.AddAsync(cart, cancellationToken);
        }

        CartItemEntity? existing = cart.Items.FirstOrDefault(i =>
            i.ProductId == command.Request.ProductId &&
            i.VariantId == command.Request.VariantId);

        if (existing is not null)
        {
            existing.Quantity = Math.Min(existing.Quantity + command.Request.Quantity, 10);
        }
        else
        {
            cart.Items.Add(new CartItemEntity
            {
                CartId    = cart.Id,
                ProductId = command.Request.ProductId,
                VariantId = command.Request.VariantId,
                Quantity  = command.Request.Quantity,
            });
        }

        _uow.Carts.Update(cart);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<CartDto>.Success(new CartDto
        {
            Id        = cart.Id,
            UserId    = cart.UserId,
            SessionId = cart.SessionId,
            ExpiresAt = cart.ExpiresAt,
            Items = cart.Items.Select(i => new CartItemDto
            {
                Id           = i.Id,
                ProductId    = i.ProductId,
                ProductName  = product.Name,
                ProductSlug  = product.Slug,
                ProductImageUrl = product.Images.OrderBy(img => img.SortOrder).FirstOrDefault()?.DisplayUrl,
                VariantId    = i.VariantId,
                Quantity     = i.Quantity,
                UnitPrice    = i.VariantId.HasValue
                    ? product.Variants.FirstOrDefault(v => v.Id == i.VariantId)?.Price ?? product.Price
                    : product.Price,
            }).ToList(),
        });
    }
}

/// <summary>Validator for <see cref="AddToCartCommand"/>.</summary>
public sealed class AddToCartCommandValidator : AbstractValidator<AddToCartCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public AddToCartCommandValidator()
    {
        RuleFor(x => x.Request.ProductId).NotEmpty();
        RuleFor(x => x.Request.Quantity).InclusiveBetween(1, 10);
    }
}
