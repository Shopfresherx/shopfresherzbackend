using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.FeaturedSections.Commands;

/// <summary>Admin command for removing a featured section card.</summary>
public sealed record DeleteFeaturedSectionCommand(Guid Id) : IRequest<Result<bool>>;

/// <summary>Handler for <see cref="DeleteFeaturedSectionCommand"/>.</summary>
public sealed class DeleteFeaturedSectionCommandHandler
    : IRequestHandler<DeleteFeaturedSectionCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;

    public DeleteFeaturedSectionCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        DeleteFeaturedSectionCommand command,
        CancellationToken cancellationToken)
    {
        FeaturedSection? card = await _uow.FeaturedSections.GetByIdAsync(command.Id, cancellationToken);
        if (card is null) return Error.NotFound("Featured section card");

        _uow.FeaturedSections.Delete(card);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
