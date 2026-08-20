using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Promotions.Queries;

/// <summary>Admin DTO representing a promotional section record with its full identity.</summary>
public sealed record PromotionalSectionAdminDto(
    Guid Id,
    Guid? ProductId,
    string SectionKey,
    string SlugId,
    string? Slug,
    string? Title,
    string? Tag,
    string? Badge,
    string? CtaText,
    string? ImageUrl,
    int SortOrder,
    bool IsActive,
    DateTime CreatedAt,
    string? Description = null,
    string? VideoUrl = null);

/// <summary>Admin query returning all promotional section records (active + deleted).</summary>
public sealed record GetAllPromotionalSectionsQuery : IRequest<Result<IReadOnlyList<PromotionalSectionAdminDto>>>;

public sealed class GetAllPromotionalSectionsQueryHandler
    : IRequestHandler<GetAllPromotionalSectionsQuery, Result<IReadOnlyList<PromotionalSectionAdminDto>>>
{
    private readonly IUnitOfWork _uow;
    public GetAllPromotionalSectionsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Result<IReadOnlyList<PromotionalSectionAdminDto>>> Handle(
        GetAllPromotionalSectionsQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<PromotionalSection> all =
            await _uow.PromotionalSections.GetAllAsync(cancellationToken);

        IReadOnlyList<PromotionalSectionAdminDto> dtos = all
            .Select(p => new PromotionalSectionAdminDto(
                p.Id, p.ProductId, p.SectionKey, p.SlugId, p.Slug, p.Title,
                p.Tag, p.Badge, p.CtaText, p.ImageUrl, p.SortOrder, p.IsActive, p.CreatedAt,
                p.Description))
            .ToList();

        return Result<IReadOnlyList<PromotionalSectionAdminDto>>.Success(dtos);
    }
}
