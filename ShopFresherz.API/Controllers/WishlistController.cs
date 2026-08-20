using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Product;
using ShopFresherz.Application.Features.Wishlist.Commands;
using ShopFresherz.Application.Features.Wishlist.Queries;

namespace ShopFresherz.API.Controllers;

/// <summary>Manages the authenticated user's product wishlist.</summary>
[Authorize]
[ApiController]
[Route("api/v1/wishlist")]
public sealed class WishlistController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>Initialises a new instance of <see cref="WishlistController"/>.</summary>
    public WishlistController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Returns all products on the authenticated user's wishlist.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProductSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWishlist(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<ProductSummaryDto>> result = await _mediator.Send(
            new GetWishlistQuery(GetUserId()), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : StatusCode(500);
    }

    /// <summary>Adds a product to the wishlist.</summary>
    [HttpPost("{productId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Add(
        [FromRoute] Guid productId,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await _mediator.Send(
            new AddToWishlistCommand(GetUserId(), productId), cancellationToken);

        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    /// <summary>Removes a product from the wishlist.</summary>
    [HttpDelete("{productId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remove(
        [FromRoute] Guid productId,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await _mediator.Send(
            new RemoveFromWishlistCommand(GetUserId(), productId), cancellationToken);

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
        _           => StatusCode(StatusCodes.Status500InternalServerError, new { error.Code, error.Message }),
    };
}
