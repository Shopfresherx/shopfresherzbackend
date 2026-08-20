using AutoMapper;
using FluentValidation;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Auth;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;
using ShopFresherz.Domain.Interfaces.Services;
using System.Text.RegularExpressions;

namespace ShopFresherz.Application.Features.Auth.Commands.Register;

/// <summary>Command for registering a new customer account.</summary>
/// <param name="Request">The registration payload.</param>
public sealed record RegisterCommand(RegisterRequest Request) : IRequest<Result<AuthResponse>>;

/// <summary>Handler for <see cref="RegisterCommand"/>.</summary>
public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResponse>>
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly ICacheService _cacheService;
    private readonly IMapper _mapper;

    /// <summary>Initialises the handler with required services.</summary>
    public RegisterCommandHandler(
        IUnitOfWork uow,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IEmailService emailService,
        ISmsService smsService,
        ICacheService cacheService,
        IMapper mapper)
    {
        _uow = uow;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _emailService = emailService;
        _smsService = smsService;
        _cacheService = cacheService;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<Result<AuthResponse>> Handle(
        RegisterCommand command,
        CancellationToken cancellationToken)
    {
        RegisterRequest req = command.Request;

        bool emailExists = await _uow.Users.EmailExistsAsync(req.Email, cancellationToken);
        if (emailExists)
        {
            return Error.Conflict("An account with this email address already exists.");
        }

        if (!string.IsNullOrWhiteSpace(req.Phone))
        {
            bool phoneExists = await _uow.Users.PhoneExistsAsync(req.Phone, cancellationToken);
            if (phoneExists)
            {
                return Error.Conflict("An account with this phone number already exists.");
            }
        }

        User user = new()
        {
            Email = req.Email.Trim().ToLowerInvariant(),
            FirstName = req.FirstName.Trim(),
            LastName = req.LastName.Trim(),
            Phone = string.IsNullOrWhiteSpace(req.Phone) ? null : req.Phone.Trim(),
            PasswordHash = _passwordHasher.Hash(req.Password),
            IsVerified = false
        };

        await _uow.Users.AddAsync(user, cancellationToken);

        string refreshToken = _tokenService.GenerateRefreshToken();
        string refreshTokenHash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(refreshToken)));

        user.RefreshTokenHash = refreshTokenHash;
        user.RefreshTokenExpires = DateTime.UtcNow.AddDays(7);
        user.LastLoginAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync(cancellationToken);

        // Generate and cache OTP for email verification (10-minute TTL).
        string otp = GenerateOtp();
        await _cacheService.SetAsync(
            $"otp:verify:{user.Email}",
            otp,
            TimeSpan.FromMinutes(10),
            cancellationToken);

        // Fire-and-forget email (do not await to avoid blocking checkout).
        _ = _emailService.SendOtpAsync(user.Email, user.FirstName, otp, CancellationToken.None);
        _ = _emailService.SendWelcomeAsync(user.Email, user.FirstName, CancellationToken.None);
        if (!string.IsNullOrWhiteSpace(user.Phone))
        {
            _ = _smsService.SendOtpAsync(user.Phone, otp, CancellationToken.None);
        }

        string accessToken = _tokenService.GenerateAccessToken(user);

        AuthResponse response = new()
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            User = _mapper.Map<UserDto>(user)
        };

        return Result<AuthResponse>.Success(response);
    }

    private static string GenerateOtp() =>
        Random.Shared.Next(100_000, 999_999).ToString();
}

/// <summary>FluentValidation rules for <see cref="RegisterCommand"/>.</summary>
public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Request.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.Request.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

        RuleFor(x => x.Request.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Request.Phone)
            .Matches(@"^\+?[1-9]\d{6,14}$").WithMessage("Phone number must be in international format.")
            .When(x => !string.IsNullOrWhiteSpace(x.Request.Phone));

        RuleFor(x => x.Request.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"\d").WithMessage("Password must contain at least one digit.");

        RuleFor(x => x.Request.ConfirmPassword)
            .Equal(x => x.Request.Password).WithMessage("Passwords do not match.");
    }
}
