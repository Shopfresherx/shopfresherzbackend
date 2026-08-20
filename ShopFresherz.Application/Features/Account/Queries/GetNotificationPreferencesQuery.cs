using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Notifications;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Account.Queries;

/// <summary>Query to get user notification preferences.</summary>
public sealed record GetNotificationPreferencesQuery(Guid UserId)
    : IRequest<Result<NotificationPreferencesDto>>;

/// <summary>Handler for getting notification preferences.</summary>
public sealed class GetNotificationPreferencesQueryHandler : IRequestHandler<GetNotificationPreferencesQuery, Result<NotificationPreferencesDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetNotificationPreferencesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<NotificationPreferencesDto>> Handle(
        GetNotificationPreferencesQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return Result<NotificationPreferencesDto>.Failure(Error.NotFound("User"));
        }

        var preferences = new NotificationPreferencesDto
        {
            OrderUpdates = user.NotificationOrderUpdates,
            Promotions = user.NotificationPromotions,
            BackInStock = user.NotificationBackInStock,
            WishlistReminders = user.NotificationWishlistReminders,
            ReviewReminders = user.NotificationReviewReminders
        };

        return Result<NotificationPreferencesDto>.Success(preferences);
    }
}