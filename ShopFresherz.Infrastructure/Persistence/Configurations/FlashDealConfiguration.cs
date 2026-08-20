using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Infrastructure.Persistence.Configurations;

/// <summary>EF Core fluent configuration for the <see cref="FlashDeal"/> entity.</summary>
public class FlashDealConfiguration : IEntityTypeConfiguration<FlashDeal>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FlashDeal> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SalePrice)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.OriginalPrice)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        // Computed properties — not persisted to the database.
        builder.Ignore(x => x.IsExpired);
        builder.Ignore(x => x.IsLive);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(x => x.VariantId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}
