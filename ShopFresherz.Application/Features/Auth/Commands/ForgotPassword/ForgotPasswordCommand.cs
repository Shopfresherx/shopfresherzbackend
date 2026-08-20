using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using ShopFresherz.Application.Common;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;
using ShopFresherz.Domain.Interfaces.Services;
using System.Security.Cryptography;
using System.Text;

namespace ShopFresherz.Application.Features.Auth.Commands.ForgotPassword;

/// <summary>Command for initiating a password reset flow via OTP email.</summary>
/// <param name="Request">The forgot-password payload.</param>
public sealed record ForgotPasswordCommand(Dtos.Auth.ForgotPasswordRequest Request) : IRequest<Result<bool>>;

/// <summary>Handler for <see cref="ForgotPasswordCommand"/>.</summary>
public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;
    private readonly IEmailService _email;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public ForgotPasswordCommandHandler(
        IUnitOfWork uow,
        IEmailService email,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _uow = uow;
        _email = email;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        ForgotPasswordCommand command,
        CancellationToken cancellationToken)
    {
        string email = command.Request.Email.Trim().ToLowerInvariant();

        User? user = await _uow.Users.GetByEmailAsync(email, cancellationToken);

        // Always return success to prevent email enumeration.
        if (user is null)
        {
            return Result<bool>.Success(true);
        }

        string otp = RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString();
        user.PasswordResetOtpHash = HashOtp(otp);
        user.PasswordResetOtpExpires = DateTime.UtcNow.AddMinutes(15);
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(cancellationToken);

        try
        {
            await _email.SendPasswordResetAsync(
                user.Email,
                user.FirstName,
                otp,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset OTP to {Email}", email);
        }

        return Result<bool>.Success(true);
    }

    private static string HashOtp(string otp) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(otp)));
}

/// <summary>Validator for <see cref="ForgotPasswordCommand"/>.</summary>
public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Request.Email).NotEmpty().EmailAddress();
    }
}
