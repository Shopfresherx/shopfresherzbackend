using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for persisted admin settings.</summary>
public sealed class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> builder)
    {
        builder.ToTable("AppSettings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Key)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.ValueJson)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.HasIndex(x => x.Key)
            .IsUnique();

        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}
