using FluentValidation;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.FeaturedSections;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.FeaturedSections.Commands;

/// <summary>Admin command for adding a product to a homepage featured section.</summary>
public sealed record CreateFeaturedSectionCommand(CreateFeaturedSectionRequest Request)
    : IRequest<Result<Guid>>;

/// <summary>Handler for <see cref="CreateFeaturedSectionCommand"/>.</summary>
public sealed class CreateFeaturedSectionCommandHandler
    : IRequestHandler<CreateFeaturedSectionCommand, Result<Guid>>
{
    private readonly IUnitOfWork _uow;

    public CreateFeaturedSectionCommandHandler(IUnitOfWork uow) => _uow = uow;

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(
        CreateFeaturedSectionCommand command,
        CancellationToken cancellationToken)
    {
        CreateFeaturedSectionRequest req = command.Request;

        string section = req.Section.Trim().ToLowerInvariant();
        if (section is not "middle" and not "bottom")
            return Error.Validation("Section must be 'middle' or 'bottom'.");

        Product? product = await _uow.Products.GetByIdAsync(req.ProductId, cancellationToken);
        if (product is null)
            return Error.NotFound("Product");

        string? badge = DiscountBadge(product.CompareAtPrice, product.Price);

        FeaturedSection card = new()
        {
            Section     = section,
            ProductId   = product.Id,
            Title       = product.Name,
            Description = product.ShortDescription ?? string.Empty,
            Slug        = product.Slug,
            ImageUrl    = product.ImageUrls.FirstOrDefault(),
            Price       = $"₦{product.Price:N0}",
            Badge       = badge,
            Tag         = req.Tag?.Trim(),
            CtaText     = req.CtaText?.Trim(),
            SortOrder   = 0,
            IsActive    = true,
        };

        await _uow.FeaturedSections.AddAsync(card, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(card.Id);
    }

    private static string? DiscountBadge(decimal? compareAt, decimal price)
    {
        if (compareAt is null || compareAt <= price) return null;
        int pct = (int)Math.Round((compareAt.Value - price) / compareAt.Value * 100m);
        return pct > 0 ? $"{pct}% OFF" : null;
    }
}

/// <summary>Validator for <see cref="CreateFeaturedSectionCommand"/>.</summary>
public sealed class CreateFeaturedSectionCommandValidator : AbstractValidator<CreateFeaturedSectionCommand>
{
    public CreateFeaturedSectionCommandValidator()
    {
        RuleFor(x => x.Request.Section).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Request.ProductId).NotEqual(Guid.Empty)
            .WithMessage("A valid ProductId is required.");
        RuleFor(x => x.Request.Tag).MaximumLength(200).When(x => x.Request.Tag is not null);
        RuleFor(x => x.Request.CtaText).MaximumLength(100).When(x => x.Request.CtaText is not null);
    }
}
