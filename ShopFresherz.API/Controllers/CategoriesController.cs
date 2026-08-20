using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Product;
using ShopFresherz.Application.Features.Categories.Commands;
using ShopFresherz.Application.Features.Categories.Queries;

namespace ShopFresherz.API.Controllers;

/// <summary>Returns the product category tree for navigation and filtering. Admin: create/edit/delete categories.</summary>
[ApiController]
[Route("api/v1/categories")]
public sealed class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>Initialises a new instance of <see cref="CategoriesController"/>.</summary>
    public CategoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Returns the full active category tree including children.</summary>
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<CategoryDto>> result =
            await _mediator.Send(new GetCategoriesQuery(), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : StatusCode(500);
    }

    /// <summary>
    /// Returns a single category by its URL slug, including its image URL and child categories.
    /// Useful for building category header sections and image galleries.
    /// </summary>
    /// <param name="slug">The URL-safe slug of the category (e.g. "mobile-phones").</param>
    [AllowAnonymous]
    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(CategoryDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlug(
        [FromRoute] string slug,
        CancellationToken cancellationToken)
    {
        Result<CategoryDetailDto> result =
            await _mediator.Send(new GetCategoryBySlugQuery(slug), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Creates a new category. Requires Admin role.</summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        Result<int> result = await _mediator.Send(
            new CreateCategoryCommand(request), cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetAll), new { id = result.Value }, new { id = result.Value })
            : MapError(result.Error);
    }

    /// <summary>Updates an existing category. Requires Admin role.</summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await _mediator.Send(
            new UpdateCategoryCommand(id, request), cancellationToken);

        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    /// <summary>Deletes a category (must have no children or products). Requires Admin role.</summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await _mediator.Send(
            new DeleteCategoryCommand(id), cancellationToken);

        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    private IActionResult MapError(Error error) => error.Code switch
    {
        "NOT_FOUND"  => NotFound(new { error.Code, error.Message }),
        "CONFLICT"   => Conflict(new { error.Code, error.Message }),
        "VALIDATION" => BadRequest(new { error.Code, error.Message }),
        "FORBIDDEN"  => Forbid(),
        _            => StatusCode(StatusCodes.Status500InternalServerError, new { error.Code, error.Message }),
    };
}
