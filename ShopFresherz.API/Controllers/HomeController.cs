using MediatR;
using Microsoft.AspNetCore.Mvc;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Product;
using ShopFresherz.Application.Features.Home.Queries;

namespace ShopFresherz.API.Controllers;

/// <summary>Home page endpoints (best deals, new arrivals).</summary>
[ApiController]
[Route("api/v1")]
public sealed class HomeController : ControllerBase
{
    private readonly IMediator _mediator;

    public HomeController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Best deals (array of product summaries).
    /// </summary>
    [HttpGet("best-deals")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBestDeals(
        [FromQuery] int limit = 12,
        CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<ProductSummaryDto>> result = await _mediator.Send(
            new GetBestDealsQuery(limit),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>
    /// New arrivals (array of product summaries).
    /// </summary>
    [HttpGet("new-arrivals")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNewArrivals(
        [FromQuery] int limit = 12,
        CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<ProductSummaryDto>> result = await _mediator.Send(
            new GetNewArrivalsQuery(limit),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    private IActionResult MapError(Error error) => error.Code switch
    {
        "NOT_FOUND" => NotFound(new { error.Code, error.Message }),
        "VALIDATION" => BadRequest(new { error.Code, error.Message }),
        "UNAUTHORIZED" => Unauthorized(new { error.Code, error.Message }),
        "FORBIDDEN" => Forbid(),
        _ => StatusCode(StatusCodes.Status500InternalServerError, new { error.Code, error.Message }),
    };
}

