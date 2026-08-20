using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Profile;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;
using ShopFresherz.Domain.Interfaces.Services;

namespace ShopFresherz.Application.Features.Account.Commands;

/// <summary>Command for changing the authenticated user's password.</summary>
public sealed record ChangePasswordCommand(Guid UserId, ChangePasswordRequest Request)
    : IRequest<Result<bool>>;

/// <summary>Handler for <see cref="ChangePasswordCommand"/>.</summary>
public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordCommandHandler(IUnitOfWork uow, IPasswordHasher passwordHasher)
    {
        _uow = uow;
        _passwordHasher = passwordHasher;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        ChangePasswordRequest request = command.Request;

        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            return Error.Validation("Current password is required.");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword) ||
            request.NewPassword.Length < 8 ||
            !request.NewPassword.Any(char.IsUpper) ||
            !request.NewPassword.Any(char.IsDigit))
        {
            return Error.Validation("New password must be at least 8 characters and contain an uppercase letter and a digit.");
        }

        if (!string.Equals(request.NewPassword, request.ConfirmNewPassword, StringComparison.Ordinal))
        {
            return Error.Validation("Confirm new password must match new password.");
        }

        User? user = await _uow.Users.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return Error.NotFound("User");
        }

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return Error.Validation("Current password is incorrect.");
        }

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.RefreshTokenHash = null;
        user.RefreshTokenExpires = null;

        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
