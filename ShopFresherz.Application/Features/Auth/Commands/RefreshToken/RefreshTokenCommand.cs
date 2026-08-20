using FluentValidation;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Auth;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;
using ShopFresherz.Domain.Interfaces.Services;

namespace ShopFresherz.Application.Features.Auth.Commands.RefreshToken;

/// <summary>Command for rotating a JWT access / refresh token pair.</summary>
/// <param name="Request">The refresh payload.</param>
public sealed record RefreshTokenCommand(RefreshTokenRequest Request) : IRequest<Result<AuthResponse>>;

/// <summary>Handler for <see cref="RefreshTokenCommand"/>.</summary>
public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    private readonly IUnitOfWork _uow;
    private readonly ITokenService _tokenService;

    /// <summary>Initialises the handler.</summary>
    public RefreshTokenCommandHandler(IUnitOfWork uow, ITokenService tokenService)
    {
        _uow = uow;
        _tokenService = tokenService;
    }

    /// <inheritdoc />
    public async Task<Result<AuthResponse>> Handle(
        RefreshTokenCommand command,
        CancellationToken cancellationToken)
    {
        Guid? userId = _tokenService.GetUserIdFromExpiredToken(command.Request.AccessToken);
        if (userId is null)
        {
            return Error.Unauthorized("Invalid access token.");
        }

        User? user = await _uow.Users.GetByIdAsync(userId.Value, cancellationToken);
        if (user is null || user.RefreshTokenExpires < DateTime.UtcNow)
        {
            return Error.Unauthorized("Refresh token has expired. Please log in again.");
        }

        string incomingHash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(command.Request.RefreshToken)));

        if (!string.Equals(user.RefreshTokenHash, incomingHash, StringComparison.Ordinal))
        {
            return Error.Unauthorized("Invalid refresh token.");
        }

        string newRefreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshTokenHash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(newRefreshToken)));
        user.RefreshTokenExpires = DateTime.UtcNow.AddDays(7);

        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(cancellationToken);

        string newAccessToken = _tokenService.GenerateAccessToken(user);

        return Result<AuthResponse>.Success(new AuthResponse
        {
            AccessToken  = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt    = DateTime.UtcNow.AddHours(24),
            User = new Dtos.Auth.UserDto
            {
                Id            = user.Id,
                Email         = user.Email,
                FirstName     = user.FirstName,
                LastName      = user.LastName,
                Phone         = user.Phone,
                AvatarUrl     = user.AvatarUrl,
                Role          = user.Role,
                IsVerified    = user.IsVerified,
                LoyaltyPoints = user.LoyaltyPoints,
            },
        });
    }
}

/// <summary>Validator for <see cref="RefreshTokenCommand"/>.</summary>
public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.Request.AccessToken).NotEmpty().WithMessage("Access token is required.");
        RuleFor(x => x.Request.RefreshToken).NotEmpty().WithMessage("Refresh token is required.");
    }
}
