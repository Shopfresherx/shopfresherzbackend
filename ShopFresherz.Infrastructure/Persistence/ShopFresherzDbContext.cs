using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ShopFresherz.Domain.Common;
using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core DbContext for the ShopFresherz platform.
/// Applies all entity configurations from the Configurations folder,
/// auto-sets UpdatedAt on save, and enforces soft-delete global query filters.
/// </summary>
public class ShopFresherzDbContext : DbContext
{
    private IDbContextTransaction? _currentTransaction;

    /// <summary>Initialises a new instance of <see cref="ShopFresherzDbContext"/>.</summary>
    public ShopFresherzDbContext(DbContextOptions<ShopFresherzDbContext> options) : base(options)
    {
    }

    // ── DbSets ──────────────────────────────────────────────────────────────

    /// <summary>Gets or sets the Users table.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Gets or sets the Products table.</summary>
    public DbSet<Product> Products => Set<Product>();

    /// <summary>Gets or sets the ProductImages table.</summary>
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

    /// <summary>Gets or sets the ProductVariants table.</summary>
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();

    /// <summary>Gets or sets the Categories table.</summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <summary>Gets or sets the Brands table.</summary>
    public DbSet<Brand> Brands => Set<Brand>();

    /// <summary>Gets or sets the Orders table.</summary>
    public DbSet<Order> Orders => Set<Order>();

    /// <summary>Gets or sets the OrderItems table.</summary>
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    /// <summary>Gets or sets the Carts table.</summary>
    public DbSet<Cart> Carts => Set<Cart>();

    /// <summary>Gets or sets the CartItems table.</summary>
    public DbSet<CartItem> CartItems => Set<CartItem>();

    /// <summary>Gets or sets the Reviews table.</summary>
    public DbSet<Review> Reviews => Set<Review>();

    /// <summary>Gets or sets the FlashDeals table.</summary>
    public DbSet<FlashDeal> FlashDeals => Set<FlashDeal>();

    /// <summary>Gets or sets the Coupons table.</summary>
    public DbSet<Coupon> Coupons => Set<Coupon>();

    /// <summary>Gets or sets the Addresses table.</summary>
    public DbSet<Address> Addresses => Set<Address>();

    /// <summary>Gets or sets the Wishlists table.</summary>
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();

    /// <summary>Gets or sets the LoyaltyTransactions table.</summary>
    public DbSet<LoyaltyTransaction> LoyaltyTransactions => Set<LoyaltyTransaction>();

    /// <summary>Gets or sets the NotifyRequests table.</summary>
    public DbSet<NotifyRequest> NotifyRequests => Set<NotifyRequest>();

    /// Gets or sets the AuditLogs table.</summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    /// <summary>Gets or sets the Notifications table.</summary>
    public DbSet<Notification> Notifications => Set<Notification>();

    /// <summary>Gets or sets the HomepageBanners table.</summary>
    public DbSet<HomepageBanner> HomepageBanners => Set<HomepageBanner>();

    /// <summary>Gets or sets the AppSettings table.</summary>
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    /// <summary>Gets or sets the SavedCards (bank cards) table.</summary>
    public DbSet<SavedCard> SavedCards => Set<SavedCard>();

    /// <summary>Gets or sets the FeaturedSections table.</summary>
    public DbSet<FeaturedSection> FeaturedSections => Set<FeaturedSection>();

    /// <summary>Gets or sets the PromotionalSections table.</summary>
    public DbSet<PromotionalSection> PromotionalSections => Set<PromotionalSection>();

    // ── Model configuration ─────────────────────────────────────────────────

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration<T> implementations in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShopFresherzDbContext).Assembly);
    }

    // ── SaveChanges overrides ───────────────────────────────────────────────

    /// <inheritdoc />
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetAuditTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override int SaveChanges()
    {
        SetAuditTimestamps();
        return base.SaveChanges();
    }

    // ── Transaction helpers ─────────────────────────────────────────────────

    /// <summary>Begins an explicit database transaction.</summary>
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            return; // already in a transaction
        }

        _currentTransaction = await Database.BeginTransactionAsync(cancellationToken);
    }

    /// <summary>Commits the current explicit transaction.</summary>
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No active transaction to commit.");
        }

        try
        {
            await SaveChangesAsync(cancellationToken);
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    /// <summary>Rolls back the current explicit transaction.</summary>
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            return;
        }

        try
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    // ── Private helpers ─────────────────────────────────────────────────────

    private void SetAuditTimestamps()
    {
        DateTime utcNow = DateTime.UtcNow;

        foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<BaseEntity> entry in
                 ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = utcNow;
            }
        }

        foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Category> entry in
                 ChangeTracker.Entries<Category>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = utcNow;
            }
        }
    }
}
