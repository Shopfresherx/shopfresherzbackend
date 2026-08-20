using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Coupons;
using ShopFresherz.Application.Features.Coupons.Commands;
using ShopFresherz.Application.Features.Coupons.Queries;

namespace ShopFresherz.API.Controllers;

/// <summary>Manages coupon validation and admin coupon operations.</summary>
[ApiController]
[Route("api/v1/coupons")]
public sealed class CouponsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CouponsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Returns all coupons. Requires Admin role.</summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CouponDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<CouponDto>> result = await _mediator.Send(
            new GetAllCouponsQuery(), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Validates a coupon against an order subtotal.</summary>
    [HttpGet("validate")]
    [ProducesResponseType(typeof(CouponValidationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Validate(
        [FromQuery] string code,
        [FromQuery] decimal orderSubtotal,
        [FromQuery] Guid? userId,
        CancellationToken cancellationToken)
    {
        Result<CouponValidationDto> result = await _mediator.Send(
            new ValidateCouponQuery(code, orderSubtotal, userId), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Creates a coupon. Requires Admin role.</summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCouponRequest request,
        CancellationToken cancellationToken)
    {
        Result<int> result = await _mediator.Send(
            new CreateCouponCommand(request), cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(Validate), new { code = request.Code, orderSubtotal = 0m }, new { id = result.Value })
            : MapError(result.Error);
    }

    /// <summary>Updates a coupon. Requires Admin role.</summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] UpdateCouponRequest request,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await _mediator.Send(
            new UpdateCouponCommand(id, request), cancellationToken);

        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    /// <summary>Disables a coupon immediately. Requires Admin role.</summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await _mediator.Send(
            new DeleteCouponCommand(id), cancellationToken);

        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    private IActionResult MapError(Error error) => error.Code switch
    {
        "NOT_FOUND" => NotFound(new { error.Code, error.Message }),
        "CONFLICT" => Conflict(new { error.Code, error.Message }),
        "VALIDATION" => BadRequest(new { error.Code, error.Message }),
        "UNAUTHORIZED" => Unauthorized(new { error.Code, error.Message }),
        "FORBIDDEN" => Forbid(),
        _ => StatusCode(StatusCodes.Status500InternalServerError, new { error.Code, error.Message }),
    };
}
