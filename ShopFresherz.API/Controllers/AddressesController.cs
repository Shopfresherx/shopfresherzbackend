using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Profile;
using ShopFresherz.Application.Features.Addresses.Commands;
using ShopFresherz.Application.Features.Addresses.Queries;

namespace ShopFresherz.API.Controllers;

/// <summary>Manages the authenticated user's saved delivery addresses.</summary>
[Authorize]
[ApiController]
[Route("api/v1/addresses")]
public sealed class AddressesController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>Initialises a new instance of <see cref="AddressesController"/>.</summary>
    public AddressesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Returns all saved addresses for the authenticated user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AddressDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAddresses(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<AddressDto>> result = await _mediator.Send(
            new GetAddressesQuery(GetUserId()), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : StatusCode(500);
    }

    /// <summary>Saves a new delivery address to the user's profile.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAddressRequest request,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await _mediator.Send(
            new CreateAddressCommand(GetUserId(), request), cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetAddresses), new { id = result.Value })
            : MapError(result.Error);
    }

    /// <summary>Updates a saved delivery address by ID.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateAddressRequest request,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await _mediator.Send(
            new UpdateAddressCommand(GetUserId(), id, request), cancellationToken);

        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    /// <summary>Soft-deletes a saved address by ID.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await _mediator.Send(
            new DeleteAddressCommand(GetUserId(), id), cancellationToken);

        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    private Guid GetUserId()
    {
        string? sub =
            User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub) ??
            User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out Guid id) ? id : Guid.Empty;
    }

    private IActionResult MapError(Error error) => error.Code switch
    {
        "NOT_FOUND" => NotFound(new { error.Code, error.Message }),
        "FORBIDDEN" => Forbid(),
        "VALIDATION"=> BadRequest(new { error.Code, error.Message }),
        _           => StatusCode(StatusCodes.Status500InternalServerError, new { error.Code, error.Message }),
    };
}
