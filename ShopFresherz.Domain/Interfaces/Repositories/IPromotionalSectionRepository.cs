using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Domain.Interfaces.Repositories;

/// <summary>Repository interface for <see cref="PromotionalSection"/> entities.</summary>
public interface IPromotionalSectionRepository
{
    /// <summary>
    /// Returns all active sections for the given key, projected to <typeparamref name="TDto"/>
    /// ordered by <see cref="PromotionalSection.SortOrder"/>.
    /// </summary>
    Task<IReadOnlyList<TDto>> GetBySectionKeyAsync<TDto>(
        string sectionKey,
        CancellationToken cancellationToken,
        Func<PromotionalSection, TDto> selector);

    /// <summary>Returns the raw active entities for the given section key (for heterogeneous projections).</summary>
    Task<List<PromotionalSection>> GetRawBySectionKeyAsync(
        string sectionKey,
        CancellationToken cancellationToken);

    /// <summary>Returns all non-deleted records for a section, including inactive records.</summary>
    Task<IReadOnlyList<PromotionalSection>> GetAllBySectionKeyAsync(
        string sectionKey,
        CancellationToken cancellationToken);

    /// <summary>Returns a tracked promotion by section and linked product.</summary>
    Task<PromotionalSection?> GetByProductIdAsync(
        string sectionKey,
        Guid productId,
        CancellationToken cancellationToken);

    /// <summary>Returns a tracked promotion by its front-end slug identifier.</summary>
    Task<PromotionalSection?> GetBySlugIdAsync(
        string slugId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Soft-deletes all active records for the given section key.
    /// Used by admin replace operations to ensure only one item exists per single-item section.
    /// </summary>
    Task DeleteBySectionKeyAsync(string sectionKey, CancellationToken cancellationToken);

    /// <summary>Returns all records (active and soft-deleted) ordered by section key then sort order. Admin only.</summary>
    Task<IReadOnlyList<PromotionalSection>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Returns a tracked promotional section by its primary key, or null if not found.</summary>
    Task<PromotionalSection?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Adds a new promotional section record to the store.</summary>
    Task AddAsync(PromotionalSection section, CancellationToken cancellationToken);

    /// <summary>
    /// Soft-deletes all records (including previously deleted ones) that match the given SlugId.
    /// Used by hero-banner replace logic to free the unique SlugId before re-inserting.
    /// </summary>
    Task DeleteBySlugIdAsync(string slugId, CancellationToken cancellationToken);
}
