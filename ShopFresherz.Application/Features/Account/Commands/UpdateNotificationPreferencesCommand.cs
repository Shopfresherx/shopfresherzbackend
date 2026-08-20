using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Notifications;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Account.Commands;

/// <summary>Command to update user notification preferences.</summary>
public sealed record UpdateNotificationPreferencesCommand(Guid UserId, NotificationPreferencesDto Preferences)
    : IRequest<Result<bool>>;

/// <summary>Handler for updating notification preferences.</summary>
public sealed class UpdateNotificationPreferencesCommandHandler : IRequestHandler<UpdateNotificationPreferencesCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateNotificationPreferencesCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(
        UpdateNotificationPreferencesCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return Result<bool>.Failure(Error.NotFound("User"));
        }

        user.NotificationOrderUpdates = request.Preferences.OrderUpdates;
        user.NotificationPromotions = request.Preferences.Promotions;
        user.NotificationBackInStock = request.Preferences.BackInStock;
        user.NotificationWishlistReminders = request.Preferences.WishlistReminders;
        user.NotificationReviewReminders = request.Preferences.ReviewReminders;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}