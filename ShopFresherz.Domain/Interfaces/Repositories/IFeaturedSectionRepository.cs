using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Domain.Interfaces.Repositories;

/// <summary>Repository contract for FeaturedSection persistence operations.</summary>
public interface IFeaturedSectionRepository
{
    /// <summary>Retrieves a featured section card by its unique identifier.</summary>
    Task<FeaturedSection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns active cards for the given section, ordered by SortOrder.</summary>
    Task<IReadOnlyList<FeaturedSection>> GetActiveBySectionAsync(string section, CancellationToken cancellationToken = default);

    /// <summary>Returns all cards (admin view), ordered by section then SortOrder.</summary>
    Task<IReadOnlyList<FeaturedSection>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a new featured section card.</summary>
    Task AddAsync(FeaturedSection card, CancellationToken cancellationToken = default);

    /// <summary>Marks a card as modified.</summary>
    void Update(FeaturedSection card);

    /// <summary>Removes a card.</summary>
    void Delete(FeaturedSection card);
}
