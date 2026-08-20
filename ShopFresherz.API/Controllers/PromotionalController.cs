using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Promotions;
using ShopFresherz.Application.Features.Promotions.Commands;
using ShopFresherz.Application.Features.Promotions.Queries;

namespace ShopFresherz.API.Controllers;

/// <summary>
/// Returns promotional content for homepage UI sections.
/// Public GET endpoints — no auth required.
/// Admin POST endpoints require the Admin or SuperAdmin role.
/// </summary>
[ApiController]
[Route("api/v1/promotions")]
public sealed class PromotionalController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>Initialises a new instance of <see cref="PromotionalController"/>.</summary>
    public PromotionalController(IMediator mediator) => _mediator = mediator;

    // ── PUBLIC GET ENDPOINTS ──────────────────────────────────────────────────

    /// <summary>
    /// Returns all active hero carousel banners ordered by sort order.
    /// Used for the hero slider at the top of the homepage.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("hero")]
    [ProducesResponseType(typeof(IReadOnlyList<HeroBannerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHeroBanners(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<HeroBannerDto>> result =
            await _mediator.Send(new GetHeroBannersQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(500, result.Error);
    }

    /// <summary>
    /// Returns the single active "Best Deal" featured product card.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("best-deal")]
    [HttpGet("best-deals")]
    [ProducesResponseType(typeof(BestDealCardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBestDealCard(CancellationToken cancellationToken)
    {
        Result<BestDealCardDto?> result =
            await _mediator.Send(new GetBestDealCardQuery(), cancellationToken);
        if (!result.IsSuccess) return StatusCode(500, result.Error);
        return result.Value is null ? NotFound() : Ok(result.Value);
    }

    /// <summary>
    /// Returns the single active promotional section banner.
    /// Displayed below the featured products grid.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("promo-banner")]
    [ProducesResponseType(typeof(PromoBannerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPromoBanner(CancellationToken cancellationToken)
    {
        Result<PromoBannerDto?> result =
            await _mediator.Send(new GetPromoBannerQuery(), cancellationToken);
        if (!result.IsSuccess) return StatusCode(500, result.Error);
        return result.Value is null ? NotFound() : Ok(result.Value);
    }

    /// <summary>
    /// Returns the single active Computer Accessories featured product card.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("accessories-promo")]
    [ProducesResponseType(typeof(FeatureCardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccessoriesPromo(CancellationToken cancellationToken)
    {
        Result<FeatureCardDto?> result =
            await _mediator.Send(new GetAccessoriesPromoQuery(), cancellationToken);
        if (!result.IsSuccess) return StatusCode(500, result.Error);
        return result.Value is null ? NotFound() : Ok(result.Value);
    }

    /// <summary>
    /// Returns the single active bottom promo card shown below the Computer Accessories section.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("laptop-promo")]
    [ProducesResponseType(typeof(LaptopPromoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLaptopPromo(CancellationToken cancellationToken)
    {
        Result<LaptopPromoDto?> result =
            await _mediator.Send(new GetLaptopPromoQuery(), cancellationToken);
        if (!result.IsSuccess) return StatusCode(500, result.Error);
        return result.Value is null ? NotFound() : Ok(result.Value);
    }

    /// <summary>
    /// Returns all active flash-sale cards.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("flash-sale")]
    [ProducesResponseType(typeof(IReadOnlyList<FlashSaleCardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFlashSale(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<FlashSaleCardDto>> result =
            await _mediator.Send(new GetFlashSaleQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(500, result.Error);
    }

    /// <summary>
    /// Returns the single active storefront promo banner.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("store-promo-banner")]
    [ProducesResponseType(typeof(StorePromoBannerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStorePromoBanner(CancellationToken cancellationToken)
    {
        Result<StorePromoBannerDto?> result =
            await _mediator.Send(new GetStorePromoBannerQuery(), cancellationToken);
        if (!result.IsSuccess) return StatusCode(500, result.Error);
        return result.Value is null ? NotFound() : Ok(result.Value);
    }

    // ── ADMIN POST ENDPOINTS ──────────────────────────────────────────────────

    /// <summary>
    /// Adds a new hero carousel banner linked to a product.
    /// Title, image and price are auto-populated from the product record.
    /// Requires Admin role.
    /// </summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpPost("admin/hero")]
    [ProducesResponseType(typeof(AdminHeroBannerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetHeroBanner(
        [FromBody] SetHeroBannerRequest request,
        CancellationToken cancellationToken)
    {
        Result<AdminHeroBannerDto> result =
            await _mediator.Send(new SetHeroBannerCommand(request), cancellationToken);

        if (!result.IsSuccess)
            return result.Error.Code == "NOT_FOUND"
                ? NotFound(new { result.Error.Code, result.Error.Message })
                : MapError(result.Error);

        return Ok(result.Value);
    }

    /// <summary>Returns all active and inactive hero banners for admin management.</summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpGet("admin/hero")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminHeroBannerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAdminHeroBanners(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<AdminHeroBannerDto>> result =
            await _mediator.Send(new GetAdminHeroBannersQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>
    /// Sets the "Best Deal" featured product (replaces any existing one).
    /// Name, image, pricing and rating are auto-populated from the product record.
    /// Requires Admin role.
    /// </summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpPost("admin/best-deal")]
    [ProducesResponseType(typeof(AdminBestDealDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetBestDeal(
        [FromBody] SetBestDealRequest request,
        CancellationToken cancellationToken)
    {
        Result<AdminBestDealDto> result =
            await _mediator.Send(new SetBestDealCommand(request), cancellationToken);

        if (!result.IsSuccess)
            return result.Error.Code == "NOT_FOUND"
                ? NotFound(new { result.Error.Code, result.Error.Message })
                : MapError(result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    /// Sets the promotional section banner (replaces any existing one).
    /// Not linked to a product — all fields are provided manually.
    /// Requires Admin role.
    /// </summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpPost("admin/promo-banner")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    public async Task<IActionResult> SetPromoBanner(
        [FromBody] SetPromoBannerRequest request,
        CancellationToken cancellationToken)
    {
        Result<Guid> result =
            await _mediator.Send(new SetPromoBannerCommand(request), cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetPromoBanner), new { }, new { id = result.Value })
            : StatusCode(500, result.Error);
    }

    /// <summary>
    /// Sets the Computer Accessories featured product card (replaces any existing one).
    /// Title, image and pricing are auto-populated from the product record.
    /// Requires Admin role.
    /// </summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpPost("admin/accessories-promo")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetAccessoriesPromo(
        [FromBody] SetAccessoriesPromoRequest request,
        CancellationToken cancellationToken)
    {
        Result<Guid> result =
            await _mediator.Send(new SetAccessoriesPromoCommand(request), cancellationToken);

        if (!result.IsSuccess)
            return result.Error.Code == "NOT_FOUND"
                ? NotFound(new { result.Error.Code, result.Error.Message })
                : StatusCode(500, result.Error);

        return CreatedAtAction(nameof(GetAccessoriesPromo), new { }, new { id = result.Value });
    }

    /// <summary>
    /// Sets the bottom promo card below Computer Accessories (replaces any existing one).
    /// Title, subtitle, image and pricing are auto-populated from the product record.
    /// Requires Admin role.
    /// </summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpPost("admin/laptop-promo")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetLaptopPromo(
        [FromBody] SetLaptopPromoRequest request,
        CancellationToken cancellationToken)
    {
        Result<Guid> result =
            await _mediator.Send(new SetLaptopPromoCommand(request), cancellationToken);

        if (!result.IsSuccess)
            return result.Error.Code == "NOT_FOUND"
                ? NotFound(new { result.Error.Code, result.Error.Message })
                : StatusCode(500, result.Error);

        return CreatedAtAction(nameof(GetLaptopPromo), new { }, new { id = result.Value });
    }

    /// <summary>
    /// Adds a new flash-sale card, optionally linked to a product.
    /// Requires Admin role.
    /// </summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpPost("admin/flash-sale")]
    [ProducesResponseType(typeof(AdminFlashSaleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetFlashSale(
        [FromBody] SetFlashSaleRequest request,
        CancellationToken cancellationToken)
    {
        Result<AdminFlashSaleDto> result =
            await _mediator.Send(new SetFlashSaleCommand(request), cancellationToken);

        if (!result.IsSuccess)
            return result.Error.Code == "NOT_FOUND"
                ? NotFound(new { result.Error.Code, result.Error.Message })
                : MapError(result.Error);

        return Ok(result.Value);
    }

    /// <summary>Returns all active and inactive flash-sale cards for admin management.</summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpGet("admin/flash-sale")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminFlashSaleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAdminFlashSale(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<AdminFlashSaleDto>> result =
            await _mediator.Send(new GetAdminFlashSaleQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>
    /// Sets (replaces) the storefront promo banner placement, optionally linked to a product.
    /// Requires Admin role.
    /// </summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpPost("admin/store-promo-banner")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetStorePromoBanner(
        [FromBody] SetStorePromoBannerRequest request,
        CancellationToken cancellationToken)
    {
        Result<Guid> result =
            await _mediator.Send(new SetStorePromoBannerCommand(request), cancellationToken);

        if (!result.IsSuccess)
            return result.Error.Code == "NOT_FOUND"
                ? NotFound(new { result.Error.Code, result.Error.Message })
                : MapError(result.Error);

        return CreatedAtAction(nameof(GetStorePromoBanner), new { }, new { id = result.Value });
    }

    // ── ADMIN GET ALL ─────────────────────────────────────────────────────────

    /// <summary>Returns all promotional section records (including deleted) with their GUIDs. Requires Admin role.</summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpGet("admin/all")]
    [ProducesResponseType(typeof(IReadOnlyList<PromotionalSectionAdminDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<PromotionalSectionAdminDto>> result =
            await _mediator.Send(new GetAllPromotionalSectionsQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(500, result.Error);
    }

    // ── ADMIN DELETE ──────────────────────────────────────────────────────────

    /// <summary>Soft-deletes any promotional section record by ID. Requires Admin role.</summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpDelete("admin/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await _mediator.Send(new DeletePromotionalSectionCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    // ── ADMIN PUT ENDPOINTS ───────────────────────────────────────────────────

    /// <summary>Updates a hero carousel banner. Supply a new ProductId to refresh product data. Requires Admin role.</summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpPut("admin/hero/{id:guid}")]
    [ProducesResponseType(typeof(AdminHeroBannerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateHeroBanner(
        [FromRoute] Guid id,
        [FromBody] UpdateHeroBannerRequest request,
        CancellationToken cancellationToken)
    {
        Result<AdminHeroBannerDto> result =
            await _mediator.Send(new UpdateHeroBannerCommand(id, request), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Disables a hero banner without deleting its row. Requires Admin role.</summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpDelete("admin/hero/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DisableHeroBanner(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await _mediator.Send(
            new DisableHeroBannerCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    /// <summary>Updates the Best Deal featured product card. Requires Admin role.</summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpPut("admin/best-deal/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBestDeal(
        [FromRoute] Guid id,
        [FromBody] UpdateBestDealRequest request,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await _mediator.Send(new UpdateBestDealCommand(id, request), cancellationToken);
        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    /// <summary>Updates the full-width promotional banner. Requires Admin role.</summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpPut("admin/promo-banner/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePromoBanner(
        [FromRoute] Guid id,
        [FromBody] UpdatePromoBannerRequest request,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await _mediator.Send(new UpdatePromoBannerCommand(id, request), cancellationToken);
        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    /// <summary>Updates the Computer Accessories featured product card. Requires Admin role.</summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpPut("admin/accessories-promo/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAccessoriesPromo(
        [FromRoute] Guid id,
        [FromBody] UpdateAccessoriesPromoRequest request,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await _mediator.Send(new UpdateAccessoriesPromoCommand(id, request), cancellationToken);
        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    /// <summary>Updates the bottom laptop/promo card. Requires Admin role.</summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpPut("admin/laptop-promo/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLaptopPromo(
        [FromRoute] Guid id,
        [FromBody] UpdateLaptopPromoRequest request,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await _mediator.Send(new UpdateLaptopPromoCommand(id, request), cancellationToken);
        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    /// <summary>Updates a flash-sale card. Supply a new ProductId to refresh product data. Requires Admin role.</summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpPut("admin/flash-sale/{id:guid}")]
    [ProducesResponseType(typeof(AdminFlashSaleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFlashSale(
        [FromRoute] Guid id,
        [FromBody] UpdateFlashSaleRequest request,
        CancellationToken cancellationToken)
    {
        Result<AdminFlashSaleDto> result =
            await _mediator.Send(new UpdateFlashSaleCommand(id, request), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Disables a flash-sale card without deleting its row. Requires Admin role.</summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpDelete("admin/flash-sale/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DisableFlashSale(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await _mediator.Send(new DisableFlashSaleCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    /// <summary>Updates the storefront promo banner placement. Requires Admin role.</summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpPut("admin/store-promo-banner/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStorePromoBanner(
        [FromRoute] Guid id,
        [FromBody] UpdateStorePromoBannerRequest request,
        CancellationToken cancellationToken)
    {
        Result<bool> result =
            await _mediator.Send(new UpdateStorePromoBannerCommand(id, request), cancellationToken);
        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    private IActionResult MapError(Error error) => error.Code switch
    {
        "NOT_FOUND"  => NotFound(new { error.Code, error.Message }),
        "VALIDATION" => BadRequest(new { error.Code, error.Message }),
        _            => StatusCode(500, error),
    };
}
