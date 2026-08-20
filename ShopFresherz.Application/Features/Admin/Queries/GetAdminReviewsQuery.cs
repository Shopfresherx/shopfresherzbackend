using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Review;

namespace ShopFresherz.Application.Features.Admin.Queries;

/// <summary>Query for paginated admin review results.</summary>
public sealed record GetAdminReviewsQuery(
    int Page = 1,
    int PageSize = 20) : IRequest<Result<PagedResult<ReviewDto>>>;