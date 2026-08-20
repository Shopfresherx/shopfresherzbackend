using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Infrastructure.Persistence.Configurations;

/// <summary>EF Core fluent configuration for the <see cref="Product"/> entity.</summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SKU)
            .IsRequired()
            .HasMaxLength(100);
        builder.HasIndex(x => x.SKU).IsUnique();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(x => x.Slug)
            .IsRequired()
            .HasMaxLength(350);
        builder.HasIndex(x => x.Slug).IsUnique();

        builder.Property(x => x.Price)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.CompareAtPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.CostPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.AverageRating)
            .HasColumnType("decimal(3,2)")
            .HasDefaultValue(0m);

        builder.Property(x => x.WeightKg)
            .HasColumnType("decimal(8,3)");

        builder.Property(x => x.MetaTitle)
            .HasMaxLength(70);

        builder.Property(x => x.MetaDescription)
            .HasMaxLength(160);

        // Ignore computed property — not mapped to a column
        builder.Ignore(x => x.AvailableQty);

        builder.HasOne(x => x.Brand)
            .WithMany(b => b.Products)
            .HasForeignKey(x => x.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Images)
            .WithOne(i => i.Product)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Variants)
            .WithOne(v => v.Product)
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Reviews)
            .WithOne(r => r.Product)
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // OrderItems navigation exists on Product but no matching collection exists for
        // the other FK-dependent entities — configure explicitly to prevent EF Core from
        // creating duplicate shadow navigations.
        builder.HasMany(x => x.OrderItems)
            .WithOne(i => i.Product)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}
