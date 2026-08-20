using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.FlashDeals.Commands;

/// <summary>Admin command for enabling or disabling a flash deal.</summary>
public sealed record ToggleFlashDealCommand(Guid Id, bool IsActive)
    : IRequest<Result<bool>>;

/// <summary>Handler for <see cref="ToggleFlashDealCommand"/>.</summary>
public sealed class ToggleFlashDealCommandHandler
    : IRequestHandler<ToggleFlashDealCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;

    public ToggleFlashDealCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        ToggleFlashDealCommand command,
        CancellationToken cancellationToken)
    {
        FlashDeal? deal = await _uow.FlashDeals.GetByIdAsync(command.Id, cancellationToken);
        if (deal is null)
        {
            return Error.NotFound("Flash deal");
        }

        deal.IsActive = command.IsActive;
        _uow.FlashDeals.Update(deal);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
