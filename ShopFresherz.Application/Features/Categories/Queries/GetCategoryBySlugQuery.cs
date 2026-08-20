using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Product;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Categories.Queries;

/// <summary>Full category detail DTO including children and image.</summary>
public sealed class CategoryDetailDto
{
    /// <summary>Gets or sets the category's integer primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the parent category ID.</summary>
    public int? ParentId { get; set; }

    /// <summary>Gets or sets the category display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the URL slug.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Gets or sets the CDN URL for the category image.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Gets or sets the sort order.</summary>
    public int SortOrder { get; set; }

    /// <summary>Gets or sets whether this category is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets the SEO meta title.</summary>
    public string? MetaTitle { get; set; }

    /// <summary>Gets or sets the SEO meta description.</summary>
    public string? MetaDescription { get; set; }

    /// <summary>Gets or sets the child categories.</summary>
    public IReadOnlyList<CategoryDto> Children { get; set; } = new List<CategoryDto>();

    /// <summary>Gets or sets when this category was created.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>Query for retrieving a single category by its URL slug.</summary>
/// <param name="Slug">The URL-safe slug of the category to retrieve.</param>
public sealed record GetCategoryBySlugQuery(string Slug)
    : IRequest<Result<CategoryDetailDto>>;

/// <summary>Handler for <see cref="GetCategoryBySlugQuery"/>.</summary>
public sealed class GetCategoryBySlugQueryHandler
    : IRequestHandler<GetCategoryBySlugQuery, Result<CategoryDetailDto>>
{
    private readonly IUnitOfWork _uow;

    /// <summary>Initialises the handler.</summary>
    public GetCategoryBySlugQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<CategoryDetailDto>> Handle(
        GetCategoryBySlugQuery query,
        CancellationToken cancellationToken)
    {
        Category? category = await _uow.Categories.GetBySlugAsync(
            query.Slug.ToLowerInvariant(), cancellationToken);

        if (category is null)
        {
            return Error.NotFound($"Category '{query.Slug}'");
        }

        CategoryDetailDto dto = new()
        {
            Id          = category.Id,
            ParentId    = category.ParentId,
            Name        = category.Name,
            Slug        = category.Slug,
            ImageUrl    = category.ImageUrl,
            SortOrder   = category.SortOrder,
            IsActive    = category.IsActive,
            MetaTitle   = category.MetaTitle,
            MetaDescription = category.MetaDescription,
            CreatedAt   = category.CreatedAt,
            Children    = category.Children
                .Select(c => new CategoryDto
                {
                    Id       = c.Id,
                    ParentId = c.ParentId,
                    Name     = c.Name,
                    Slug     = c.Slug,
                    ImageUrl = c.ImageUrl,
                })
                .ToList(),
        };

        return Result<CategoryDetailDto>.Success(dto);
    }
}
