using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.PaymentMethods;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.PaymentMethods.Commands;

/// <summary>Command for updating a saved bank card.</summary>
public sealed record UpdatePaymentMethodCommand(Guid UserId, Guid PaymentMethodId, UpdatePaymentMethodRequest Request)
    : IRequest<Result<bool>>;

/// <summary>Handler for <see cref="UpdatePaymentMethodCommand"/>.</summary>
public sealed class UpdatePaymentMethodCommandHandler
    : IRequestHandler<UpdatePaymentMethodCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;

    public UpdatePaymentMethodCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        UpdatePaymentMethodCommand command,
        CancellationToken cancellationToken)
    {
        SavedCard? method =
            await _uow.PaymentMethods.GetByIdAsync(command.PaymentMethodId, cancellationToken);

        if (method is null) return Error.NotFound("Payment method");
        if (method.UserId != command.UserId) return Error.Forbidden();

        UpdatePaymentMethodRequest req = command.Request;

        // Set as default — clear other defaults first.
        if (req.IsDefault == true && !method.IsDefault)
        {
            IReadOnlyList<SavedCard> existing =
                await _uow.PaymentMethods.GetByUserIdAsync(command.UserId, cancellationToken);

            foreach (SavedCard pm in existing.Where(p => p.IsDefault && p.Id != method.Id))
            {
                pm.IsDefault = false;
                _uow.PaymentMethods.Update(pm);
            }

            method.IsDefault = true;
        }

        if (req.CardHolderName is not null) method.CardHolderName = req.CardHolderName.Trim();
        if (req.ExpiryMonth.HasValue)        method.ExpiryMonth    = req.ExpiryMonth.Value;
        if (req.ExpiryYear.HasValue)         method.ExpiryYear     = req.ExpiryYear.Value;

        _uow.PaymentMethods.Update(method);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
