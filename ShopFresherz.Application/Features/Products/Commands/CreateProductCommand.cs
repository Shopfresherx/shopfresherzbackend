using FluentValidation;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Product;
using ShopFresherz.Application.Features.Search.Commands;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;
using ShopFresherz.Domain.Interfaces.Services;

namespace ShopFresherz.Application.Features.Products.Commands;

/// <summary>Command for creating a new product (admin only).</summary>
/// <param name="Request">The product creation payload.</param>
public sealed record CreateProductCommand(CreateProductRequest Request) : IRequest<Result<Guid>>;

/// <summary>Handler for <see cref="CreateProductCommand"/>.</summary>
public sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    private readonly IUnitOfWork _uow;
    private readonly ISearchService _search;

    /// <summary>Initialises the handler.</summary>
    public CreateProductCommandHandler(IUnitOfWork uow, ISearchService search)
    {
        _uow = uow;
        _search = search;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        CreateProductRequest req = command.Request;

        string slug = string.IsNullOrWhiteSpace(req.Slug)
            ? GenerateSlug(req.Name)
            : req.Slug.Trim().ToLowerInvariant();

        if (await _uow.Products.SlugExistsAsync(slug, cancellationToken))
        {
            return Error.Conflict($"A product with slug '{slug}' already exists.");
        }

        Product product = new()
        {
            SKU = req.SKU.Trim().ToUpperInvariant(),
            Name = req.Name.Trim(),
            Slug = slug,
            BrandId = req.BrandId,
            CategoryId = req.CategoryId,
            Description = req.Description,
            ShortDescription = req.ShortDescription,
            Price = req.Price,
            CompareAtPrice = req.CompareAtPrice,
            CostPrice = req.CostPrice,
            StockQty = req.StockQty,
            WeightKg = req.WeightKg,
            AttributesJson = req.AttributesJson,
            TagsJson = req.TagsJson,
            IsActive = req.IsActive,
            IsFeatured = req.IsFeatured,
            // CreateProductRequest currently does not expose MetaTitle/MetaDescription in this codebase.
            // Persist meta SEO fields only if/when the DTO supports them.

            ImageUrls = req.ImageUrls, // Set the image URLs
            AverageRating = req.InitialRating,
            ReviewCount = req.InitialReviewCount,
        };

        await _uow.Products.AddAsync(product, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        ProductSearchDocument doc = IndexProductCommandHandler.Map(product);
        _ = Task.Run(() => _search.IndexProductAsync(doc, CancellationToken.None), CancellationToken.None);

        return Result<Guid>.Success(product.Id);
    }

    private static string GenerateSlug(string name)
    {
        string lower = name.Trim().ToLowerInvariant();
        System.Text.StringBuilder sb = new();
        foreach (char c in lower)
        {
            if (char.IsLetterOrDigit(c)) sb.Append(c);
            else if (c == ' ') sb.Append('-');
        }
        return sb.ToString().Trim('-');
    }
}

/// <summary>Validator for <see cref="CreateProductCommand"/>.</summary>
public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Request.SKU).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Request.BrandId).NotEmpty();
        RuleFor(x => x.Request.CategoryId).GreaterThan(0);
        RuleFor(x => x.Request.Price).GreaterThan(0);
        RuleFor(x => x.Request.StockQty).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.InitialRating).InclusiveBetween(0, 5)
            .WithMessage("InitialRating must be between 0 and 5.");

        // imageUrls: Array of Strings (URLs)
        RuleFor(x => x.Request.ImageUrls)
            .NotNull();

        // Allow empty array, but disallow invalid URL entries.
        RuleForEach(x => x.Request.ImageUrls)
            .NotEmpty()
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Each imageUrl must be a valid absolute URL.");
    }
}

