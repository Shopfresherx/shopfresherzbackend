using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Notifications;
using ShopFresherz.Application.Dtos.Profile;
using ShopFresherz.Application.Features.Account.Commands;
using ShopFresherz.Application.Features.Account.Queries;
using ShopFresherz.Application.Features.Notifications;

namespace ShopFresherz.API.Controllers;

/// <summary>Manages authenticated customer account operations.</summary>
[Authorize]
[ApiController]
[Route("api/v1/account")]
public sealed class AccountController : ControllerBase
{
    private readonly IMediator _mediator;

    public AccountController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Returns the authenticated user's profile.</summary>
    [HttpGet("profile")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        Result<UserProfileDto> result = await _mediator.Send(
            new GetProfileQuery(GetRequiredUserId()), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Updates the authenticated user's profile.</summary>
    [HttpPut("profile")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await _mediator.Send(
            new UpdateProfileCommand(GetRequiredUserId(), request), cancellationToken);

        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    /// <summary>Changes the authenticated user's password.</summary>
    [HttpPut("password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await _mediator.Send(
            new ChangePasswordCommand(GetRequiredUserId(), request), cancellationToken);

        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    /// <summary>Returns the user's loyalty balance and transaction history.</summary>
    [HttpGet("loyalty")]
    [ProducesResponseType(typeof(LoyaltyDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLoyalty(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        Result<LoyaltyDto> result = await _mediator.Send(
            new GetLoyaltyQuery(GetRequiredUserId(), page, pageSize), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Registers the authenticated user for a back-in-stock notification.</summary>
    [HttpPost("notify-me/{productId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> NotifyMe(
        [FromRoute] Guid productId,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await _mediator.Send(
            new RegisterNotifyRequestCommand(GetRequiredUserId(), productId),
            cancellationToken);

        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    /// <summary>Returns the user's notification preferences.</summary>
    [HttpGet("notifications")]
    [ProducesResponseType(typeof(NotificationPreferencesDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotificationPreferences(CancellationToken cancellationToken)
    {
        Result<NotificationPreferencesDto> result = await _mediator.Send(
            new GetNotificationPreferencesQuery(GetRequiredUserId()), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Updates the user's notification preferences.</summary>
    [HttpPut("notifications")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateNotificationPreferences(
        [FromBody] NotificationPreferencesDto preferences,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await _mediator.Send(
            new UpdateNotificationPreferencesCommand(GetRequiredUserId(), preferences), cancellationToken);

        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    private Guid GetRequiredUserId()
    {
        string? sub =
            User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub) ??
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(sub, out Guid id) ? id : Guid.Empty;
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
