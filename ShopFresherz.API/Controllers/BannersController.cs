using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Banners;
using ShopFresherz.Application.Features.Banners.Queries;

namespace ShopFresherz.API.Controllers;

/// <summary>Public homepage banner endpoints.</summary>
[ApiController]
[Route("api/v1/banners")]
public sealed class BannersController : ControllerBase
{
    private readonly IMediator _mediator;

    public BannersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Returns active homepage banners for the storefront carousel.</summary>
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BannerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<BannerDto>> result =
            await _mediator.Send(new GetActiveBannersQuery(), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : StatusCode(StatusCodes.Status500InternalServerError, result.Error);
    }
}
