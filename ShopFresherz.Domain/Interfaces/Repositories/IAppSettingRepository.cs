using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Domain.Interfaces.Repositories;

/// <summary>Repository contract for persisted admin settings.</summary>
public interface IAppSettingRepository
{
    Task<IReadOnlyList<AppSetting>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<AppSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);

    Task UpsertAsync(string key, string valueJson, CancellationToken cancellationToken = default);
}
