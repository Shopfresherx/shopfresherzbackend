using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Coupons.Commands;

/// <summary>Admin command for disabling a coupon immediately.</summary>
public sealed record DeleteCouponCommand(int Id) : IRequest<Result<bool>>;

/// <summary>Handler for <see cref="DeleteCouponCommand"/>.</summary>
public sealed class DeleteCouponCommandHandler
    : IRequestHandler<DeleteCouponCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;

    public DeleteCouponCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(DeleteCouponCommand command, CancellationToken cancellationToken)
    {
        Coupon? coupon = await _uow.Coupons.GetByIdAsync(command.Id, cancellationToken);
        if (coupon is null)
        {
            return Error.NotFound("Coupon");
        }

        coupon.IsActive = false;
        coupon.ExpiresAt = DateTime.UtcNow;

        _uow.Coupons.Update(coupon);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
