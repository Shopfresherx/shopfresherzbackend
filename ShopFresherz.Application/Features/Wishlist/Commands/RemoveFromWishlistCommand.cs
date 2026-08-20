using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Wishlist.Commands;

/// <summary>Command for removing a product from the authenticated user's wishlist.</summary>
/// <param name="UserId">The authenticated user's ID.</param>
/// <param name="ProductId">The product to remove.</param>
public sealed record RemoveFromWishlistCommand(Guid UserId, Guid ProductId) : IRequest<Result<bool>>;

/// <summary>Handler for <see cref="RemoveFromWishlistCommand"/>.</summary>
public sealed class RemoveFromWishlistCommandHandler : IRequestHandler<RemoveFromWishlistCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;

    /// <summary>Initialises the handler.</summary>
    public RemoveFromWishlistCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        RemoveFromWishlistCommand command,
        CancellationToken cancellationToken)
    {
        Domain.Entities.Wishlist? entry = await _uow.Wishlists.GetAsync(
            command.UserId, command.ProductId, cancellationToken);

        if (entry is null) return Result<bool>.Success(true);

        _uow.Wishlists.Remove(entry);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
