using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Infrastructure.Persistence.Configurations;

/// <summary>EF Core fluent configuration for <see cref="PromotionalSection"/>.</summary>
public sealed class PromotionalSectionConfiguration : IEntityTypeConfiguration<PromotionalSection>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PromotionalSection> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SectionKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(x => x.SectionKey);

        builder.Property(x => x.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.SlugId)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(x => x.SlugId).IsUnique().HasFilter("[DeletedAt] IS NULL");

        builder.Property(x => x.Slug).HasMaxLength(350);

        builder.HasIndex(x => new { x.SectionKey, x.ProductId });

        builder.Property(x => x.Tag).HasMaxLength(200);
        builder.Property(x => x.Badge).HasMaxLength(100);
        builder.Property(x => x.Title).HasMaxLength(500);
        builder.Property(x => x.Subtitle).HasMaxLength(500);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Headline).HasMaxLength(200);
        builder.Property(x => x.CtaText).HasMaxLength(100);
        builder.Property(x => x.ButtonText).HasMaxLength(100);
        builder.Property(x => x.ImageUrl).HasMaxLength(1000);
        builder.Property(x => x.VideoUrl).HasMaxLength(1000);
        builder.Property(x => x.ImageAlt).HasMaxLength(300);
        builder.Property(x => x.PriceText).HasMaxLength(100);
        builder.Property(x => x.PriceLabel).HasMaxLength(100);
        builder.Property(x => x.PriceValue).HasMaxLength(100);
        builder.Property(x => x.PriceBadge).HasMaxLength(200);
        builder.Property(x => x.OriginalPriceText).HasMaxLength(100);
        builder.Property(x => x.SalePriceText).HasMaxLength(100);
        builder.Property(x => x.Rating).HasPrecision(3, 2);

        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}
