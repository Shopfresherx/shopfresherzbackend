using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Banners;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Banners.Commands;

/// <summary>Admin command for patching a homepage banner.</summary>
public sealed record UpdateBannerCommand(Guid Id, UpdateBannerRequest Request) : IRequest<Result<bool>>;

/// <summary>Handler for <see cref="UpdateBannerCommand"/>.</summary>
public sealed class UpdateBannerCommandHandler : IRequestHandler<UpdateBannerCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;

    public UpdateBannerCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(UpdateBannerCommand command, CancellationToken cancellationToken)
    {
        HomepageBanner? banner = await _uow.HomepageBanners.GetByIdAsync(command.Id, cancellationToken);
        if (banner is null)
        {
            return Error.NotFound("Banner");
        }

        UpdateBannerRequest request = command.Request;
        if (request.Title is not null) banner.Title = request.Title.Trim();
        if (request.ImageUrl is not null) banner.ImageUrl = request.ImageUrl.Trim();
        if (request.LinkUrl is not null) banner.LinkUrl = string.IsNullOrWhiteSpace(request.LinkUrl) ? null : request.LinkUrl.Trim();
        if (request.SubTitle is not null) banner.SubTitle = string.IsNullOrWhiteSpace(request.SubTitle) ? null : request.SubTitle.Trim();
        if (request.CtaText is not null) banner.CtaText = string.IsNullOrWhiteSpace(request.CtaText) ? null : request.CtaText.Trim();
        if (request.Tag is not null) banner.Tag = string.IsNullOrWhiteSpace(request.Tag) ? null : request.Tag.Trim();
        if (request.SortOrder.HasValue) banner.SortOrder = request.SortOrder.Value;
        if (request.IsActive.HasValue) banner.IsActive = request.IsActive.Value;
        if (request.StartsAt.HasValue) banner.StartsAt = request.StartsAt;
        if (request.EndsAt.HasValue) banner.EndsAt = request.EndsAt;

        _uow.HomepageBanners.Update(banner);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
