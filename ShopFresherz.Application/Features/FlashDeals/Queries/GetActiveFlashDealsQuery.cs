using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.FlashDeals;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.FlashDeals.Queries;

/// <summary>Query for currently live flash deals.</summary>
public sealed record GetActiveFlashDealsQuery() : IRequest<Result<IReadOnlyList<FlashDealDto>>>;

/// <summary>Handler for <see cref="GetActiveFlashDealsQuery"/>.</summary>
public sealed class GetActiveFlashDealsQueryHandler
    : IRequestHandler<GetActiveFlashDealsQuery, Result<IReadOnlyList<FlashDealDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetActiveFlashDealsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<FlashDealDto>>> Handle(
        GetActiveFlashDealsQuery query,
        CancellationToken cancellationToken)
    {
        DateTime now = DateTime.UtcNow;
        IReadOnlyList<FlashDeal> deals = await _uow.FlashDeals.GetActiveAsync(cancellationToken);

        IReadOnlyList<FlashDealDto> dtos = deals
            .OrderBy(d => d.EndsAt)
            .Select(d => Map(d, now))
            .ToList();

        return Result<IReadOnlyList<FlashDealDto>>.Success(dtos);
    }

    private static FlashDealDto Map(FlashDeal deal, DateTime now)
    {
        int? remaining = deal.MaxQuantity.HasValue
            ? Math.Max(0, deal.MaxQuantity.Value - deal.SoldQuantity)
            : null;

        return new FlashDealDto
        {
            Id = deal.Id,
            ProductId = deal.ProductId,
            ProductName = deal.Product.Name,
            ProductSlug = deal.Product.Slug,
            ProductImageUrl = deal.Product.Images.OrderBy(i => i.SortOrder).FirstOrDefault()?.ThumbUrl,
            SalePrice = deal.SalePrice,
            OriginalPrice = deal.OriginalPrice,
            DiscountPercent = deal.OriginalPrice <= 0
                ? 0
                : Math.Round((deal.OriginalPrice - deal.SalePrice) / deal.OriginalPrice * 100m, 0),
            StartsAt = deal.StartsAt,
            EndsAt = deal.EndsAt,
            MaxQuantity = deal.MaxQuantity,
            SoldQuantity = deal.SoldQuantity,
            RemainingQuantity = remaining,
            IsLive = deal.IsActive && deal.StartsAt <= now && deal.EndsAt > now,
            TimeRemaining = deal.EndsAt > now ? deal.EndsAt - now : TimeSpan.Zero,
        };
    }
}
