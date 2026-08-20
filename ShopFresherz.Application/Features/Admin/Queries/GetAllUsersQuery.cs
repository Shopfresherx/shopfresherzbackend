using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Admin;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Admin.Queries;

/// <summary>Admin query for paginated users.</summary>
public sealed record GetAllUsersQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null) : IRequest<Result<PagedResult<AdminUserDto>>>;

/// <summary>Handler for <see cref="GetAllUsersQuery"/>.</summary>
public sealed class GetAllUsersQueryHandler
    : IRequestHandler<GetAllUsersQuery, Result<PagedResult<AdminUserDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetAllUsersQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<AdminUserDto>>> Handle(
        GetAllUsersQuery query,
        CancellationToken cancellationToken)
    {
        int page = Math.Max(1, query.Page);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);

        (IReadOnlyList<User> items, int total) = await _uow.Users.GetAllAsync(
            page, pageSize, query.Search, cancellationToken);

        IReadOnlyList<AdminUserDto> users = items.Select(u => new AdminUserDto
        {
            Id = u.Id,
            Email = u.Email,
            Phone = u.Phone,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Role = u.Role,
            IsVerified = u.IsVerified,
            LoyaltyPoints = u.LoyaltyPoints,
            OrderCount = u.Orders.Count,
            CreatedAt = u.CreatedAt,
        }).ToList();

        return Result<PagedResult<AdminUserDto>>.Success(
            new PagedResult<AdminUserDto>(users, total, page, pageSize));
    }
}
