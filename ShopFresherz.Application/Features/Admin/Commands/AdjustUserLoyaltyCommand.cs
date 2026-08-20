using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Admin;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Enums;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Admin.Commands;

/// <summary>Admin command for manually adjusting a user's loyalty balance.</summary>
public sealed record AdjustUserLoyaltyCommand(
    Guid TargetUserId,
    AdjustLoyaltyRequest Request) : IRequest<Result<bool>>;

/// <summary>Handler for <see cref="AdjustUserLoyaltyCommand"/>.</summary>
public sealed class AdjustUserLoyaltyCommandHandler
    : IRequestHandler<AdjustUserLoyaltyCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;

    public AdjustUserLoyaltyCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        AdjustUserLoyaltyCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Request.Points == 0)
        {
            return Error.Validation("Points adjustment cannot be zero.");
        }

        if (string.IsNullOrWhiteSpace(command.Request.Reason) ||
            command.Request.Reason.Length > 200)
        {
            return Error.Validation("Reason is required and must be 200 characters or fewer.");
        }

        User? user = await _uow.Users.GetByIdAsync(command.TargetUserId, cancellationToken);
        if (user is null)
        {
            return Error.NotFound("User");
        }

        if (user.LoyaltyPoints + command.Request.Points < 0)
        {
            return Error.Validation("Insufficient loyalty points balance.");
        }

        user.LoyaltyPoints += command.Request.Points;

        await _uow.LoyaltyTransactions.AddAsync(new LoyaltyTransaction
        {
            UserId = user.Id,
            EventType = command.Request.Points > 0 ? LoyaltyEventType.Bonus : LoyaltyEventType.Redeemed,
            Points = command.Request.Points,
            Description = command.Request.Reason,
            ExpiresAt = DateTime.UtcNow.AddYears(1),
        }, cancellationToken);

        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
