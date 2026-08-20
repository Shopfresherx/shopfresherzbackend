using AutoMapper;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Product;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Wishlist.Queries;

/// <summary>Query for fetching all wishlist products for the authenticated user.</summary>
/// <param name="UserId">The authenticated user's ID.</param>
public sealed record GetWishlistQuery(Guid UserId)
    : IRequest<Result<IReadOnlyList<ProductSummaryDto>>>;

/// <summary>Handler for <see cref="GetWishlistQuery"/>.</summary>
public sealed class GetWishlistQueryHandler
    : IRequestHandler<GetWishlistQuery, Result<IReadOnlyList<ProductSummaryDto>>>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    /// <summary>Initialises the handler.</summary>
    public GetWishlistQueryHandler(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ProductSummaryDto>>> Handle(
        GetWishlistQuery query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Domain.Entities.Wishlist> items =
            await _uow.Wishlists.GetByUserIdAsync(query.UserId, cancellationToken);

        IReadOnlyList<ProductSummaryDto> dtos =
            _mapper.Map<IReadOnlyList<ProductSummaryDto>>(items.Select(w => w.Product).ToList());

        return Result<IReadOnlyList<ProductSummaryDto>>.Success(dtos);
    }
}
