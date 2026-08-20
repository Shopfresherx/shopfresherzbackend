using AutoMapper;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Product;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Products.Queries;

/// <summary>Query for a single product detail page by URL slug.</summary>
/// <param name="Slug">The product's URL slug.</param>
public sealed record GetProductBySlugQuery(string Slug) : IRequest<Result<ProductDetailDto>>;

/// <summary>Handler for <see cref="GetProductBySlugQuery"/>.</summary>
public sealed class GetProductBySlugQueryHandler
    : IRequestHandler<GetProductBySlugQuery, Result<ProductDetailDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    /// <summary>Initialises the handler.</summary>
    public GetProductBySlugQueryHandler(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<Result<ProductDetailDto>> Handle(
        GetProductBySlugQuery query,
        CancellationToken cancellationToken)
    {
        Product? product = await _uow.Products.GetBySlugAsync(query.Slug, cancellationToken);
        if (product is null)
        {
            return Error.NotFound("Product");
        }

        product.ViewCount++;
        _uow.Products.Update(product);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<ProductDetailDto>.Success(_mapper.Map<ProductDetailDto>(product));
    }
}
