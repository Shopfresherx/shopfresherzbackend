using AutoMapper;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Product;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Brands.Queries;

/// <summary>Query for retrieving all active brands.</summary>
public sealed record GetBrandsQuery() : IRequest<Result<IReadOnlyList<BrandDto>>>;

/// <summary>Handler for <see cref="GetBrandsQuery"/>.</summary>
public sealed class GetBrandsQueryHandler
    : IRequestHandler<GetBrandsQuery, Result<IReadOnlyList<BrandDto>>>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    /// <summary>Initialises the handler.</summary>
    public GetBrandsQueryHandler(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<BrandDto>>> Handle(
        GetBrandsQuery query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Brand> brands =
            await _uow.Brands.GetAllActiveAsync(cancellationToken);

        return Result<IReadOnlyList<BrandDto>>.Success(
            _mapper.Map<IReadOnlyList<BrandDto>>(brands));
    }
}
