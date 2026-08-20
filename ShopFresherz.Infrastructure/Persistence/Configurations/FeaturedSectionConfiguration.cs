using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Infrastructure.Persistence.Configurations;

/// <summary>EF Core fluent configuration for the <see cref="FeaturedSection"/> entity.</summary>
public sealed class FeaturedSectionConfiguration : IEntityTypeConfiguration<FeaturedSection>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FeaturedSection> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Section).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(350);
        builder.Property(x => x.ImageUrl).HasMaxLength(1000);
        builder.Property(x => x.Price).HasMaxLength(50);
        builder.Property(x => x.Badge).HasMaxLength(50);
        builder.Property(x => x.Tag).HasMaxLength(200);
        builder.Property(x => x.CtaText).HasMaxLength(100);

        builder.HasIndex(x => x.Section);

        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}
