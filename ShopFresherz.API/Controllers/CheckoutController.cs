using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Checkout;
using ShopFresherz.Application.Features.Checkout.Commands.ConfirmOrder;
using ShopFresherz.Application.Features.Checkout.Commands.InitiatePayment;

namespace ShopFresherz.API.Controllers;

/// <summary>
/// Two-step inline-popup checkout: initiate-payment creates a Draft order and returns
/// Flutterwave inline config (or bank details); confirm-order verifies the transaction
/// and finalises the order. Both endpoints support authenticated users and guests.
/// </summary>
[ApiController]
[Route("api/v1/checkout")]
public sealed class CheckoutController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>Initialises a new instance of <see cref="CheckoutController"/>.</summary>
    public CheckoutController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Validates the cart, creates a Draft order, and returns payment configuration.</summary>
    [AllowAnonymous]
    [HttpPost("initiate-payment")]
    [ProducesResponseType(typeof(InitiatePaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InitiatePayment(
        [FromBody] InitiatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        Result<InitiatePaymentResponse> result = await _mediator.Send(
            new InitiatePaymentCommand(GetUserId(), request), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Verifies the Flutterwave transaction and finalises the Draft order.</summary>
    [AllowAnonymous]
    [HttpPost("confirm-order")]
    [ProducesResponseType(typeof(ConfirmOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmOrder(
        [FromBody] ConfirmOrderRequest request,
        CancellationToken cancellationToken)
    {
        Result<ConfirmOrderResponse> result = await _mediator.Send(
            new ConfirmOrderCommand(GetUserId(), request), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    private Guid? GetUserId()
    {
        string? sub =
            User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub) ??
            User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out Guid id) ? id : null;
    }

    private IActionResult MapError(Error error) => error.Code switch
    {
        "NOT_FOUND"    => NotFound(new { error.Code, error.Message }),
        "CONFLICT"     => Conflict(new { error.Code, error.Message }),
        "UNAUTHORIZED" => Unauthorized(new { error.Code, error.Message }),
        "FORBIDDEN"    => Forbid(),
        "VALIDATION"   => BadRequest(new { error.Code, error.Message }),
        _              => StatusCode(StatusCodes.Status500InternalServerError, new { error.Code, error.Message }),
    };
}
