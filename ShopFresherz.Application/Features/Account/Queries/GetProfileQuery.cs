using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Profile;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Account.Queries;

/// <summary>Query for retrieving the authenticated user's profile.</summary>
public sealed record GetProfileQuery(Guid UserId) : IRequest<Result<UserProfileDto>>;

/// <summary>Handler for <see cref="GetProfileQuery"/>.</summary>
public sealed class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, Result<UserProfileDto>>
{
    private readonly IUnitOfWork _uow;

    public GetProfileQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<UserProfileDto>> Handle(GetProfileQuery query, CancellationToken cancellationToken)
    {
        User? user = await _uow.Users.GetByIdAsync(query.UserId, cancellationToken);
        if (user is null)
        {
            return Error.NotFound("User");
        }

        return Result<UserProfileDto>.Success(new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email,
            Phone = user.Phone,
            FirstName = user.FirstName,
            LastName = user.LastName,
            AvatarUrl = user.AvatarUrl,
            Role = user.Role,
            IsVerified = user.IsVerified,
            LoyaltyPoints = user.LoyaltyPoints,
            CreatedAt = user.CreatedAt,
        });
    }
}
