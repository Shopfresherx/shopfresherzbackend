using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Notifications;

/// <summary>Command for registering a user's back-in-stock notification request.</summary>
public sealed record RegisterNotifyRequestCommand(Guid UserId, Guid ProductId) : IRequest<Result<bool>>;

/// <summary>Handler for <see cref="RegisterNotifyRequestCommand"/>.</summary>
public sealed class RegisterNotifyRequestCommandHandler
    : IRequestHandler<RegisterNotifyRequestCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;

    public RegisterNotifyRequestCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        RegisterNotifyRequestCommand command,
        CancellationToken cancellationToken)
    {
        Product? product = await _uow.Products.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            return Error.NotFound("Product");
        }

        if (product.AvailableQty > 0)
        {
            return Error.Validation("Product is currently in stock. Add it to cart directly.");
        }

        bool exists = await _uow.NotifyRequests.ExistsAsync(
            command.UserId,
            command.ProductId,
            cancellationToken);

        if (exists)
        {
            return Result<bool>.Success(true);
        }

        NotifyRequest request = new()
        {
            UserId = command.UserId,
            ProductId = command.ProductId,
        };

        await _uow.NotifyRequests.AddAsync(request, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
