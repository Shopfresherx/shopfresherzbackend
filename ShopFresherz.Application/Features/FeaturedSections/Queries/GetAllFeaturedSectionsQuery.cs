using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.FeaturedSections;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.FeaturedSections.Queries;

/// <summary>Admin query for all featured section cards.</summary>
public sealed record GetAllFeaturedSectionsQuery : IRequest<Result<IReadOnlyList<FeaturedSectionDto>>>;

/// <summary>Handler for <see cref="GetAllFeaturedSectionsQuery"/>.</summary>
public sealed class GetAllFeaturedSectionsQueryHandler
    : IRequestHandler<GetAllFeaturedSectionsQuery, Result<IReadOnlyList<FeaturedSectionDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetAllFeaturedSectionsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<FeaturedSectionDto>>> Handle(
        GetAllFeaturedSectionsQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<FeaturedSection> cards =
            await _uow.FeaturedSections.GetAllAsync(cancellationToken);

        IReadOnlyList<FeaturedSectionDto> dtos = cards
            .Select(GetFeaturedSectionQueryHandler.ToDto)
            .ToList();

        return Result<IReadOnlyList<FeaturedSectionDto>>.Success(dtos);
    }
}
