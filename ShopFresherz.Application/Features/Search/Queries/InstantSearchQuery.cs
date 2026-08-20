using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Domain.Interfaces.Services;

namespace ShopFresherz.Application.Features.Search.Queries;

/// <summary>Query for instant product and category search suggestions.</summary>
public sealed record InstantSearchQuery(string Query) : IRequest<Result<InstantSearchResult>>;

/// <summary>Handler for <see cref="InstantSearchQuery"/>.</summary>
public sealed class InstantSearchQueryHandler
    : IRequestHandler<InstantSearchQuery, Result<InstantSearchResult>>
{
    private readonly ISearchService _search;

    public InstantSearchQueryHandler(ISearchService search)
    {
        _search = search;
    }

    /// <inheritdoc />
    public async Task<Result<InstantSearchResult>> Handle(
        InstantSearchQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query) || request.Query.Trim().Length < 2)
        {
            return Result<InstantSearchResult>.Success(new InstantSearchResult([], []));
        }

        try
        {
            InstantSearchResult result = await _search.InstantSearchAsync(
                request.Query.Trim(),
                cancellationToken);

            return Result<InstantSearchResult>.Success(result);
        }
        catch
        {
            return Result<InstantSearchResult>.Success(new InstantSearchResult([], []));
        }
    }
}
