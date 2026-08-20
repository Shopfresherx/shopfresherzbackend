using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Infrastructure.Persistence.Configurations;

/// <summary>EF Core fluent configuration for the <see cref="Brand"/> entity.</summary>
public class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(150);

        builder.Property(x => x.Slug).IsRequired().HasMaxLength(200);
        builder.HasIndex(x => x.Slug).IsUnique();

        builder.Property(x => x.LogoUrl).HasMaxLength(500);

        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}
