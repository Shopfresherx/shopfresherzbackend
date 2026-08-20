using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Banners.Commands;

/// <summary>Admin command for soft-deleting a homepage banner.</summary>
public sealed record DeleteBannerCommand(Guid Id) : IRequest<Result<bool>>;

/// <summary>Handler for <see cref="DeleteBannerCommand"/>.</summary>
public sealed class DeleteBannerCommandHandler : IRequestHandler<DeleteBannerCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;

    public DeleteBannerCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(DeleteBannerCommand command, CancellationToken cancellationToken)
    {
        HomepageBanner? banner = await _uow.HomepageBanners.GetByIdAsync(command.Id, cancellationToken);
        if (banner is null)
        {
            return Error.NotFound("Banner");
        }

        banner.DeletedAt = DateTime.UtcNow;
        _uow.HomepageBanners.Update(banner);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
