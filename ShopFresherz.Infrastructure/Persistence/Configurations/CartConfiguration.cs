using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Infrastructure.Persistence.Configurations;

/// <summary>EF Core fluent configuration for the <see cref="Cart"/> entity.</summary>
public class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SessionId).HasMaxLength(128);
        builder.Property(x => x.CouponCode).HasMaxLength(50);

        // A user may have at most one active cart — enforce uniqueness when UserId is present.
        builder.HasIndex(x => x.UserId)
            .IsUnique()
            .HasFilter("[UserId] IS NOT NULL");

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Items)
            .WithOne(i => i.Cart)
            .HasForeignKey(i => i.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}
