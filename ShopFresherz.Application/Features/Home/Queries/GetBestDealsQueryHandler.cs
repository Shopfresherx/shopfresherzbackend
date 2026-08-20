using AutoMapper;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Product;
using ShopFresherz.Domain.Interfaces;
using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Application.Features.Home.Queries;

public sealed class GetBestDealsQueryHandler : IRequestHandler<GetBestDealsQuery, Result<IReadOnlyList<ProductSummaryDto>>>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public GetBestDealsQueryHandler(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<ProductSummaryDto>>> Handle(
        GetBestDealsQuery request,
        CancellationToken cancellationToken)
    {
        int limit = Math.Clamp(request.Limit, 1, 50);

        IReadOnlyList<Product> products = await _uow.Products.GetBestDealsAsync(limit, cancellationToken);

        IReadOnlyList<ProductSummaryDto> dtos = _mapper.Map<IReadOnlyList<ProductSummaryDto>>(products);

        return Result<IReadOnlyList<ProductSummaryDto>>.Success(dtos);
    }
}

