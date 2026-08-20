using ShopFresherz.Domain.Interfaces.Repositories;

namespace ShopFresherz.Domain.Interfaces;

/// <summary>
/// Unit of Work pattern — coordinates all repository operations within a single DB transaction.
/// Call SaveChangesAsync() to commit all pending changes atomically.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>Gets the user repository.</summary>
    IUserRepository Users { get; }

    /// <summary>Gets the product repository.</summary>
    IProductRepository Products { get; }

    /// <summary>Gets the category repository.</summary>
    ICategoryRepository Categories { get; }

    /// <summary>Gets the brand repository.</summary>
    IBrandRepository Brands { get; }

    /// <summary>Gets the order repository.</summary>
    IOrderRepository Orders { get; }

    /// <summary>Gets the cart repository.</summary>
    ICartRepository Carts { get; }

    /// <summary>Gets the review repository.</summary>
    IReviewRepository Reviews { get; }

    /// <summary>Gets the flash deal repository.</summary>
    IFlashDealRepository FlashDeals { get; }

    /// <summary>Gets the coupon repository.</summary>
    ICouponRepository Coupons { get; }

    /// <summary>Gets the address repository.</summary>
    IAddressRepository Addresses { get; }

    /// <summary>Gets the wishlist repository.</summary>
    IWishlistRepository Wishlists { get; }

    /// <summary>Gets the loyalty transaction repository.</summary>
    ILoyaltyTransactionRepository LoyaltyTransactions { get; }

    /// <summary>Gets the back-in-stock notification request repository.</summary>
    INotifyRequestRepository NotifyRequests { get; }

    /// <summary>Gets the homepage banner repository.</summary>
    IHomepageBannerRepository HomepageBanners { get; }

    /// <summary>Gets the application settings repository.</summary>
    IAppSettingRepository AppSettings { get; }

    /// <summary>Gets the payment method repository.</summary>
    IPaymentMethodRepository PaymentMethods { get; }

    /// <summary>Gets the featured section repository.</summary>
    IFeaturedSectionRepository FeaturedSections { get; }

    /// <summary>Gets the promotional sections repository.</summary>
    IPromotionalSectionRepository PromotionalSections { get; }

    /// <summary>
    /// Commits all pending changes to the database atomically.
    /// Returns the number of state entries written to the database.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Begins an explicit database transaction for multi-step operations (e.g., stock reservation).</summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>Commits the current explicit transaction.</summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>Rolls back the current explicit transaction.</summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
