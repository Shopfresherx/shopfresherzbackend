using FluentValidation;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Profile;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Addresses.Commands;

/// <summary>Command for saving a new delivery address to the user's profile.</summary>
/// <param name="UserId">The authenticated user's ID.</param>
/// <param name="Request">The address creation payload.</param>
public sealed record CreateAddressCommand(Guid UserId, CreateAddressRequest Request)
    : IRequest<Result<Guid>>;

/// <summary>Handler for <see cref="CreateAddressCommand"/>.</summary>
public sealed class CreateAddressCommandHandler : IRequestHandler<CreateAddressCommand, Result<Guid>>
{
    private readonly IUnitOfWork _uow;

    /// <summary>Initialises the handler.</summary>
    public CreateAddressCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(
        CreateAddressCommand command,
        CancellationToken cancellationToken)
    {
        if (command.UserId == Guid.Empty)
        {
            return Error.Unauthorized("Unable to resolve the current user identity.");
        }

        CreateAddressRequest req = command.Request;

        if (req.IsDefault)
        {
            IReadOnlyList<Address> existing =
                await _uow.Addresses.GetByUserIdAsync(command.UserId, cancellationToken);

            foreach (Address addr in existing.Where(a => a.IsDefault))
            {
                addr.IsDefault = false;
                _uow.Addresses.Update(addr);
            }
        }

        Address address = new()
        {
            UserId     = command.UserId,
            Label      = req.Label.Trim(),
            Line1      = req.Line1.Trim(),
            Line2      = req.Line2?.Trim(),
            City       = req.City.Trim(),
            State      = req.State.Trim(),
            PostalCode = req.PostalCode?.Trim(),
            IsDefault  = req.IsDefault,
        };

        await _uow.Addresses.AddAsync(address, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(address.Id);
    }
}

/// <summary>Validator for <see cref="CreateAddressCommand"/>.</summary>
public sealed class CreateAddressCommandValidator : AbstractValidator<CreateAddressCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public CreateAddressCommandValidator()
    {
        RuleFor(x => x.Request.Label).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Request.Line1).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.State).NotEmpty().MaximumLength(100);
    }
}
