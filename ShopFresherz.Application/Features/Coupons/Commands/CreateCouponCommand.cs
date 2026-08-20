using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Coupons;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Coupons.Commands;

/// <summary>Admin command for creating a coupon.</summary>
public sealed record CreateCouponCommand(CreateCouponRequest Request)
    : IRequest<Result<int>>;

/// <summary>Handler for <see cref="CreateCouponCommand"/>.</summary>
public sealed class CreateCouponCommandHandler
    : IRequestHandler<CreateCouponCommand, Result<int>>
{
    private readonly IUnitOfWork _uow;

    public CreateCouponCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<int>> Handle(
        CreateCouponCommand command,
        CancellationToken cancellationToken)
    {
        CreateCouponRequest request = command.Request;
        string code = request.Code.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(code) || code.Length > 50)
        {
            return Error.Validation("Coupon code is required and must be 50 characters or fewer.");
        }

        if (request.Value <= 0)
        {
            return Error.Validation("Coupon value must be greater than zero.");
        }

        if (request.MaxUses.HasValue && request.MaxUses.Value <= 0)
        {
            return Error.Validation("Max uses must be greater than zero.");
        }

        if (request.MaxUsesPerUser.HasValue && request.MaxUsesPerUser.Value <= 0)
        {
            return Error.Validation("Max uses per user must be greater than zero.");
        }

        if (await _uow.Coupons.CodeExistsAsync(code, cancellationToken))
        {
            return Error.Conflict("Coupon code already exists.");
        }

        Coupon coupon = new()
        {
            Code = code,
            Type = request.Type,
            Value = request.Value,
            MinimumOrderAmount = request.MinimumOrderAmount,
            MaxUses = request.MaxUses,
            MaxUsesPerUser = request.MaxUsesPerUser,
            IsStackable = request.IsStackable,
            ExpiresAt = request.ExpiresAt,
            RestrictToProductId = request.RestrictToProductId,
            RestrictToCategoryId = request.RestrictToCategoryId,
            IsActive = true,
        };

        await _uow.Coupons.AddAsync(coupon, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(coupon.Id);
    }
}
