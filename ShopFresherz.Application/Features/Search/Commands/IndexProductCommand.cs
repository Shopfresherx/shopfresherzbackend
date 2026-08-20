using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;
using ShopFresherz.Domain.Interfaces.Services;

namespace ShopFresherz.Application.Features.Search.Commands;

/// <summary>Command for indexing a product into the search backend.</summary>
public sealed record IndexProductCommand(Guid ProductId) : IRequest<Result<bool>>;

/// <summary>Handler for <see cref="IndexProductCommand"/>.</summary>
public sealed class IndexProductCommandHandler : IRequestHandler<IndexProductCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;
    private readonly ISearchService _search;

    public IndexProductCommandHandler(IUnitOfWork uow, ISearchService search)
    {
        _uow = uow;
        _search = search;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(IndexProductCommand command, CancellationToken cancellationToken)
    {
        Product? product = await _uow.Products.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            return Result<bool>.Success(true);
        }

        ProductSearchDocument doc = Map(product);

        try
        {
            await _search.IndexProductAsync(doc, cancellationToken);
        }
        catch
        {
            return Result<bool>.Success(true);
        }

        return Result<bool>.Success(true);
    }

    public static ProductSearchDocument Map(Product product) => new()
    {
        Id = product.Id.ToString(),
        Name = product.Name,
        Slug = product.Slug,
        Brand = product.Brand?.Name,
        Category = product.Category?.Name,
        Description = product.Description ?? product.ShortDescription,
        Tags = product.TagsJson,
        Price = product.Price,
        CompareAtPrice = product.CompareAtPrice,
        AverageRating = product.AverageRating,
        StockQty = product.StockQty,
        SoldCount = product.SoldCount,
        IsActive = product.IsActive,
        ThumbUrl = product.Images?.OrderBy(i => i.SortOrder).FirstOrDefault()?.ThumbUrl,
        CreatedAt = product.CreatedAt,
    };
}
