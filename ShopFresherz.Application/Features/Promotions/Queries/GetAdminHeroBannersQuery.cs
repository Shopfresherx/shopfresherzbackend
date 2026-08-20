using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Promotions;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Promotions.Queries;

/// <summary>Returns all non-deleted hero banners for admin management.</summary>
public sealed record GetAdminHeroBannersQuery
    : IRequest<Result<IReadOnlyList<AdminHeroBannerDto>>>;

/// <summary>Handler for <see cref="GetAdminHeroBannersQuery"/>.</summary>
public sealed class GetAdminHeroBannersQueryHandler
    : IRequestHandler<GetAdminHeroBannersQuery, Result<IReadOnlyList<AdminHeroBannerDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetAdminHeroBannersQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Result<IReadOnlyList<AdminHeroBannerDto>>> Handle(
        GetAdminHeroBannersQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<PromotionalSection> sections = await _uow.PromotionalSections
            .GetAllBySectionKeyAsync("hero", cancellationToken);

        IReadOnlyList<AdminHeroBannerDto> result = sections
            .Select(section => new AdminHeroBannerDto(
                section.Id,
                section.ProductId,
                section.SlugId,
                section.Title,
                section.Tag,
                section.Badge,
                section.CtaText,
                section.ImageUrl,
                section.PriceText,
                section.Slug,
                section.SortOrder,
                section.IsActive,
                section.CreatedAt,
                section.Description))
            .ToList();

        return Result<IReadOnlyList<AdminHeroBannerDto>>.Success(result);
    }
}

/// <summary>Returns all non-deleted flash-sale cards for admin management.</summary>
public sealed record GetAdminFlashSaleQuery
    : IRequest<Result<IReadOnlyList<AdminFlashSaleDto>>>;

/// <summary>Handler for <see cref="GetAdminFlashSaleQuery"/>.</summary>
public sealed class GetAdminFlashSaleQueryHandler
    : IRequestHandler<GetAdminFlashSaleQuery, Result<IReadOnlyList<AdminFlashSaleDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetAdminFlashSaleQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Result<IReadOnlyList<AdminFlashSaleDto>>> Handle(
        GetAdminFlashSaleQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<PromotionalSection> sections = await _uow.PromotionalSections
            .GetAllBySectionKeyAsync("flash-sale", cancellationToken);

        IReadOnlyList<AdminFlashSaleDto> result = sections
            .Select(section => new AdminFlashSaleDto(
                section.Id,
                section.ProductId,
                section.SlugId,
                section.Title,
                section.Tag,
                section.Badge,
                section.CtaText,
                section.ImageUrl,
                section.PriceText,
                section.Slug,
                section.SortOrder,
                section.IsActive,
                section.CreatedAt,
                section.Description,
                section.VideoUrl))
            .ToList();

        return Result<IReadOnlyList<AdminFlashSaleDto>>.Success(result);
    }
}
