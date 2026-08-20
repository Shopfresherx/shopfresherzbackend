using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Infrastructure.Jobs;

/// <summary>Deactivates active flash deals whose end time has passed.</summary>
public sealed class FlashDealExpiryJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FlashDealExpiryJob> _logger;

    public FlashDealExpiryJob(
        IServiceScopeFactory scopeFactory,
        ILogger<FlashDealExpiryJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>Executes the flash deal expiry job.</summary>
    public async Task ExecuteAsync()
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IUnitOfWork uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        IReadOnlyList<FlashDeal> expired =
            await uow.FlashDeals.GetExpiredActiveAsync(CancellationToken.None);

        foreach (FlashDeal deal in expired)
        {
            deal.IsActive = false;
            uow.FlashDeals.Update(deal);
        }

        if (expired.Count > 0)
        {
            await uow.SaveChangesAsync(CancellationToken.None);
            _logger.LogInformation("FlashDealExpiryJob deactivated {Count} expired deals.", expired.Count);
        }
    }
}
