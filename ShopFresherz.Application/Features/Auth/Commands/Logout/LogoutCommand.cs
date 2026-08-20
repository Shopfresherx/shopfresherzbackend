using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Auth.Commands.Logout;

/// <summary>Command for revoking the user's refresh token (server-side logout).</summary>
/// <param name="UserId">The authenticated user's ID extracted from the JWT claim.</param>
public sealed record LogoutCommand(Guid UserId) : IRequest<Result<bool>>;

/// <summary>Handler for <see cref="LogoutCommand"/>.</summary>
public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;

    /// <summary>Initialises the handler.</summary>
    public LogoutCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        LogoutCommand command,
        CancellationToken cancellationToken)
    {
        User? user = await _uow.Users.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return Result<bool>.Success(true);
        }

        user.RefreshTokenHash    = null;
        user.RefreshTokenExpires = null;

        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
