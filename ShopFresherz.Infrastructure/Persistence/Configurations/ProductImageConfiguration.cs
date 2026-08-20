using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Infrastructure.Persistence.Configurations;

/// <summary>EF Core fluent configuration for the <see cref="ProductImage"/> entity.</summary>
public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ThumbUrl).IsRequired().HasMaxLength(500);
        builder.Property(x => x.DisplayUrl).IsRequired().HasMaxLength(500);
        builder.Property(x => x.ZoomUrl).IsRequired().HasMaxLength(500);
        builder.Property(x => x.OriginalUrl).IsRequired().HasMaxLength(500);

        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}
