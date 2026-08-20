using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Infrastructure.Persistence.Configurations;

/// <summary>EF Core fluent configuration for the <see cref="SavedCard"/> entity.</summary>
public sealed class SavedCardConfiguration : IEntityTypeConfiguration<SavedCard>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SavedCard> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CardType).IsRequired().HasMaxLength(30);
        builder.Property(x => x.CardNumber).IsRequired().HasMaxLength(19);
        builder.Property(x => x.CardHolderName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.AuthorizationCode).HasMaxLength(512);

        builder.HasOne(x => x.User)
            .WithMany(u => u.SavedCards)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}
