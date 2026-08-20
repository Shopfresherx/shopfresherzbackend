using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Infrastructure.Persistence.Configurations;

/// <summary>EF Core fluent configuration for the <see cref="Review"/> entity.</summary>
public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.HasKey(x => x.Id);

        // A user can only review each product once.
        builder.HasIndex(x => new { x.UserId, x.ProductId }).IsUnique();

        builder.Property(x => x.Rating).IsRequired();

        builder.Property(x => x.Title).HasMaxLength(150);

        builder.Property(x => x.Body).HasMaxLength(2000);

        // Product → Reviews relationship is owned by ProductConfiguration (Cascade from Product).
        builder.HasOne(x => x.User)
            .WithMany(u => u.Reviews)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}
