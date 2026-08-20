using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.FeaturedSections;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.FeaturedSections.Queries;

/// <summary>Query for the active cards in a homepage section.</summary>
/// <param name="Section">The section name ("middle" or "bottom").</param>
/// <param name="Limit">Maximum number of cards to return.</param>
public sealed record GetFeaturedSectionQuery(string Section, int Limit)
    : IRequest<Result<IReadOnlyList<FeaturedSectionDto>>>;

/// <summary>Handler for <see cref="GetFeaturedSectionQuery"/>.</summary>
public sealed class GetFeaturedSectionQueryHandler
    : IRequestHandler<GetFeaturedSectionQuery, Result<IReadOnlyList<FeaturedSectionDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetFeaturedSectionQueryHandler(IUnitOfWork uow) => _uow = uow;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<FeaturedSectionDto>>> Handle(
        GetFeaturedSectionQuery query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<FeaturedSection> cards =
            await _uow.FeaturedSections.GetActiveBySectionAsync(query.Section, cancellationToken);

        IReadOnlyList<FeaturedSectionDto> dtos = cards
            .Take(query.Limit)
            .Select(ToDto)
            .ToList();

        return Result<IReadOnlyList<FeaturedSectionDto>>.Success(dtos);
    }

    internal static FeaturedSectionDto ToDto(FeaturedSection card) => new()
    {
        Id          = card.Id,
        Section     = card.Section,
        ProductId   = card.ProductId,
        Title       = card.Title,
        Description = card.Description,
        Slug        = card.Slug,
        ImageUrl    = card.ImageUrl,
        Price       = card.Price,
        Badge       = card.Badge,
        Tag         = card.Tag,
        CtaText     = card.CtaText,
    };
}
