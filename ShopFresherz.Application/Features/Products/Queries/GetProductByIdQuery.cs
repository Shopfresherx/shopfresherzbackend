using AutoMapper;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Product;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Products.Queries;

/// <summary>Query for a single product detail by database ID.</summary>
/// <param name="Id">The product's unique identifier.</param>
public sealed record GetProductByIdQuery(Guid Id) : IRequest<Result<ProductDetailDto>>;

/// <summary>Handler for <see cref="GetProductByIdQuery"/>.</summary>
public sealed class GetProductByIdQueryHandler
    : IRequestHandler<GetProductByIdQuery, Result<ProductDetailDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    /// <summary>Initialises a new instance of <see cref="GetProductByIdQueryHandler"/>.</summary>
    public GetProductByIdQueryHandler(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<Result<ProductDetailDto>> Handle(
        GetProductByIdQuery query,
        CancellationToken cancellationToken)
    {
        Product? product = await _uow.Products.GetByIdAsync(query.Id, cancellationToken);
        if (product is null)
        {
            return Error.NotFound("Product");
        }

        return Result<ProductDetailDto>.Success(_mapper.Map<ProductDetailDto>(product));
    }
}
