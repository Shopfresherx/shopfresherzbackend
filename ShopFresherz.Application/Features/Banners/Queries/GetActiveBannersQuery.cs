using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Banners;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Banners.Queries;

/// <summary>Query for active homepage banners.</summary>
public sealed record GetActiveBannersQuery : IRequest<Result<IReadOnlyList<BannerDto>>>;

/// <summary>Handler for <see cref="GetActiveBannersQuery"/>.</summary>
public sealed class GetActiveBannersQueryHandler
    : IRequestHandler<GetActiveBannersQuery, Result<IReadOnlyList<BannerDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetActiveBannersQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<BannerDto>>> Handle(
        GetActiveBannersQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<HomepageBanner> active =
            await _uow.HomepageBanners.GetActiveAsync(cancellationToken);

        IReadOnlyList<BannerDto> banners = active.Select(ToDto).ToList();

        return Result<IReadOnlyList<BannerDto>>.Success(banners);
    }

    internal static BannerDto ToDto(HomepageBanner banner) => new(
        banner.Id,
        banner.Title,
        banner.ImageUrl,
        banner.LinkUrl,
        banner.SubTitle,
        banner.CtaText,
        banner.Tag,
        banner.SortOrder);
}
