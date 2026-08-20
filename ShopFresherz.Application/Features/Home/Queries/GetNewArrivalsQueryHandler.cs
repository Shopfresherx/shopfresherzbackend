using AutoMapper;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Product;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Home.Queries;

public sealed class GetNewArrivalsQueryHandler : IRequestHandler<GetNewArrivalsQuery, Result<IReadOnlyList<ProductSummaryDto>>>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public GetNewArrivalsQueryHandler(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<ProductSummaryDto>>> Handle(
        GetNewArrivalsQuery request,
        CancellationToken cancellationToken)
    {
        int limit = Math.Clamp(request.Limit, 1, 50);

        IReadOnlyList<Product> products = await _uow.Products.GetNewArrivalsAsync(limit, cancellationToken);
        IReadOnlyList<ProductSummaryDto> dtos = _mapper.Map<IReadOnlyList<ProductSummaryDto>>(products);

        return Result<IReadOnlyList<ProductSummaryDto>>.Success(dtos);
    }
}

