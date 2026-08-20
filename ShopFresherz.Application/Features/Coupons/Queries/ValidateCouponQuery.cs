using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Coupons;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Coupons.Queries;

/// <summary>Query for validating a coupon against an order subtotal.</summary>
public sealed record ValidateCouponQuery(
    string Code,
    decimal OrderSubtotal,
    Guid? UserId) : IRequest<Result<CouponValidationDto>>;

/// <summary>Handler for <see cref="ValidateCouponQuery"/>.</summary>
public sealed class ValidateCouponQueryHandler
    : IRequestHandler<ValidateCouponQuery, Result<CouponValidationDto>>
{
    private readonly IUnitOfWork _uow;

    public ValidateCouponQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<CouponValidationDto>> Handle(
        ValidateCouponQuery query,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.Code))
        {
            return Error.Validation("Coupon code is required.");
        }

        if (query.OrderSubtotal < 0)
        {
            return Error.Validation("Order subtotal cannot be negative.");
        }

        CouponValidationDto dto = await CouponValidation.ValidateAsync(
            _uow,
            query.Code,
            query.OrderSubtotal,
            query.UserId,
            cancellationToken);

        return Result<CouponValidationDto>.Success(dto);
    }
}
