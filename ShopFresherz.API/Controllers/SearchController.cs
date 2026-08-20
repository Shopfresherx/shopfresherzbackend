using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Features.Search.Queries;
using ShopFresherz.Domain.Interfaces.Services;

namespace ShopFresherz.API.Controllers;

/// <summary>Handles full-text product search and instant search suggestions.</summary>
[ApiController]
[Route("api/v1/search")]
public sealed class SearchController : ControllerBase
{
    private readonly IMediator _mediator;

    public SearchController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Searches active products with optional filters and sorting.</summary>
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(SearchResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery(Name = "q")] string? query = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] decimal? priceMin = null,
        [FromQuery] decimal? priceMax = null,
        [FromQuery] decimal? ratingMin = null,
        [FromQuery] bool? inStockOnly = null,
        [FromQuery] string? sortBy = null,
        CancellationToken cancellationToken = default)
    {
        Result<SearchResult> result = await _mediator.Send(
            new SearchProductsQuery(query, page, pageSize, priceMin, priceMax, ratingMin, inStockOnly, sortBy),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Returns quick product and category suggestions for typeahead search.</summary>
    [AllowAnonymous]
    [HttpGet("instant")]
    [ResponseCache(Duration = 120, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(typeof(InstantSearchResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Instant(
        [FromQuery(Name = "q")] string query,
        CancellationToken cancellationToken)
    {
        Result<InstantSearchResult> result = await _mediator.Send(
            new InstantSearchQuery(query),
            cancellationToken);

        Response.Headers.CacheControl = "public, max-age=120";
        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    private IActionResult MapError(Error error) => error.Code switch
    {
        "VALIDATION" => BadRequest(new { error.Code, error.Message }),
        _ => StatusCode(StatusCodes.Status500InternalServerError, new { error.Code, error.Message }),
    };
}
