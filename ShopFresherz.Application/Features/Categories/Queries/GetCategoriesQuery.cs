using AutoMapper;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Product;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Categories.Queries;

/// <summary>Query for retrieving the full category tree.</summary>
public sealed record GetCategoriesQuery() : IRequest<Result<IReadOnlyList<CategoryDto>>>;

/// <summary>Handler for <see cref="GetCategoriesQuery"/>.</summary>
public sealed class GetCategoriesQueryHandler
    : IRequestHandler<GetCategoriesQuery, Result<IReadOnlyList<CategoryDto>>>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    /// <summary>Initialises the handler.</summary>
    public GetCategoriesQueryHandler(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CategoryDto>>> Handle(
        GetCategoriesQuery query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Category> categories =
            await _uow.Categories.GetTreeAsync(cancellationToken);

        return Result<IReadOnlyList<CategoryDto>>.Success(
            _mapper.Map<IReadOnlyList<CategoryDto>>(categories));
    }
}
