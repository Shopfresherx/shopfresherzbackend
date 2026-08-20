using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Notifications;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Admin.Queries;

/// <summary>Handler for <see cref="GetAdminNotificationsQuery"/>.</summary>
public sealed class GetAdminNotificationsQueryHandler
    : IRequestHandler<GetAdminNotificationsQuery, Result<NotificationResponseDto>>
{
    private readonly IUnitOfWork _uow;

    public GetAdminNotificationsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<NotificationResponseDto>> Handle(
        GetAdminNotificationsQuery query,
        CancellationToken cancellationToken)
    {
        int page = Math.Max(1, query.Page);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);

        // In this codebase, admin-side notification list is backed by NotifyRequest rows.
        IReadOnlyList<NotifyRequest> items = await _uow.NotifyRequests.GetPendingAsync(cancellationToken);
        int total = items.Count;

        // Map to DTOs and apply pagination in-memory.
        var pageItems = items
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = "BackInStock",
                Title = "Back in stock",
                Message = n.Product?.Name ?? string.Empty,
                LinkUrl = $"/products/{n.Product?.Slug ?? string.Empty}",
                IsRead = false,
                CreatedAt = n.CreatedAt
            })
            .ToList();

        var response = new NotificationResponseDto
        {
            Items = pageItems,
            UnreadCount = total
        };

        return Result<NotificationResponseDto>.Success(response);


    }
}