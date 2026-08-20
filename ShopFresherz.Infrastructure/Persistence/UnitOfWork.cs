using ShopFresherz.Domain.Interfaces;
using ShopFresherz.Domain.Interfaces.Repositories;
using ShopFresherz.Infrastructure.Persistence.Repositories;

namespace ShopFresherz.Infrastructure.Persistence;

/// <summary>
/// EF Core Unit of Work — coordinates all repository operations through a single <see cref="ShopFresherzDbContext"/>.
/// Repositories are lazily instantiated on first access.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ShopFresherzDbContext _context;

    private IUserRepository?       _users;
    private IProductRepository?    _products;
    private ICategoryRepository?   _categories;
    private IBrandRepository?      _brands;
    private IOrderRepository?      _orders;
    private ICartRepository?       _carts;
    private IReviewRepository?     _reviews;
    private IFlashDealRepository?  _flashDeals;
    private ICouponRepository?     _coupons;
    private IAddressRepository?    _addresses;
    private IWishlistRepository?   _wishlists;
    private ILoyaltyTransactionRepository? _loyaltyTransactions;
    private INotificationRepository? _notifications;
    private INotifyRequestRepository? _notifyRequests;
    private IHomepageBannerRepository? _homepageBanners;
    private IAppSettingRepository? _appSettings;
    private IPaymentMethodRepository? _paymentMethods;
    private IFeaturedSectionRepository? _featuredSections;
    private IPromotionalSectionRepository? _promotionalSections;

    /// <summary>Initialises a new instance of <see cref="UnitOfWork"/>.</summary>
    public UnitOfWork(ShopFresherzDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public IUserRepository Users =>
        _users ??= new EfUserRepository(_context);

    /// <inheritdoc />
    public IProductRepository Products =>
        _products ??= new EfProductRepository(_context);

    /// <inheritdoc />
    public ICategoryRepository Categories =>
        _categories ??= new EfCategoryRepository(_context);

    /// <inheritdoc />
    public IBrandRepository Brands =>
        _brands ??= new EfBrandRepository(_context);

    /// <inheritdoc />
    public IOrderRepository Orders =>
        _orders ??= new EfOrderRepository(_context);

    /// <inheritdoc />
    public ICartRepository Carts =>
        _carts ??= new EfCartRepository(_context);

    /// <inheritdoc />
    public IReviewRepository Reviews =>
        _reviews ??= new EfReviewRepository(_context);

    /// <inheritdoc />
    public IFlashDealRepository FlashDeals =>
        _flashDeals ??= new EfFlashDealRepository(_context);

    /// <inheritdoc />
    public ICouponRepository Coupons =>
        _coupons ??= new EfCouponRepository(_context);

    /// <inheritdoc />
    public IAddressRepository Addresses =>
        _addresses ??= new EfAddressRepository(_context);

    /// <inheritdoc />
    public IWishlistRepository Wishlists =>
        _wishlists ??= new EfWishlistRepository(_context);

    /// <inheritdoc />
    public ILoyaltyTransactionRepository LoyaltyTransactions =>
        _loyaltyTransactions ??= new EfLoyaltyTransactionRepository(_context);

    /// <inheritdoc />
    public INotificationRepository Notifications =>
        _notifications ??= new EfNotificationRepository(_context);

    /// <inheritdoc />
    public INotifyRequestRepository NotifyRequests =>
        _notifyRequests ??= new EfNotifyRequestRepository(_context);

    /// <inheritdoc />
    public IHomepageBannerRepository HomepageBanners =>
        _homepageBanners ??= new EfHomepageBannerRepository(_context);

    /// <inheritdoc />
    public IAppSettingRepository AppSettings =>
        _appSettings ??= new EfAppSettingRepository(_context);

    /// <inheritdoc />
    public IPaymentMethodRepository PaymentMethods =>
        _paymentMethods ??= new EfPaymentMethodRepository(_context);

    /// <inheritdoc />
    public IFeaturedSectionRepository FeaturedSections =>
        _featuredSections ??= new EfFeaturedSectionRepository(_context);

    /// <inheritdoc />
    public IPromotionalSectionRepository PromotionalSections =>
        _promotionalSections ??= new EfPromotionalSectionRepository(_context);

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _context.BeginTransactionAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _context.CommitTransactionAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _context.RollbackTransactionAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _context.Dispose();
    }
}
