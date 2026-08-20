using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Coupons;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Coupons.Commands;

/// <summary>Admin command for updating coupon lifecycle and limits.</summary>
public sealed record UpdateCouponCommand(int Id, UpdateCouponRequest Request)
    : IRequest<Result<bool>>;

/// <summary>Handler for <see cref="UpdateCouponCommand"/>.</summary>
public sealed class UpdateCouponCommandHandler
    : IRequestHandler<UpdateCouponCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;

    public UpdateCouponCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        UpdateCouponCommand command,
        CancellationToken cancellationToken)
    {
        Coupon? coupon = await _uow.Coupons.GetByIdAsync(command.Id, cancellationToken);
        if (coupon is null)
        {
            return Error.NotFound("Coupon");
        }

        if (command.Request.MaxUses.HasValue && command.Request.MaxUses.Value <= 0)
        {
            return Error.Validation("Max uses must be greater than zero.");
        }

        if (command.Request.MaxUsesPerUser.HasValue && command.Request.MaxUsesPerUser.Value <= 0)
        {
            return Error.Validation("Max uses per user must be greater than zero.");
        }

        if (command.Request.IsActive.HasValue) coupon.IsActive = command.Request.IsActive.Value;
        if (command.Request.ExpiresAt.HasValue) coupon.ExpiresAt = command.Request.ExpiresAt;
        if (command.Request.MaxUses.HasValue) coupon.MaxUses = command.Request.MaxUses;
        if (command.Request.MaxUsesPerUser.HasValue) coupon.MaxUsesPerUser = command.Request.MaxUsesPerUser;

        _uow.Coupons.Update(coupon);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
