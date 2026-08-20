using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Admin;
using ShopFresherz.Application.Dtos.Banners;
using ShopFresherz.Application.Dtos.Notifications;
using ShopFresherz.Application.Dtos.Product;
using ShopFresherz.Application.Dtos.Review;
using ShopFresherz.Application.Features.Admin.Commands;
using ShopFresherz.Application.Features.Admin.Queries;
using ShopFresherz.Application.Features.Banners.Commands;
using ShopFresherz.Application.Features.Banners.Queries;
using ShopFresherz.Application.Features.Orders.Commands;
using ShopFresherz.Domain.Interfaces.Services;
using System.Security.Claims;
using OrderDto = ShopFresherz.Application.Dtos.Order.OrderDto;

namespace ShopFresherz.API.Controllers;

/// <summary>Provides admin dashboard, order, user, loyalty, and inventory operations.</summary>
[Authorize(Policy = "RequireAdmin")]
[ApiController]
[Route("api/v1/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAuditLogService _auditLog;

    public AdminController(IMediator mediator, IAuditLogService auditLog)
    {
        _mediator = mediator;
        _auditLog = auditLog;
    }

    /// <summary>Returns dashboard statistics.</summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        Result<DashboardStatsDto> result = await _mediator.Send(
            new GetDashboardStatsQuery(), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Returns paginated admin order results.</summary>
    [HttpGet("orders")]
    [ProducesResponseType(typeof(PagedResult<OrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Orders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? paymentStatus = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        Result<PagedResult<OrderDto>> result = await _mediator.Send(
            new GetAllOrdersQuery(page, pageSize, status, paymentStatus, from, to),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Returns paginated admin user results.</summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(PagedResult<AdminUserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Users(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        Result<PagedResult<AdminUserDto>> result = await _mediator.Send(
            new GetAllUsersQuery(page, pageSize, search), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Returns paginated admin customer results (alias for users).</summary>
    [HttpGet("customers")]
    [ProducesResponseType(typeof(PagedResult<AdminUserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Customers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        // Reuse the same query as users
        Result<PagedResult<AdminUserDto>> result = await _mediator.Send(
            new GetAllUsersQuery(page, pageSize, search), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Returns admin analytics data.</summary>
    [HttpGet("analytics")]
    [ProducesResponseType(typeof(AnalyticsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Analytics(CancellationToken cancellationToken)
    {
        Result<AnalyticsDto> result = await _mediator.Send(
            new GetAnalyticsQuery(), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Returns paginated admin review results.</summary>
    [HttpGet("reviews")]
    [ProducesResponseType(typeof(PagedResult<ReviewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reviews(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        Result<PagedResult<ReviewDto>> result = await _mediator.Send(
            new GetAdminReviewsQuery(page, pageSize), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Returns admin notifications with unread count.</summary>
    [HttpGet("notifications")]
    [ProducesResponseType(typeof(NotificationResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Notifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? unreadOnly = null,
        CancellationToken cancellationToken = default)
    {
        Result<NotificationResponseDto> result = await _mediator.Send(
            new GetAdminNotificationsQuery(page, pageSize, unreadOnly), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Returns all admin-configurable store settings.</summary>
    [HttpGet("settings")]
    [ProducesResponseType(typeof(AdminSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Settings(CancellationToken cancellationToken)
    {
        Result<object> result = await _mediator.Send(
            new GetAdminSettingsQuery(), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Returns one admin settings section, such as store, payment, shipping, notifications, seo, or maintenance.</summary>
    [HttpGet("settings/{section}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> SettingsSection(
        [FromRoute] string section,
        CancellationToken cancellationToken)
    {
        Result<object> result = await _mediator.Send(
            new GetAdminSettingsQuery(section), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Updates one or more admin settings sections.</summary>
    [HttpPut("settings")]
    [ProducesResponseType(typeof(AdminSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSettings(
        [FromBody] AdminSettingsUpdateRequest request,
        CancellationToken cancellationToken)
    {
        Result<AdminSettingsDto> result = await _mediator.Send(
            new UpdateAdminSettingsCommand(request), cancellationToken);

        if (result.IsSuccess)
        {
            _ = _auditLog.LogAsync(
                GetUserId(),
                "settings_changed",
                "AppSettings",
                "multiple",
                newValues: System.Text.Json.JsonSerializer.Serialize(request),
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken: CancellationToken.None);
        }

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Replaces one admin settings section independently.</summary>
    [HttpPut("settings/{section}")]
    [ProducesResponseType(typeof(AdminSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSettingsSection(
        [FromRoute] string section,
        [FromBody] AdminSettingsSectionUpdateRequest request,
        CancellationToken cancellationToken)
    {
        Result<AdminSettingsDto> result = await _mediator.Send(
            new UpdateAdminSettingsSectionCommand(section, request.Value), cancellationToken);

        if (result.IsSuccess)
        {
            _ = _auditLog.LogAsync(
                GetUserId(),
                "settings_changed",
                "AppSettings",
                section,
                newValues: request.Value.GetRawText(),
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken: CancellationToken.None);
        }

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Updates one admin settings section independently.</summary>
    [HttpPatch("settings/{section}")]
    [ProducesResponseType(typeof(AdminSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> PatchSettingsSection(
        [FromRoute] string section,
        [FromBody] AdminSettingsSectionUpdateRequest request,
        CancellationToken cancellationToken)
    {
        Result<AdminSettingsDto> result = await _mediator.Send(
            new UpdateAdminSettingsSectionCommand(section, request.Value), cancellationToken);

        if (result.IsSuccess)
        {
            _ = _auditLog.LogAsync(
                GetUserId(),
                "settings_changed",
                "AppSettings",
                section,
                newValues: request.Value.GetRawText(),
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken: CancellationToken.None);
        }

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Updates an order status.</summary>
    [HttpPut("orders/{orderNumber}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateOrderStatus(
        [FromRoute] string orderNumber,
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await _mediator.Send(
            new UpdateOrderStatusCommand(orderNumber, request), cancellationToken);

        if (!result.IsSuccess)
        {
            return MapError(result.Error);
        }

        _ = _auditLog.LogAsync(
            GetUserId(),
            "UpdateOrderStatus",
            "Order",
            orderNumber,
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken: CancellationToken.None);

        return NoContent();
    }

    /// <summary>Cancels an order on the admin's behalf, regardless of ownership.</summary>
    [HttpPost("orders/{orderNumber}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelOrder(
        [FromRoute] string orderNumber,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await _mediator.Send(
            new CancelOrderCommand(orderNumber, RequestingUserId: null), cancellationToken);

        if (!result.IsSuccess)
        {
            return MapError(result.Error);
        }

        _ = _auditLog.LogAsync(
            GetUserId(),
            "CancelOrder",
            "Order",
            orderNumber,
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken: CancellationToken.None);

        return NoContent();
    }

    /// <summary>Adjusts a user's loyalty points balance.</summary>
    [HttpPost("users/{userId:guid}/loyalty")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AdjustUserLoyalty(
        [FromRoute] Guid userId,
        [FromBody] AdjustLoyaltyRequest request,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await _mediator.Send(
            new AdjustUserLoyaltyCommand(userId, request), cancellationToken);

        if (!result.IsSuccess)
        {
            return MapError(result.Error);
        }

        _ = _auditLog.LogAsync(
            GetUserId(),
            "AdjustUserLoyalty",
            "User",
            userId.ToString(),
            newValues: System.Text.Json.JsonSerializer.Serialize(request),
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken: CancellationToken.None);

        return NoContent();
    }

    /// <summary>
    /// Returns a paginated product listing for admin management, of any status.
    /// Unlike the public GET /api/v1/products endpoint (which only ever returns active
    /// products), status defaults to "all" here so admins can see everything.
    /// </summary>
    [HttpGet("products")]
    [ProducesResponseType(typeof(PagedResult<ProductSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAdminProducts(
        [FromQuery] int page          = 1,
        [FromQuery] int pageSize      = 20,
        [FromQuery] string? status    = null,
        [FromQuery] int? categoryId   = null,
        [FromQuery] Guid? brandId     = null,
        [FromQuery] decimal? priceMin = null,
        [FromQuery] decimal? priceMax = null,
        [FromQuery] decimal? ratingMin = null,
        [FromQuery] string? sortBy    = null,
        [FromQuery] string? search    = null,
        CancellationToken cancellationToken = default)
    {
        Result<PagedResult<ProductSummaryDto>> result =
            await _mediator.Send(
                new GetAdminProductsQuery(
                    page, pageSize, status, categoryId, brandId,
                    priceMin, priceMax, ratingMin, sortBy, search),
                cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Returns products at or below the low-stock threshold.</summary>
    [HttpGet("inventory/low-stock")]
    [ProducesResponseType(typeof(IReadOnlyList<LowStockDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> LowStock(
        [FromQuery] int threshold = 5,
        CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<LowStockDto>> result = await _mediator.Send(
            new GetLowStockQuery(threshold), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Returns active homepage banners for admin preview.</summary>
    [HttpGet("banners")]
    [ProducesResponseType(typeof(IReadOnlyList<BannerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBanners(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<BannerDto>> result =
            await _mediator.Send(new GetActiveBannersQuery(), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Creates a homepage banner.</summary>
    [HttpPost("banners")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateBanner(
        [FromBody] CreateBannerRequest request,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await _mediator.Send(new CreateBannerCommand(request), cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetBanners), new { id = result.Value }, new { id = result.Value })
            : MapError(result.Error);
    }

    /// <summary>Updates a homepage banner.</summary>
    [HttpPut("banners/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateBanner(
        [FromRoute] Guid id,
        [FromBody] UpdateBannerRequest request,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await _mediator.Send(new UpdateBannerCommand(id, request), cancellationToken);

        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    /// <summary>Soft-deletes a homepage banner.</summary>
    [HttpDelete("banners/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteBanner(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await _mediator.Send(new DeleteBannerCommand(id), cancellationToken);

        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    private IActionResult MapError(Error error) => error.Code switch
    {
        "NOT_FOUND" => NotFound(new { error.Code, error.Message }),
        "VALIDATION" => BadRequest(new { error.Code, error.Message }),
        "UNAUTHORIZED" => Unauthorized(new { error.Code, error.Message }),
        "FORBIDDEN" => Forbid(),
        _ => StatusCode(StatusCodes.Status500InternalServerError, new { error.Code, error.Message }),
    };

    private Guid? GetUserId()
    {
        string? sub =
            User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub) ??
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(sub, out Guid id) ? id : null;
    }
}
