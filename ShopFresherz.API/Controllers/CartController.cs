using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Cart;
using ShopFresherz.Application.Dtos.Coupons;
using ShopFresherz.Application.Features.Cart.Commands;
using ShopFresherz.Application.Features.Cart.Queries;

namespace ShopFresherz.API.Controllers;

/// <summary>Manages the shopping cart for authenticated users and guests.</summary>
[ApiController]
[Route("api/v1/cart")]
public sealed class CartController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>Initialises a new instance of <see cref="CartController"/>.</summary>
    public CartController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Returns the active cart for the current user or guest session.</summary>
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCart(
        [FromHeader(Name = "X-Session-Id")] string? sessionId,
        CancellationToken cancellationToken)
    {
        Result<CartDto> result = await _mediator.Send(
            new GetCartQuery(GetUserId(), sessionId), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : StatusCode(500);
    }

    /// <summary>Adds a product to the cart. Supports guests (via X-Session-Id header) and authenticated users.</summary>
    [AllowAnonymous]
    [HttpPost("items")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddItem(
        [FromBody] AddToCartRequest request,
        [FromHeader(Name = "X-Session-Id")] string? sessionId,
        CancellationToken cancellationToken)
    {
        Result<CartDto> result = await _mediator.Send(
            new AddToCartCommand(GetUserId(), sessionId, request), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Updates the quantity of a specific cart line item.</summary>
    [AllowAnonymous]
    [HttpPut("items/{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateItem(
        [FromRoute] Guid itemId,
        [FromBody] UpdateCartItemRequest request,
        [FromHeader(Name = "X-Session-Id")] string? sessionId,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await _mediator.Send(
            new UpdateCartItemCommand(GetUserId(), sessionId, itemId, request.Quantity),
            cancellationToken);

        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    /// <summary>Removes a line item from the cart.</summary>
    [AllowAnonymous]
    [HttpDelete("items/{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveItem(
        [FromRoute] Guid itemId,
        [FromHeader(Name = "X-Session-Id")] string? sessionId,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await _mediator.Send(
            new RemoveFromCartCommand(GetUserId(), sessionId, itemId), cancellationToken);

        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    /// <summary>Applies a coupon to the active cart.</summary>
    [AllowAnonymous]
    [HttpPost("coupon")]
    [ProducesResponseType(typeof(CouponValidationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApplyCoupon(
        [FromQuery] string couponCode,
        [FromHeader(Name = "X-Session-Id")] string? sessionId,
        CancellationToken cancellationToken)
    {
        Result<CouponValidationDto> result = await _mediator.Send(
            new ApplyCouponCommand(GetUserId(), sessionId, couponCode),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Removes any applied coupon from the active cart.</summary>
    [AllowAnonymous]
    [HttpDelete("coupon")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveCoupon(
        [FromHeader(Name = "X-Session-Id")] string? sessionId,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await _mediator.Send(
            new RemoveCouponCommand(GetUserId(), sessionId),
            cancellationToken);

        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    private Guid? GetUserId()
    {
        string? sub =
            User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub) ??
            User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out Guid id) ? id : null;
    }

    private IActionResult MapError(Error error) => error.Code switch
    {
        "NOT_FOUND"  => NotFound(new { error.Code, error.Message }),
        "VALIDATION" => BadRequest(new { error.Code, error.Message }),
        _            => StatusCode(StatusCodes.Status500InternalServerError, new { error.Code, error.Message }),
    };
}
