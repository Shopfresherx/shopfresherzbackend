using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Notifications;

namespace ShopFresherz.Application.Features.Admin.Queries;

/// <summary>Query for paginated admin notifications with unread count.</summary>
public sealed record GetAdminNotificationsQuery(
    int Page = 1,
    int PageSize = 20,
    bool? UnreadOnly = null) : IRequest<Result<NotificationResponseDto>>;