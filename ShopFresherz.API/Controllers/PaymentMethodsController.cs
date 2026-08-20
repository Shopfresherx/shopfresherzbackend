using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.PaymentMethods;
using ShopFresherz.Application.Features.PaymentMethods.Commands;
using ShopFresherz.Application.Features.PaymentMethods.Queries;

namespace ShopFresherz.API.Controllers;

/// <summary>Manages the authenticated user's saved bank cards / payment methods.</summary>
[Authorize]
[ApiController]
[Route("api/v1/payment-methods")]
public sealed class PaymentMethodsController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>Initialises a new instance of <see cref="PaymentMethodsController"/>.</summary>
    public PaymentMethodsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Returns all saved payment methods for the authenticated user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PaymentMethodDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<PaymentMethodDto>> result = await _mediator.Send(
            new GetPaymentMethodsQuery(GetUserId()), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Saves a new bank card to the user's profile.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePaymentMethodRequest request,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await _mediator.Send(
            new CreatePaymentMethodCommand(GetUserId(), request), cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetAll), new { id = result.Value }, new { id = result.Value })
            : MapError(result.Error);
    }

    /// <summary>Updates a saved bank card (e.g., set as default or update expiry).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdatePaymentMethodRequest request,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await _mediator.Send(
            new UpdatePaymentMethodCommand(GetUserId(), id, request), cancellationToken);

        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    /// <summary>Removes a saved bank card.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await _mediator.Send(
            new DeletePaymentMethodCommand(GetUserId(), id), cancellationToken);

        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private Guid GetUserId()
    {
        string? sub =
            User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub) ??
            User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out Guid id) ? id : Guid.Empty;
    }

    private IActionResult MapError(Error error) => error.Code switch
    {
        "NOT_FOUND"    => NotFound(new { error.Code, error.Message }),
        "FORBIDDEN"    => Forbid(),
        "VALIDATION"   => BadRequest(new { error.Code, error.Message }),
        "UNAUTHORIZED" => Unauthorized(new { error.Code, error.Message }),
        _              => StatusCode(StatusCodes.Status500InternalServerError, new { error.Code, error.Message }),
    };
}
