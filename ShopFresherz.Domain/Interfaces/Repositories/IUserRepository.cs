using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Domain.Interfaces.Repositories;

/// <summary>Repository contract for User aggregate persistence operations.</summary>
public interface IUserRepository
{
    /// <summary>Retrieves a user by their unique identifier.</summary>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a user by their email address (case-insensitive).</summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a user by their phone number.</summary>
    Task<User?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a user by their Google OAuth subject identifier.</summary>
    Task<User?> GetByGoogleIdAsync(string googleId, CancellationToken cancellationToken = default);

    /// <summary>Checks whether an email address is already registered.</summary>
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Checks whether a phone number is already registered.</summary>
    Task<bool> PhoneExistsAsync(string phone, CancellationToken cancellationToken = default);

    /// <summary>Returns paginated users matching an optional search term.</summary>
    Task<(IReadOnlyList<User> Items, int TotalCount)> GetAllAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default);

    /// <summary>Counts users created today.</summary>
    Task<int> CountTodayAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a new user to the persistence store.</summary>
    Task AddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>Marks an existing user entity as modified.</summary>
    void Update(User user);
}
