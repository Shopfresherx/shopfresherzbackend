using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Infrastructure.Persistence.Configurations;

/// <summary>EF Core fluent configuration for the <see cref="User"/> entity.</summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(255);
        builder.HasIndex(x => x.Email).IsUnique();

        builder.Property(x => x.Phone)
            .HasMaxLength(20);
        builder.HasIndex(x => x.Phone).IsUnique().HasFilter("[Phone] IS NOT NULL");

        builder.Property(x => x.PasswordHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(x => x.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.AvatarUrl)
            .HasMaxLength(500);

        builder.Property(x => x.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.GoogleId)
            .HasMaxLength(255);
        builder.HasIndex(x => x.GoogleId).IsUnique().HasFilter("[GoogleId] IS NOT NULL");

        builder.Property(x => x.RefreshTokenHash)
            .HasMaxLength(512);

        builder.Property(x => x.PasswordResetOtpHash)
            .HasMaxLength(64);

        builder.HasMany(x => x.Orders)
            .WithOne(o => o.User)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Addresses)
            .WithOne(a => a.User)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Reviews)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Wishlist)
            .WithOne(w => w.User)
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.LoyaltyTransactions)
            .WithOne(lt => lt.User)
            .HasForeignKey(lt => lt.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.NotificationOrderUpdates)
            .HasDefaultValue(true);
        builder.Property(x => x.NotificationPromotions)
            .HasDefaultValue(true);
        builder.Property(x => x.NotificationBackInStock)
            .HasDefaultValue(true);
        builder.Property(x => x.NotificationWishlistReminders)
            .HasDefaultValue(true);
        builder.Property(x => x.NotificationReviewReminders)
            .HasDefaultValue(true);

        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}
