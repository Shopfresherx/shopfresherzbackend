using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Wishlist.Queries;

/// <summary>Query that checks whether a product is in the authenticated user's favourites list.</summary>
/// <param name="UserId">The authenticated user's ID.</param>
/// <param name="ProductId">The product to check.</param>
public sealed record CheckFavoriteQuery(Guid UserId, Guid ProductId)
    : IRequest<Result<bool>>;

/// <summary>Handler for <see cref="CheckFavoriteQuery"/>.</summary>
public sealed class CheckFavoriteQueryHandler : IRequestHandler<CheckFavoriteQuery, Result<bool>>
{
    private readonly IUnitOfWork _uow;

    /// <summary>Initialises the handler.</summary>
    public CheckFavoriteQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        CheckFavoriteQuery query,
        CancellationToken cancellationToken)
    {
        bool isFavorited = await _uow.Wishlists.ExistsAsync(
            query.UserId, query.ProductId, cancellationToken);

        return Result<bool>.Success(isFavorited);
    }
}
