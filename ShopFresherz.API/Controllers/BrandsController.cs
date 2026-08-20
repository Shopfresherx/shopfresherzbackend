using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Product;
using ShopFresherz.Application.Features.Brands.Queries;

namespace ShopFresherz.API.Controllers;

/// <summary>Returns the active brand list for filtering and display.</summary>
[ApiController]
[Route("api/v1/brands")]
public sealed class BrandsController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>Initialises a new instance of <see cref="BrandsController"/>.</summary>
    public BrandsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Returns all active brands.</summary>
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BrandDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<BrandDto>> result =
            await _mediator.Send(new GetBrandsQuery(), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : StatusCode(500);
    }
}
