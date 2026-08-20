using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Products.Commands;

/// <summary>Command for soft-deleting a product (admin only).</summary>
/// <param name="Id">The product ID to delete.</param>
public sealed record DeleteProductCommand(Guid Id) : IRequest<Result<bool>>;

/// <summary>Handler for <see cref="DeleteProductCommand"/>.</summary>
public sealed class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;

    /// <summary>Initialises the handler.</summary>
    public DeleteProductCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        DeleteProductCommand command,
        CancellationToken cancellationToken)
    {
        Product? product = await _uow.Products.GetByIdAsync(command.Id, cancellationToken);
        if (product is null)
        {
            return Error.NotFound("Product");
        }

        product.DeletedAt = DateTime.UtcNow;
        _uow.Products.Delete(product);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
