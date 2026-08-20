using FluentValidation;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.PaymentMethods;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.PaymentMethods.Commands;

/// <summary>Command for saving a new bank card to the user's profile.</summary>
public sealed record CreatePaymentMethodCommand(Guid UserId, CreatePaymentMethodRequest Request)
    : IRequest<Result<Guid>>;

/// <summary>Handler for <see cref="CreatePaymentMethodCommand"/>.</summary>
public sealed class CreatePaymentMethodCommandHandler
    : IRequestHandler<CreatePaymentMethodCommand, Result<Guid>>
{
    private readonly IUnitOfWork _uow;

    public CreatePaymentMethodCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(
        CreatePaymentMethodCommand command,
        CancellationToken cancellationToken)
    {
        if (command.UserId == Guid.Empty)
        {
            return Error.Unauthorized("Unable to resolve the current user identity.");
        }

        CreatePaymentMethodRequest req = command.Request;

        // If this card should be the default, clear existing defaults first.
        if (req.IsDefault)
        {
            IReadOnlyList<SavedCard> existing =
                await _uow.PaymentMethods.GetByUserIdAsync(command.UserId, cancellationToken);

            foreach (SavedCard pm in existing.Where(p => p.IsDefault))
            {
                pm.IsDefault = false;
                _uow.PaymentMethods.Update(pm);
            }
        }

        SavedCard card = new()
        {
            UserId         = command.UserId,
            CardType       = req.CardType.Trim(),
            CardNumber     = req.CardNumber.Trim(),
            CardHolderName = req.CardHolderName.Trim(),
            ExpiryMonth    = req.ExpiryMonth,
            ExpiryYear     = req.ExpiryYear,
            IsDefault      = req.IsDefault,
        };

        await _uow.PaymentMethods.AddAsync(card, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(card.Id);
    }
}

/// <summary>Validator for <see cref="CreatePaymentMethodCommand"/>.</summary>
public sealed class CreatePaymentMethodCommandValidator : AbstractValidator<CreatePaymentMethodCommand>
{
    public CreatePaymentMethodCommandValidator()
    {
        RuleFor(x => x.Request.CardType).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Request.CardNumber).NotEmpty().MinimumLength(13).MaximumLength(19)
            .Matches(@"^\d{13,19}$").WithMessage("Card number must be 13–19 numeric digits.");
        RuleFor(x => x.Request.CardHolderName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.ExpiryMonth).InclusiveBetween(1, 12);
        RuleFor(x => x.Request.ExpiryYear).GreaterThanOrEqualTo(DateTime.UtcNow.Year);
    }
}
