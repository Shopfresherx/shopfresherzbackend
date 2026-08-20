using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.FlashDeals;
using ShopFresherz.Application.Features.FlashDeals.Commands;
using ShopFresherz.Application.Features.FlashDeals.Queries;

namespace ShopFresherz.API.Controllers;

/// <summary>Manages public and admin flash deal operations.</summary>
[ApiController]
[Route("api/v1/flash-deals")]
public sealed class FlashDealsController : ControllerBase
{
    private readonly IMediator _mediator;

    public FlashDealsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Returns currently active flash deals.</summary>
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FlashDealDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<FlashDealDto>> result = await _mediator.Send(
            new GetActiveFlashDealsQuery(), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Creates a flash deal. Requires Admin role.</summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreateFlashDealRequest request,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await _mediator.Send(
            new CreateFlashDealCommand(request), cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetActive), new { id = result.Value }, new { id = result.Value })
            : MapError(result.Error);
    }

    /// <summary>Updates a flash deal. Requires Admin role.</summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateFlashDealRequest request,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await _mediator.Send(
            new UpdateFlashDealCommand(id, request), cancellationToken);

        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    /// <summary>Enables or disables a flash deal. Requires Admin role.</summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpPatch("{id:guid}/toggle")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Toggle(
        [FromRoute] Guid id,
        [FromQuery] bool isActive,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await _mediator.Send(
            new ToggleFlashDealCommand(id, isActive), cancellationToken);

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
