using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.FlashDeals;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.FlashDeals.Commands;

/// <summary>Admin command for updating a flash deal.</summary>
public sealed record UpdateFlashDealCommand(Guid Id, UpdateFlashDealRequest Request)
    : IRequest<Result<bool>>;

/// <summary>Handler for <see cref="UpdateFlashDealCommand"/>.</summary>
public sealed class UpdateFlashDealCommandHandler
    : IRequestHandler<UpdateFlashDealCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;

    public UpdateFlashDealCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        UpdateFlashDealCommand command,
        CancellationToken cancellationToken)
    {
        FlashDeal? deal = await _uow.FlashDeals.GetByIdAsync(command.Id, cancellationToken);
        if (deal is null)
        {
            return Error.NotFound("Flash deal");
        }

        decimal salePrice = command.Request.SalePrice ?? deal.SalePrice;
        DateTime startsAt = command.Request.StartsAt ?? deal.StartsAt;
        DateTime endsAt = command.Request.EndsAt ?? deal.EndsAt;
        int? maxQuantity = command.Request.MaxQuantity ?? deal.MaxQuantity;

        if (salePrice <= 0) return Error.Validation("Sale price must be greater than zero.");
        if (endsAt <= startsAt) return Error.Validation("End date must be after start date.");
        if (maxQuantity.HasValue && maxQuantity.Value <= 0) return Error.Validation("Max quantity must be greater than zero.");
        if (salePrice >= deal.OriginalPrice) return Error.Validation("Sale price must be less than the product price.");

        bool hasOverlap = await _uow.FlashDeals.HasOverlapAsync(
            deal.ProductId,
            deal.VariantId,
            startsAt,
            endsAt,
            deal.Id,
            cancellationToken);

        if (hasOverlap)
        {
            return Error.Conflict("An overlapping active flash deal already exists for this product.");
        }

        deal.SalePrice = salePrice;
        deal.StartsAt = startsAt;
        deal.EndsAt = endsAt;
        deal.MaxQuantity = maxQuantity;
        if (command.Request.IsActive.HasValue)
        {
            deal.IsActive = command.Request.IsActive.Value;
        }

        _uow.FlashDeals.Update(deal);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
