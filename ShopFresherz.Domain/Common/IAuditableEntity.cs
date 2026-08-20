namespace ShopFresherz.Domain.Common;

/// <summary>
/// Contract for entities that track creation and modification timestamps.
/// </summary>
public interface IAuditableEntity
{
    /// <summary>Gets or sets the UTC timestamp when the entity was created.</summary>
    DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the entity was last updated.</summary>
    DateTime? UpdatedAt { get; set; }
}
