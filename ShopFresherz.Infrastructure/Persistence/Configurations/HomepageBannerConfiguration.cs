using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for homepage banners.</summary>
public sealed class HomepageBannerConfiguration : IEntityTypeConfiguration<HomepageBanner>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<HomepageBanner> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ImageUrl).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.LinkUrl).HasMaxLength(1000);
        builder.Property(x => x.SubTitle).HasMaxLength(300);
        builder.Property(x => x.CtaText).HasMaxLength(80);
        builder.Property(x => x.Tag).HasMaxLength(100);
        builder.HasIndex(x => new { x.IsActive, x.SortOrder });
        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}
