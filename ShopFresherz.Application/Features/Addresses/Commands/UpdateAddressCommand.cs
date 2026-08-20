using FluentValidation;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Profile;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Addresses.Commands;

/// <summary>Command for updating an existing delivery address.</summary>
/// <param name="UserId">The authenticated user's ID for ownership validation.</param>
/// <param name="AddressId">The address to update.</param>
/// <param name="Request">The updated address payload.</param>
public sealed record UpdateAddressCommand(Guid UserId, Guid AddressId, UpdateAddressRequest Request)
    : IRequest<Result<bool>>;

/// <summary>Handler for <see cref="UpdateAddressCommand"/>.</summary>
public sealed class UpdateAddressCommandHandler : IRequestHandler<UpdateAddressCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;

    /// <summary>Initialises the handler.</summary>
    public UpdateAddressCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        UpdateAddressCommand command,
        CancellationToken cancellationToken)
    {
        if (command.UserId == Guid.Empty)
        {
            return Error.Unauthorized("Unable to resolve the current user identity.");
        }

        Address? address = await _uow.Addresses.GetByIdAsync(command.AddressId, cancellationToken);
        if (address is null) return Error.NotFound("Address");
        if (address.UserId != command.UserId) return Error.Forbidden();

        UpdateAddressRequest req = command.Request;

        // If this address is being set as default, clear others first.
        if (req.IsDefault == true && !address.IsDefault)
        {
            IReadOnlyList<Address> existing =
                await _uow.Addresses.GetByUserIdAsync(command.UserId, cancellationToken);

            foreach (Address addr in existing.Where(a => a.IsDefault && a.Id != address.Id))
            {
                addr.IsDefault = false;
                _uow.Addresses.Update(addr);
            }
        }

        if (req.Label is not null) address.Label = req.Label.Trim();
        if (req.Line1 is not null) address.Line1 = req.Line1.Trim();
        address.Line2      = req.Line2?.Trim() ?? address.Line2;
        if (req.City is not null) address.City = req.City.Trim();
        if (req.State is not null) address.State = req.State.Trim();
        address.PostalCode = req.PostalCode?.Trim() ?? address.PostalCode;
        if (req.IsDefault.HasValue) address.IsDefault = req.IsDefault.Value;

        _uow.Addresses.Update(address);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}

/// <summary>Validator for <see cref="UpdateAddressCommand"/>.</summary>
public sealed class UpdateAddressCommandValidator : AbstractValidator<UpdateAddressCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public UpdateAddressCommandValidator()
    {
        When(x => x.Request.Label is not null,
            () => RuleFor(x => x.Request.Label).NotEmpty().MaximumLength(50));
        When(x => x.Request.Line1 is not null,
            () => RuleFor(x => x.Request.Line1).NotEmpty().MaximumLength(200));
        When(x => x.Request.City is not null,
            () => RuleFor(x => x.Request.City).NotEmpty().MaximumLength(100));
        When(x => x.Request.State is not null,
            () => RuleFor(x => x.Request.State).NotEmpty().MaximumLength(100));
    }
}
