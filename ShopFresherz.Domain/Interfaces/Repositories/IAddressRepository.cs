using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Domain.Interfaces.Repositories;

/// <summary>Repository contract for saved Address persistence operations.</summary>
public interface IAddressRepository
{
    /// <summary>Retrieves an address by its unique identifier.</summary>
    Task<Address?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns all saved addresses for a user.</summary>
    Task<IReadOnlyList<Address>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Returns the default address for a user.</summary>
    Task<Address?> GetDefaultAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Adds a new address.</summary>
    Task AddAsync(Address address, CancellationToken cancellationToken = default);

    /// <summary>Marks an address as modified.</summary>
    void Update(Address address);

    /// <summary>Soft-deletes an address.</summary>
    void Delete(Address address);
}
