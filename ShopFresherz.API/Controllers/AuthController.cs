using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Auth;
using ShopFresherz.Application.Features.Auth.Commands.ForgotPassword;
using ShopFresherz.Application.Features.Auth.Commands.GoogleSignIn;
using ShopFresherz.Application.Features.Auth.Commands.Login;
using ShopFresherz.Application.Features.Auth.Commands.Logout;
using ShopFresherz.Application.Features.Auth.Commands.RefreshToken;
using ShopFresherz.Application.Features.Auth.Commands.Register;
using ShopFresherz.Application.Features.Auth.Commands.ResetPassword;
using ShopFresherz.Application.Features.Auth.Commands.VerifyEmail;

namespace ShopFresherz.API.Controllers;

/// <summary>Handles user registration, login, token refresh, and password recovery.</summary>
[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>Initialises a new instance of <see cref="AuthController"/>.</summary>
    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Registers a new customer account and returns access + refresh tokens.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        Result<AuthResponse> result =
            await _mediator.Send(new RegisterCommand(request), cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(Register), result.Value)
            : MapError(result.Error);
    }

    /// <summary>Authenticates an existing user and returns access + refresh tokens.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        Result<AuthResponse> result =
            await _mediator.Send(new LoginCommand(request), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Authenticates or creates a customer using a Google ID token.</summary>
    [AllowAnonymous]
    [HttpPost("google")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GoogleSignIn(
        [FromBody] GoogleSignInRequest request,
        CancellationToken cancellationToken)
    {
        Result<AuthResponse> result = await _mediator.Send(
            new GoogleSignInCommand(request.IdToken), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Rotates the access and refresh token pair using the supplied refresh token.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        Result<AuthResponse> result =
            await _mediator.Send(new RefreshTokenCommand(request), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Verifies the user's email using a 6-digit OTP sent at registration.</summary>
    [HttpPost("verify-email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyOtpRequest request,
        CancellationToken cancellationToken)
    {
        Result<bool> result =
            await _mediator.Send(new VerifyEmailCommand(request), cancellationToken);

        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    /// <summary>Sends a password-reset OTP to the supplied email address.</summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new ForgotPasswordCommand(request), cancellationToken);
        return NoContent();
    }

    /// <summary>Completes a password reset using the OTP and new password.</summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        Result<bool> result =
            await _mediator.Send(new ResetPasswordCommand(request), cancellationToken);

        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    /// <summary>Revokes the current user's refresh token (server-side logout).</summary>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        Guid userId = GetUserId();
        await _mediator.Send(new LogoutCommand(userId), cancellationToken);
        return NoContent();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private Guid GetUserId()
    {
        string? sub =
            User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub) ??
            User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out Guid id) ? id : Guid.Empty;
    }

    private IActionResult MapError(Error error) => error.Code switch
    {
        "NOT_FOUND"   => NotFound(new { error.Code, error.Message }),
        "CONFLICT"    => Conflict(new { error.Code, error.Message }),
        "UNAUTHORIZED"=> Unauthorized(new { error.Code, error.Message }),
        "FORBIDDEN"   => Forbid(),
        "VALIDATION"  => BadRequest(new { error.Code, error.Message }),
        _             => StatusCode(StatusCodes.Status500InternalServerError, new { error.Code, error.Message }),
    };
}
