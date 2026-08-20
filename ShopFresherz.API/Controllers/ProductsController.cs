using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Product;
using ShopFresherz.Application.Features.Products.Commands;
using ShopFresherz.Application.Features.Products.Queries;

namespace ShopFresherz.API.Controllers;

/// <summary>Handles product catalog browsing and admin CRUD operations.</summary>
[ApiController]
[Route("api/v1/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>Initialises a new instance of <see cref="ProductsController"/>.</summary>
    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Returns a paginated product listing with optional filters.</summary>
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int page       = 1,
        [FromQuery] int pageSize   = 20,
        [FromQuery] int? categoryId  = null,
        [FromQuery] Guid? brandId    = null,
        [FromQuery] decimal? priceMin  = null,
        [FromQuery] decimal? priceMax  = null,
        [FromQuery] decimal? ratingMin = null,
        [FromQuery] string? sortBy     = null,
        CancellationToken cancellationToken = default)
    {
        Result<PagedResult<ProductSummaryDto>> result =
            await _mediator.Send(
                new GetProductsQuery(page, pageSize, categoryId, brandId, priceMin, priceMax, ratingMin, sortBy),
                cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Returns full product detail for the product detail page (PDP).</summary>
    [AllowAnonymous]
    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlug(
        [FromRoute] string slug,
        CancellationToken cancellationToken)
    {
        Result<ProductDetailDto> result =
            await _mediator.Send(new GetProductBySlugQuery(slug), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Creates a new product. Requires Admin or SuperAdmin role.</summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        Result<Guid> result =
            await _mediator.Send(new CreateProductCommand(request), cancellationToken);

        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, new { id = result.Value })
            : MapError(result.Error);
    }

    /// <summary>Updates an existing product. Requires Admin or SuperAdmin role.</summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        Result<bool> result =
            await _mediator.Send(new UpdateProductCommand(id, request), cancellationToken);

        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    /// <summary>Soft-deletes a product. Requires Admin or SuperAdmin role.</summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        Result<bool> result =
            await _mediator.Send(new DeleteProductCommand(id), cancellationToken);

        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    /// <summary>
    /// Uploads a product image, generates 4 WebP zoom derivatives
    /// (80px thumb, 540px display, 1600px zoom, original),
    /// and stores all URLs. Requires Admin role.
    /// Max file size: 8MB. Min dimensions: 800x800px.
    /// </summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpPost("{id:guid}/images")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ProductImageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadImage(
        [FromRoute] Guid id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { Code = "VALIDATION", Message = "No file uploaded." });
        }

        using Stream stream = file.OpenReadStream();

        Result<ProductImageDto> result = await _mediator.Send(
            new UploadProductImageCommand(id, stream, file.FileName, file.Length),
            cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetBySlug), new { slug = string.Empty }, result.Value)
            : MapError(result.Error);
    }

    private IActionResult MapError(Error error) => error.Code switch
    {
        "NOT_FOUND"  => NotFound(new { error.Code, error.Message }),
        "CONFLICT"   => Conflict(new { error.Code, error.Message }),
        "VALIDATION" => BadRequest(new { error.Code, error.Message }),
        _            => StatusCode(StatusCodes.Status500InternalServerError, new { error.Code, error.Message }),
    };
}
