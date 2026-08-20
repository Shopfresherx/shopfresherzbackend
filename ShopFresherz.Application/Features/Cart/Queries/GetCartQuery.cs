using AutoMapper;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Cart;
using ShopFresherz.Domain.Interfaces;
using CartEntity = ShopFresherz.Domain.Entities.Cart;

namespace ShopFresherz.Application.Features.Cart.Queries;

/// <summary>Query for fetching the active cart for a user or guest session.</summary>
/// <param name="UserId">The authenticated user's ID (null for guests).</param>
/// <param name="SessionId">The guest browser session ID (null for authenticated users).</param>
public sealed record GetCartQuery(Guid? UserId, string? SessionId)
    : IRequest<Result<CartDto>>;

/// <summary>Handler for <see cref="GetCartQuery"/>.</summary>
public sealed class GetCartQueryHandler : IRequestHandler<GetCartQuery, Result<CartDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    /// <summary>Initialises the handler.</summary>
    public GetCartQueryHandler(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<Result<CartDto>> Handle(
        GetCartQuery query,
        CancellationToken cancellationToken)
    {
        CartEntity? cart = query.UserId.HasValue
            ? await _uow.Carts.GetByUserIdAsync(query.UserId.Value, cancellationToken)
            : query.SessionId is not null
                ? await _uow.Carts.GetBySessionIdAsync(query.SessionId, cancellationToken)
                : null;

        if (cart is null)
        {
            return Result<CartDto>.Success(new CartDto
            {
                UserId    = query.UserId,
                SessionId = query.SessionId,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                Items     = [],
            });
        }

        return Result<CartDto>.Success(_mapper.Map<CartDto>(cart));
    }
}
