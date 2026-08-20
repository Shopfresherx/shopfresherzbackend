using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Admin;
using ShopFresherz.Application.Dtos.Review;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Admin.Queries;

/// <summary>Handler for <see cref="GetAdminReviewsQuery"/>.</summary>
public sealed class GetAdminReviewsQueryHandler
    : IRequestHandler<GetAdminReviewsQuery, Result<PagedResult<ReviewDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetAdminReviewsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<ReviewDto>>> Handle(
        GetAdminReviewsQuery query,
        CancellationToken cancellationToken)
    {
        int page = Math.Max(1, query.Page);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);

        (IReadOnlyList<Review> items, int total) = await _uow.Reviews.GetAllPagedAsync(page, pageSize, cancellationToken);

        IReadOnlyList<ReviewDto> reviews = items.Select(r => new ReviewDto
        {
            Id = r.Id,
            UserId = r.UserId,
            ReviewerName = r.User?.FirstName + " " + r.User?.LastName ?? string.Empty,
            Rating = r.Rating,
            Title = r.Title,
            Body = r.Body,
            IsVerifiedPurchase = r.IsVerifiedPurchase,
            CreatedAt = r.CreatedAt,
        }).ToList();

        return Result<PagedResult<ReviewDto>>.Success(
            new PagedResult<ReviewDto>(reviews, total, page, pageSize));
    }
}