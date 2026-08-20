using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.FeaturedSections;
using ShopFresherz.Application.Features.FeaturedSections.Commands;
using ShopFresherz.Application.Features.FeaturedSections.Queries;

namespace ShopFresherz.API.Controllers;

/// <summary>
/// Public endpoints for homepage featured section cards (middle × 2, bottom × 1).
/// Admin endpoints for managing featured section cards.
/// </summary>
[ApiController]
[Route("api/v1/featured-sections")]
public sealed class FeaturedSectionsController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>Initialises a new instance of <see cref="FeaturedSectionsController"/>.</summary>
    public FeaturedSectionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ── Public endpoints ────────────────────────────────────────────────────

    /// <summary>Returns up to 2 active cards for the middle homepage section.</summary>
    [HttpGet("middle")]
    [ProducesResponseType(typeof(IReadOnlyList<FeaturedSectionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMiddle(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<FeaturedSectionDto>> result = await _mediator.Send(
            new GetFeaturedSectionQuery("middle", 2), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : StatusCode(500);
    }

    /// <summary>Returns 1 active card for the bottom homepage section.</summary>
    [HttpGet("bottom")]
    [ProducesResponseType(typeof(IReadOnlyList<FeaturedSectionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBottom(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<FeaturedSectionDto>> result = await _mediator.Send(
            new GetFeaturedSectionQuery("bottom", 1), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : StatusCode(500);
    }

    // ── Admin endpoints ─────────────────────────────────────────────────────

    /// <summary>Returns all featured section cards. Requires Admin role.</summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FeaturedSectionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<FeaturedSectionDto>> result = await _mediator.Send(
            new GetAllFeaturedSectionsQuery(), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : StatusCode(500);
    }

    /// <summary>Creates a new featured section card. Requires Admin role.</summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateFeaturedSectionRequest request,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await _mediator.Send(
            new CreateFeaturedSectionCommand(request), cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetAll), new { id = result.Value }, new { id = result.Value })
            : MapError(result.Error);
    }

    /// <summary>Updates a featured section card. Requires Admin role.</summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateFeaturedSectionRequest request,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await _mediator.Send(
            new UpdateFeaturedSectionCommand(id, request), cancellationToken);

        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    /// <summary>Removes a featured section card. Requires Admin role.</summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await _mediator.Send(
            new DeleteFeaturedSectionCommand(id), cancellationToken);

        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private IActionResult MapError(Error error) => error.Code switch
    {
        "NOT_FOUND"  => NotFound(new { error.Code, error.Message }),
        "CONFLICT"   => Conflict(new { error.Code, error.Message }),
        "VALIDATION" => BadRequest(new { error.Code, error.Message }),
        "FORBIDDEN"  => Forbid(),
        _            => StatusCode(StatusCodes.Status500InternalServerError, new { error.Code, error.Message }),
    };
}
