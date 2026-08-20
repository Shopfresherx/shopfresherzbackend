using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Profile;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Account.Queries;

/// <summary>Query for retrieving a user's loyalty balance and transaction history.</summary>
public sealed record GetLoyaltyQuery(Guid UserId, int Page = 1, int PageSize = 20)
    : IRequest<Result<LoyaltyDto>>;

/// <summary>Handler for <see cref="GetLoyaltyQuery"/>.</summary>
public sealed class GetLoyaltyQueryHandler : IRequestHandler<GetLoyaltyQuery, Result<LoyaltyDto>>
{
    private readonly IUnitOfWork _uow;

    public GetLoyaltyQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<LoyaltyDto>> Handle(GetLoyaltyQuery query, CancellationToken cancellationToken)
    {
        User? user = await _uow.Users.GetByIdAsync(query.UserId, cancellationToken);
        if (user is null)
        {
            return Error.NotFound("User");
        }

        int page = Math.Max(1, query.Page);
        int pageSize = Math.Clamp(query.PageSize, 1, 50);
        (IReadOnlyList<LoyaltyTransaction> items, int total) =
            await _uow.LoyaltyTransactions.GetByUserIdAsync(user.Id, page, pageSize, cancellationToken);

        IReadOnlyList<LoyaltyTransactionDto> transactions = items.Select(t => new LoyaltyTransactionDto
        {
            Id = t.Id,
            EventType = t.EventType,
            Points = t.Points,
            Description = t.Description,
            CreatedAt = t.CreatedAt,
            ExpiresAt = t.ExpiresAt,
        }).ToList();

        return Result<LoyaltyDto>.Success(new LoyaltyDto
        {
            Balance = user.LoyaltyPoints,
            Transactions = new PagedResult<LoyaltyTransactionDto>(transactions, total, page, pageSize),
        });
    }
}
