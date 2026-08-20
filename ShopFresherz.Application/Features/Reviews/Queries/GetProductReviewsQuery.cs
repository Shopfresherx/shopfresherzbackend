using AutoMapper;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Review;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Reviews.Queries;

/// <summary>Query for paginated approved reviews of a product.</summary>
/// <param name="ProductId">The product ID.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Items per page.</param>
/// <param name="StarFilter">Optional star rating filter (1–5).</param>
public sealed record GetProductReviewsQuery(Guid ProductId, int Page = 1, int PageSize = 10, int? StarFilter = null)
    : IRequest<Result<PagedResult<ReviewDto>>>;

/// <summary>Handler for <see cref="GetProductReviewsQuery"/>.</summary>
public sealed class GetProductReviewsQueryHandler
    : IRequestHandler<GetProductReviewsQuery, Result<PagedResult<ReviewDto>>>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    /// <summary>Initialises the handler.</summary>
    public GetProductReviewsQueryHandler(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<ReviewDto>>> Handle(
        GetProductReviewsQuery query,
        CancellationToken cancellationToken)
    {
        int page     = Math.Max(1, query.Page);
        int pageSize = Math.Clamp(query.PageSize, 1, 50);

        (IReadOnlyList<Review> items, int total) = await _uow.Reviews.GetByProductAsync(
            query.ProductId, page, pageSize, query.StarFilter, cancellationToken);

        IReadOnlyList<ReviewDto> dtos = _mapper.Map<IReadOnlyList<ReviewDto>>(items);

        return Result<PagedResult<ReviewDto>>.Success(
            new PagedResult<ReviewDto>(dtos, total, page, pageSize));
    }
}
