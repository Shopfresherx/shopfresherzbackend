using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Banners;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Banners.Commands;

/// <summary>Admin command for creating a homepage banner.</summary>
public sealed record CreateBannerCommand(CreateBannerRequest Request) : IRequest<Result<Guid>>;

/// <summary>Handler for <see cref="CreateBannerCommand"/>.</summary>
public sealed class CreateBannerCommandHandler : IRequestHandler<CreateBannerCommand, Result<Guid>>
{
    private readonly IUnitOfWork _uow;

    public CreateBannerCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateBannerCommand command, CancellationToken cancellationToken)
    {
        CreateBannerRequest request = command.Request;
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.ImageUrl))
        {
            return Error.Validation("Title and image URL are required.");
        }

        HomepageBanner banner = new()
        {
            Title = request.Title.Trim(),
            ImageUrl = request.ImageUrl.Trim(),
            LinkUrl = string.IsNullOrWhiteSpace(request.LinkUrl) ? null : request.LinkUrl.Trim(),
            SubTitle = string.IsNullOrWhiteSpace(request.SubTitle) ? null : request.SubTitle.Trim(),
            CtaText = string.IsNullOrWhiteSpace(request.CtaText) ? null : request.CtaText.Trim(),
            Tag = string.IsNullOrWhiteSpace(request.Tag) ? null : request.Tag.Trim(),
            SortOrder = request.SortOrder,
            IsActive = request.IsActive,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
        };

        await _uow.HomepageBanners.AddAsync(banner, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(banner.Id);
    }
}
