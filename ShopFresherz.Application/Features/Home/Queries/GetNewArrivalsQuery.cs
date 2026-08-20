using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Product;

namespace ShopFresherz.Application.Features.Home.Queries;

/// <summary>
/// Returns new arrivals products for homepage.
/// </summary>
public sealed record GetNewArrivalsQuery(int Limit = 12) : IRequest<Result<IReadOnlyList<ProductSummaryDto>>>;

