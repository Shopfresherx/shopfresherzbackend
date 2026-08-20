using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.PaymentMethods;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.PaymentMethods.Queries;

/// <summary>Query for all saved bank cards of the authenticated user.</summary>
/// <param name="UserId">The authenticated user's ID.</param>
public sealed record GetPaymentMethodsQuery(Guid UserId) : IRequest<Result<IReadOnlyList<PaymentMethodDto>>>;

/// <summary>Handler for <see cref="GetPaymentMethodsQuery"/>.</summary>
public sealed class GetPaymentMethodsQueryHandler
    : IRequestHandler<GetPaymentMethodsQuery, Result<IReadOnlyList<PaymentMethodDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetPaymentMethodsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PaymentMethodDto>>> Handle(
        GetPaymentMethodsQuery query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<SavedCard> cards =
            await _uow.PaymentMethods.GetByUserIdAsync(query.UserId, cancellationToken);

        IReadOnlyList<PaymentMethodDto> dtos = cards.Select(m => new PaymentMethodDto
        {
            Id             = m.Id,
            CardType       = m.CardType,
            CardNumber     = m.CardNumber,
            CardHolderName = m.CardHolderName,
            ExpiryMonth    = m.ExpiryMonth,
            ExpiryYear     = m.ExpiryYear,
            IsDefault      = m.IsDefault,
            CreatedAt      = m.CreatedAt,
        }).ToList();

        return Result<IReadOnlyList<PaymentMethodDto>>.Success(dtos);
    }
}
