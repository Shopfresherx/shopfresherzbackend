using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Addresses.Commands;

/// <summary>Command for soft-deleting a saved address.</summary>
/// <param name="UserId">The authenticated user's ID for ownership validation.</param>
/// <param name="AddressId">The address to delete.</param>
public sealed record DeleteAddressCommand(Guid UserId, Guid AddressId) : IRequest<Result<bool>>;

/// <summary>Handler for <see cref="DeleteAddressCommand"/>.</summary>
public sealed class DeleteAddressCommandHandler : IRequestHandler<DeleteAddressCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;

    /// <summary>Initialises the handler.</summary>
    public DeleteAddressCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        DeleteAddressCommand command,
        CancellationToken cancellationToken)
    {
        Address? address = await _uow.Addresses.GetByIdAsync(command.AddressId, cancellationToken);
        if (address is null) return Error.NotFound("Address");
        if (address.UserId != command.UserId) return Error.Forbidden();

        _uow.Addresses.Delete(address);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
