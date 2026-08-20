using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Profile;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Account.Commands;

/// <summary>Command for updating the authenticated user's profile.</summary>
public sealed record UpdateProfileCommand(Guid UserId, UpdateProfileRequest Request)
    : IRequest<Result<bool>>;

/// <summary>Handler for <see cref="UpdateProfileCommand"/>.</summary>
public sealed class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;

    public UpdateProfileCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        User? user = await _uow.Users.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return Error.NotFound("User");
        }

        if (command.Request.FirstName is not null) user.FirstName = command.Request.FirstName;
        if (command.Request.LastName is not null) user.LastName = command.Request.LastName;
        if (command.Request.Phone is not null) user.Phone = command.Request.Phone;
        if (command.Request.AvatarUrl is not null) user.AvatarUrl = command.Request.AvatarUrl;

        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
