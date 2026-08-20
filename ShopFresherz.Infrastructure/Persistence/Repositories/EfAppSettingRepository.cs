using Microsoft.EntityFrameworkCore;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces.Repositories;

namespace ShopFresherz.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IAppSettingRepository"/>.</summary>
internal sealed class EfAppSettingRepository : IAppSettingRepository
{
    private readonly ShopFresherzDbContext _context;

    public EfAppSettingRepository(ShopFresherzDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AppSetting>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AppSettings
            .OrderBy(x => x.Key)
            .ToListAsync(cancellationToken);
    }

    public async Task<AppSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _context.AppSettings
            .FirstOrDefaultAsync(x => x.Key == key, cancellationToken);
    }

    public async Task UpsertAsync(string key, string valueJson, CancellationToken cancellationToken = default)
    {
        AppSetting? setting = await GetByKeyAsync(key, cancellationToken);
        if (setting is null)
        {
            await _context.AppSettings.AddAsync(
                new AppSetting { Key = key, ValueJson = valueJson },
                cancellationToken);
            return;
        }

        setting.ValueJson = valueJson;
        setting.UpdatedAt = DateTime.UtcNow;
    }
}
