using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Categories.Commands;

/// <summary>Admin command for deleting a category.</summary>
/// <param name="Id">The category's integer ID.</param>
public sealed record DeleteCategoryCommand(int Id) : IRequest<Result<bool>>;

/// <summary>Handler for <see cref="DeleteCategoryCommand"/>.</summary>
public sealed class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;

    public DeleteCategoryCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        DeleteCategoryCommand command,
        CancellationToken cancellationToken)
    {
        Category? category = await _uow.Categories.GetByIdAsync(command.Id, cancellationToken);
        if (category is null) return Error.NotFound("Category");

        if (category.Children.Count > 0)
        {
            return Error.Conflict("Cannot delete a category that has sub-categories. Remove all children first.");
        }

        if (category.Products.Count > 0)
        {
            return Error.Conflict("Cannot delete a category that has products assigned to it. Re-assign or delete the products first.");
        }

        _uow.Categories.Delete(category);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
