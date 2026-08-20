using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Domain.Interfaces.Repositories;

/// <summary>Repository contract for Brand persistence operations.</summary>
public interface IBrandRepository
{
    /// <summary>Retrieves a brand by its unique identifier.</summary>
    Task<Brand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a brand by its URL slug.</summary>
    Task<Brand?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>Returns all active brands.</summary>
    Task<IReadOnlyList<Brand>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a new brand.</summary>
    Task AddAsync(Brand brand, CancellationToken cancellationToken = default);

    /// <summary>Marks a brand as modified.</summary>
    void Update(Brand brand);
}
