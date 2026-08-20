using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Domain.Interfaces.Repositories;

/// <summary>Repository contract for homepage banners.</summary>
public interface IHomepageBannerRepository
{
    Task<IReadOnlyList<HomepageBanner>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<HomepageBanner?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(HomepageBanner banner, CancellationToken cancellationToken = default);

    void Update(HomepageBanner banner);
}
