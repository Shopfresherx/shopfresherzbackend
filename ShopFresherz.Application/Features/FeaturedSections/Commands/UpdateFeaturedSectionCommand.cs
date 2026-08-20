using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.FeaturedSections;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.FeaturedSections.Commands;

/// <summary>Admin command for updating a featured section card.</summary>
public sealed record UpdateFeaturedSectionCommand(Guid Id, UpdateFeaturedSectionRequest Request)
    : IRequest<Result<bool>>;

/// <summary>Handler for <see cref="UpdateFeaturedSectionCommand"/>.</summary>
public sealed class UpdateFeaturedSectionCommandHandler
    : IRequestHandler<UpdateFeaturedSectionCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;

    public UpdateFeaturedSectionCommandHandler(IUnitOfWork uow) => _uow = uow;

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        UpdateFeaturedSectionCommand command,
        CancellationToken cancellationToken)
    {
        FeaturedSection? card = await _uow.FeaturedSections.GetByIdAsync(command.Id, cancellationToken);
        if (card is null) return Error.NotFound("Featured section card");

        UpdateFeaturedSectionRequest req = command.Request;

        // If a new product was supplied, refresh all denormalised fields.
        if (req.ProductId.HasValue && req.ProductId.Value != Guid.Empty)
        {
            Product? product = await _uow.Products.GetByIdAsync(req.ProductId.Value, cancellationToken);
            if (product is null) return Error.NotFound("Product");

            string? badge = DiscountBadge(product.CompareAtPrice, product.Price);

            card.ProductId   = product.Id;
            card.Title       = product.Name;
            card.Description = product.ShortDescription ?? string.Empty;
            card.Slug        = product.Slug;
            card.ImageUrl    = product.ImageUrls.FirstOrDefault();
            card.Price       = $"₦{product.Price:N0}";
            card.Badge       = badge;
        }

        if (req.Tag is not null)     card.Tag     = req.Tag.Trim();
        if (req.CtaText is not null) card.CtaText = req.CtaText.Trim();
        if (req.IsActive.HasValue)   card.IsActive = req.IsActive.Value;

        card.UpdatedAt = DateTime.UtcNow;
        _uow.FeaturedSections.Update(card);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }

    private static string? DiscountBadge(decimal? compareAt, decimal price)
    {
        if (compareAt is null || compareAt <= price) return null;
        int pct = (int)Math.Round((compareAt.Value - price) / compareAt.Value * 100m);
        return pct > 0 ? $"{pct}% OFF" : null;
    }
}
