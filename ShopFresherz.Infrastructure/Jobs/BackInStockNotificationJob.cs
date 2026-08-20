using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;
using ShopFresherz.Domain.Interfaces.Services;

namespace ShopFresherz.Infrastructure.Jobs;

/// <summary>Sends back-in-stock notifications for products with newly available stock.</summary>
public sealed class BackInStockNotificationJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEmailService _email;
    private readonly ILogger<BackInStockNotificationJob> _logger;

    public BackInStockNotificationJob(
        IServiceScopeFactory scopeFactory,
        IEmailService email,
        ILogger<BackInStockNotificationJob> logger)
    {
        _scopeFactory = scopeFactory;
        _email = email;
        _logger = logger;
    }

    /// <summary>Executes the back-in-stock notification job.</summary>
    public async Task ExecuteAsync()
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IUnitOfWork uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        IReadOnlyList<NotifyRequest> pending =
            await uow.NotifyRequests.GetPendingAsync(CancellationToken.None);

        int notified = 0;
        foreach (NotifyRequest request in pending)
        {
            if (request.Product.AvailableQty <= 0)
            {
                continue;
            }

            try
            {
                await _email.SendBackInStockAsync(
                    request.User.Email,
                    request.User.FirstName,
                    request.Product.Name,
                    request.Product.Slug,
                    CancellationToken.None);

                request.IsNotified = true;
                notified++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to send back-in-stock notification for product {ProductId} to user {UserId}.",
                    request.ProductId,
                    request.UserId);
            }
        }

        if (notified > 0)
        {
            await uow.SaveChangesAsync(CancellationToken.None);
            _logger.LogInformation("BackInStockNotificationJob sent {Count} notifications.", notified);
        }
    }
}
