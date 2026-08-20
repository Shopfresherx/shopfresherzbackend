using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Infrastructure.Persistence.Configurations;

/// <summary>EF Core fluent configuration for the <see cref="Coupon"/> entity (INT identity PK).</summary>
public class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(50);
        builder.HasIndex(x => x.Code).IsUnique();

        builder.Property(x => x.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Value)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.MinimumOrderAmount)
            .HasColumnType("decimal(18,2)");

        // Restrict FK to specific product — product must exist if set.
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.RestrictToProductId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Restrict FK to specific category — category must exist if set.
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(x => x.RestrictToCategoryId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
