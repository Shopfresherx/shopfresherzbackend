using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Product;

namespace ShopFresherz.Application.Features.Home.Queries;

/// <summary>
/// Returns best deals products for homepage.
/// For now, best deals is implemented as best sellers by SoldCount.
/// (If you want flash-deals based, we can extend it.)
/// </summary>
public sealed record GetBestDealsQuery(int Limit = 12) : IRequest<Result<IReadOnlyList<ProductSummaryDto>>>;

