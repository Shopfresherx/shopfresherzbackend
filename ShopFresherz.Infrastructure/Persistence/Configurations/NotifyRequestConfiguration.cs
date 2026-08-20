using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Infrastructure.Persistence.Configurations;

/// <summary>EF Core fluent configuration for the <see cref="NotifyRequest"/> entity.</summary>
public class NotifyRequestConfiguration : IEntityTypeConfiguration<NotifyRequest>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<NotifyRequest> builder)
    {
        builder.HasKey(x => x.Id);

        // A user can only have one active back-in-stock request per product.
        builder.HasIndex(x => new { x.UserId, x.ProductId }).IsUnique();

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}
