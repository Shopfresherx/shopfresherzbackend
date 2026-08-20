using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Coupons;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Coupons.Queries;

/// <summary>Admin query for all coupons.</summary>
public sealed record GetAllCouponsQuery : IRequest<Result<IReadOnlyList<CouponDto>>>;

/// <summary>Handler for <see cref="GetAllCouponsQuery"/>.</summary>
public sealed class GetAllCouponsQueryHandler
    : IRequestHandler<GetAllCouponsQuery, Result<IReadOnlyList<CouponDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetAllCouponsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CouponDto>>> Handle(
        GetAllCouponsQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Coupon> coupons = await _uow.Coupons.GetAllAsync(cancellationToken);

        IReadOnlyList<CouponDto> dtos = coupons.Select(c => new CouponDto
        {
            Id                  = c.Id,
            Code                = c.Code,
            Type                = c.Type,
            Value               = c.Value,
            MinimumOrderAmount  = c.MinimumOrderAmount,
            MaxUses             = c.MaxUses,
            UsedCount           = c.UsedCount,
            MaxUsesPerUser      = c.MaxUsesPerUser,
            IsStackable         = c.IsStackable,
            ExpiresAt           = c.ExpiresAt,
            IsActive            = c.IsActive,
            RestrictToProductId = c.RestrictToProductId,
            RestrictToCategoryId = c.RestrictToCategoryId,
            CreatedAt           = c.CreatedAt,
        }).ToList();

        return Result<IReadOnlyList<CouponDto>>.Success(dtos);
    }
}
