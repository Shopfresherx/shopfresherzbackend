using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Auth;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Enums;
using ShopFresherz.Domain.Interfaces;
using ShopFresherz.Domain.Interfaces.Services;
using System.Security.Cryptography;
using System.Text;

namespace ShopFresherz.Application.Features.Auth.Commands.GoogleSignIn;

public sealed record GoogleSignInCommand(string IdToken) : IRequest<Result<AuthResponse>>;

public sealed class GoogleSignInCommandHandler
    : IRequestHandler<GoogleSignInCommand, Result<AuthResponse>>
{
    private readonly IUnitOfWork _uow;
    private readonly IGoogleTokenValidator _googleTokenValidator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;
    private readonly ILogger<GoogleSignInCommandHandler> _logger;

    public GoogleSignInCommandHandler(
        IUnitOfWork uow,
        IGoogleTokenValidator googleTokenValidator,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IMapper mapper,
        ILogger<GoogleSignInCommandHandler> logger)
    {
        _uow = uow;
        _googleTokenValidator = googleTokenValidator;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<AuthResponse>> Handle(
        GoogleSignInCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!_googleTokenValidator.IsConfigured)
            {
                _logger.LogWarning("Google Sign-In was requested before GoogleAuth:ClientId was configured.");
                return Error.Internal("Google Sign-In is not yet configured. Please try again later.");
            }

            GoogleIdentity? identity = await _googleTokenValidator.ValidateAsync(
                command.IdToken,
                cancellationToken);

            if (identity is null ||
                !identity.EmailVerified ||
                string.IsNullOrWhiteSpace(identity.Email) ||
                string.IsNullOrWhiteSpace(identity.Subject))
            {
                return Error.Unauthorized("Invalid Google token.");
            }

            string email = identity.Email.Trim().ToLowerInvariant();
            User? user = await _uow.Users.GetByGoogleIdAsync(identity.Subject, cancellationToken);
            bool isNewUser = false;

            if (user is null)
            {
                user = await _uow.Users.GetByEmailAsync(email, cancellationToken);
                if (user is not null)
                {
                    user.GoogleId = identity.Subject;
                    user.IsVerified = true;
                }
            }

            if (user is null)
            {
                user = new User
                {
                    Email = email,
                    FirstName = string.IsNullOrWhiteSpace(identity.GivenName) ? "Customer" : identity.GivenName.Trim(),
                    LastName = identity.FamilyName?.Trim() ?? string.Empty,
                    GoogleId = identity.Subject,
                    Role = UserRole.Customer,
                    IsVerified = true,
                    PasswordHash = _passwordHasher.Hash(Convert.ToHexString(RandomNumberGenerator.GetBytes(32))),
                };

                await _uow.Users.AddAsync(user, cancellationToken);
                isNewUser = true;
            }

            string refreshToken = _tokenService.GenerateRefreshToken();
            user.RefreshTokenHash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
            user.RefreshTokenExpires = DateTime.UtcNow.AddDays(7);
            user.LastLoginAt = DateTime.UtcNow;

            // Only mark existing users as Modified. The Id is assigned client-side
            // (Guid.NewGuid() in BaseEntity), so calling Update() on a just-added user
            // flips its tracking state from Added to Modified — EF then emits an UPDATE
            // for a row that does not exist yet, affecting 0 rows and throwing
            // DbUpdateConcurrencyException. New users are already tracked as Added, and
            // existing users loaded via the repository are tracked, so their field
            // changes are persisted by SaveChanges either way.
            if (!isNewUser)
            {
                _uow.Users.Update(user);
            }

            await _uow.SaveChangesAsync(cancellationToken);

            return Result<AuthResponse>.Success(new AuthResponse
            {
                AccessToken = _tokenService.GenerateAccessToken(user),
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                User = _mapper.Map<UserDto>(user),
            });
        }
        catch (InvalidGoogleTokenException ex)
        {
            _logger.LogWarning(ex, "Invalid Google JWT");
            return Error.Unauthorized("Invalid Google token.");
        }
        catch (Exception ex) when (IsUniqueConstraintViolation(ex))
        {
            _logger.LogWarning(
                ex,
                "Google Sign-In could not persist the user because the email or Google ID already exists.");
            return Error.Conflict(
                "An account with this email or Google identity already exists. Please sign in again.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error during Google Sign-In. TokenPrefix: {Prefix}",
                command.IdToken?.Length > 20 ? command.IdToken[..20] + "..." : "short");
            return Error.Internal("Google Sign-In failed. Please try again.");
        }
    }

    private static bool IsUniqueConstraintViolation(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current.GetType().FullName != "Microsoft.Data.SqlClient.SqlException")
            {
                continue;
            }

            object? number = current.GetType().GetProperty("Number")?.GetValue(current);
            return number is 2601 or 2627;
        }

        return false;
    }
}

public sealed class GoogleSignInCommandValidator : AbstractValidator<GoogleSignInCommand>
{
    public GoogleSignInCommandValidator()
    {
        RuleFor(x => x.IdToken).NotEmpty().WithMessage("Google ID token is required.");
    }
}
