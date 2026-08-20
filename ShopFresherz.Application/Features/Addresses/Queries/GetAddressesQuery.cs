using AutoMapper;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Profile;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Addresses.Queries;

/// <summary>Query for all saved addresses of the authenticated user.</summary>
/// <param name="UserId">The authenticated user's ID.</param>
public sealed record GetAddressesQuery(Guid UserId) : IRequest<Result<IReadOnlyList<AddressDto>>>;

/// <summary>Handler for <see cref="GetAddressesQuery"/>.</summary>
public sealed class GetAddressesQueryHandler
    : IRequestHandler<GetAddressesQuery, Result<IReadOnlyList<AddressDto>>>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    /// <summary>Initialises the handler.</summary>
    public GetAddressesQueryHandler(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<AddressDto>>> Handle(
        GetAddressesQuery query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Address> addresses =
            await _uow.Addresses.GetByUserIdAsync(query.UserId, cancellationToken);

        return Result<IReadOnlyList<AddressDto>>.Success(
            _mapper.Map<IReadOnlyList<AddressDto>>(addresses));
    }
}
