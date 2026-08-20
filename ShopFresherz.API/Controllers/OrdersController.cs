using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Order;
using ShopFresherz.Application.Features.Orders.Commands;
using ShopFresherz.Application.Features.Orders.Queries;

namespace ShopFresherz.API.Controllers;

/// <summary>Handles order placement, history, detail, and cancellation.</summary>
[ApiController]
[Route("api/v1/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>Initialises a new instance of <see cref="OrdersController"/>.</summary>
    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Places a new order from the active cart.</summary>
    [AllowAnonymous]
    [HttpPost]
    [ProducesResponseType(typeof(CreateOrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PlaceOrder(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        Result<CreateOrderResponse> result = await _mediator.Send(
            new PlaceOrderCommand(GetUserId(), request), cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetByNumber), new { orderNumber = result.Value!.OrderNumber }, result.Value)
            : MapError(result.Error);
    }

    /// <summary>Returns paginated order history for the authenticated user.</summary>
    [Authorize]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders(
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        Guid userId = GetRequiredUserId();
        Result<PagedResult<OrderDto>> result = await _mediator.Send(
            new GetOrdersQuery(userId, page, pageSize), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Returns full detail of a single order by its order number.</summary>
    [AllowAnonymous]
    [HttpGet("{orderNumber}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByNumber(
        [FromRoute] string orderNumber,
        CancellationToken cancellationToken)
    {
        Result<OrderDto> result = await _mediator.Send(
            new GetOrderByNumberQuery(orderNumber, GetUserId()), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Cancels an order that has not yet been shipped.</summary>
    [Authorize]
    [HttpPost("{orderNumber}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(
        [FromRoute] string orderNumber,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await _mediator.Send(
            new CancelOrderCommand(orderNumber, GetUserId()), cancellationToken);

        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    private Guid? GetUserId()
    {
        string? sub =
            User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub) ??
            User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out Guid id) ? id : null;
    }

    private Guid GetRequiredUserId()
    {
        string? sub =
            User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub) ??
            User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out Guid id) ? id : Guid.Empty;
    }

    private IActionResult MapError(Error error) => error.Code switch
    {
        "NOT_FOUND"   => NotFound(new { error.Code, error.Message }),
        "FORBIDDEN"   => Forbid(),
        "VALIDATION"  => BadRequest(new { error.Code, error.Message }),
        "UNAUTHORIZED"=> Unauthorized(new { error.Code, error.Message }),
        _             => StatusCode(StatusCodes.Status500InternalServerError, new { error.Code, error.Message }),
    };
}
