using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Review;
using ShopFresherz.Application.Features.Reviews.Commands;
using ShopFresherz.Application.Features.Reviews.Queries;

namespace ShopFresherz.API.Controllers;

/// <summary>Handles product review submission and retrieval.</summary>
[ApiController]
[Route("api/v1/reviews")]
public sealed class ReviewsController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>Initialises a new instance of <see cref="ReviewsController"/>.</summary>
    public ReviewsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Returns paginated approved reviews for a product.</summary>
    [AllowAnonymous]
    [HttpGet("product/{productId:guid}")]
    [HttpGet("/api/v1/products/{productId:guid}/reviews")]
    [ProducesResponseType(typeof(PagedResult<ReviewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProductReviews(
        [FromRoute] Guid productId,
        [FromQuery] int page       = 1,
        [FromQuery] int pageSize   = 10,
        [FromQuery] int? starFilter = null,
        CancellationToken cancellationToken = default)
    {
        Result<PagedResult<ReviewDto>> result = await _mediator.Send(
            new GetProductReviewsQuery(productId, page, pageSize, starFilter),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : StatusCode(500);
    }

    /// <summary>Submits a new review for a product. Requires authentication.</summary>
    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateReviewRequest request,
        CancellationToken cancellationToken)
    {
        Guid userId = GetUserId();
        Result<Guid> result = await _mediator.Send(
            new CreateReviewCommand(userId, request), cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(Create), new { id = result.Value })
            : MapError(result.Error);
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
        "NOT_FOUND"  => NotFound(new { error.Code, error.Message }),
        "CONFLICT"   => Conflict(new { error.Code, error.Message }),
        "VALIDATION" => BadRequest(new { error.Code, error.Message }),
        _            => StatusCode(StatusCodes.Status500InternalServerError, new { error.Code, error.Message }),
    };
}
