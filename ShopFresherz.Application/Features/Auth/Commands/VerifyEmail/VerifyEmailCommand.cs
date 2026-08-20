using FluentValidation;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;
using ShopFresherz.Domain.Interfaces.Services;

namespace ShopFresherz.Application.Features.Auth.Commands.VerifyEmail;

/// <summary>Command for verifying a user's email with a 6-digit OTP.</summary>
/// <param name="Request">The OTP verification payload.</param>
public sealed record VerifyEmailCommand(Dtos.Auth.VerifyOtpRequest Request) : IRequest<Result<bool>>;

/// <summary>Handler for <see cref="VerifyEmailCommand"/>.</summary>
public sealed class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICacheService _cache;

    /// <summary>Initialises the handler.</summary>
    public VerifyEmailCommandHandler(IUnitOfWork uow, ICacheService cache)
    {
        _uow = uow;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        VerifyEmailCommand command,
        CancellationToken cancellationToken)
    {
        string email = command.Request.Email.Trim().ToLowerInvariant();
        string cacheKey = $"otp:verify:{email}";

        string? storedOtp = await _cache.GetAsync<string>(cacheKey, cancellationToken);
        if (storedOtp is null)
        {
            return Error.Validation("OTP has expired or was never issued. Request a new one.");
        }

        if (!string.Equals(storedOtp, command.Request.Otp.Trim(), StringComparison.Ordinal))
        {
            return Error.Validation("Invalid OTP.");
        }

        User? user = await _uow.Users.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            return Error.NotFound("User");
        }

        user.IsVerified = true;
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync(cacheKey, cancellationToken);

        return Result<bool>.Success(true);
    }
}

/// <summary>Validator for <see cref="VerifyEmailCommand"/>.</summary>
public sealed class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public VerifyEmailCommandValidator()
    {
        RuleFor(x => x.Request.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Request.Otp)
            .NotEmpty()
            .Length(6).WithMessage("OTP must be 6 digits.")
            .Matches(@"^\d{6}$").WithMessage("OTP must contain only digits.");
    }
}
