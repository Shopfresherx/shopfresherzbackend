using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Wishlist.Commands;

/// <summary>Command for adding a product to the authenticated user's wishlist.</summary>
/// <param name="UserId">The authenticated user's ID.</param>
/// <param name="ProductId">The product to wishlist.</param>
public sealed record AddToWishlistCommand(Guid UserId, Guid ProductId) : IRequest<Result<bool>>;

/// <summary>Handler for <see cref="AddToWishlistCommand"/>.</summary>
public sealed class AddToWishlistCommandHandler : IRequestHandler<AddToWishlistCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;

    /// <summary>Initialises the handler.</summary>
    public AddToWishlistCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        AddToWishlistCommand command,
        CancellationToken cancellationToken)
    {
        bool exists = await _uow.Wishlists.ExistsAsync(command.UserId, command.ProductId, cancellationToken);
        if (exists)
        {
            return Result<bool>.Success(true);
        }

        Product? product = await _uow.Products.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null) return Error.NotFound("Product");

        await _uow.Wishlists.AddAsync(new Domain.Entities.Wishlist
        {
            UserId    = command.UserId,
            ProductId = command.ProductId,
        }, cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
