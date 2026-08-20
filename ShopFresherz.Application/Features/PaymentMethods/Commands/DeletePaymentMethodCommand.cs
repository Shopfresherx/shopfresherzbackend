using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.PaymentMethods.Commands;

/// <summary>Command for removing a saved bank card.</summary>
public sealed record DeletePaymentMethodCommand(Guid UserId, Guid PaymentMethodId) : IRequest<Result<bool>>;

/// <summary>Handler for <see cref="DeletePaymentMethodCommand"/>.</summary>
public sealed class DeletePaymentMethodCommandHandler
    : IRequestHandler<DeletePaymentMethodCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;

    public DeletePaymentMethodCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        DeletePaymentMethodCommand command,
        CancellationToken cancellationToken)
    {
        SavedCard? method =
            await _uow.PaymentMethods.GetByIdAsync(command.PaymentMethodId, cancellationToken);

        if (method is null) return Error.NotFound("Payment method");
        if (method.UserId != command.UserId) return Error.Forbidden();

        _uow.PaymentMethods.Delete(method);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
