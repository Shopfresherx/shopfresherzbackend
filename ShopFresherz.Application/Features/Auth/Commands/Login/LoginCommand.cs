using AutoMapper;
using FluentValidation;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Auth;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;
using ShopFresherz.Domain.Interfaces.Services;

namespace ShopFresherz.Application.Features.Auth.Commands.Login;

/// <summary>Command for authenticating an existing user.</summary>
/// <param name="Request">The login payload.</param>
public sealed record LoginCommand(LoginRequest Request) : IRequest<Result<AuthResponse>>;

/// <summary>Handler for <see cref="LoginCommand"/>.</summary>
public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;

    /// <summary>Initialises the handler with required services.</summary>
    public LoginCommandHandler(
        IUnitOfWork uow,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IMapper mapper)
    {
        _uow = uow;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<Result<AuthResponse>> Handle(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        User? user = await _uow.Users.GetByEmailAsync(
            command.Request.Email, cancellationToken);

        if (user is null || !_passwordHasher.Verify(command.Request.Password, user.PasswordHash))
        {
            return Error.Unauthorized("Invalid email or password.");
        }

        string refreshToken = _tokenService.GenerateRefreshToken();
        string refreshTokenHash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(refreshToken)));

        user.RefreshTokenHash = refreshTokenHash;
        user.RefreshTokenExpires = DateTime.UtcNow.AddDays(7);
        user.LastLoginAt = DateTime.UtcNow;

        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(cancellationToken);

        string accessToken = _tokenService.GenerateAccessToken(user);

        AuthResponse response = new()
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            User = _mapper.Map<UserDto>(user)
        };

        return Result<AuthResponse>.Success(response);
    }
}

/// <summary>FluentValidation rules for <see cref="LoginCommand"/>.</summary>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public LoginCommandValidator()
    {
        RuleFor(x => x.Request.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Request.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}
