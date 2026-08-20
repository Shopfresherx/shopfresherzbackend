using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Domain.Interfaces.Repositories;
/// <summary>Repository contract for product Review persistence operations.</summary>
public interface IReviewRepository
{
    /// <summary>Retrieves a review by its unique identifier.</summary>
    Task<Review?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns paginated approved reviews for a product.</summary>
    Task<(IReadOnlyList<Review> Items, int TotalCount)> GetByProductAsync(
        Guid productId,
        int page,
        int pageSize,
        int? starFilter = null,
        CancellationToken cancellationToken = default);

    /// <summary>Returns all reviews pending admin approval.</summary>
    Task<IReadOnlyList<Review>> GetPendingApprovalAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns paginated reviews for admin (all reviews, regardless of approval status).</summary>
    Task<(IReadOnlyList<Review> Items, int TotalCount)> GetAllPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Checks whether a user has already reviewed a specific product.</summary>
    Task<bool> UserHasReviewedAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);

    /// <summary>Adds a new review.</summary>
    Task AddAsync(Review review, CancellationToken cancellationToken = default);

    /// <summary>Marks a review as modified.</summary>
    void Update(Review review);
}
