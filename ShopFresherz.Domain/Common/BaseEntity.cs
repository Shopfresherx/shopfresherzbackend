namespace ShopFresherz.Domain.Common;

/// <summary>
/// Base class for all domain entities with audit fields and soft-delete support.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>Gets or sets the unique identifier for this entity.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the UTC timestamp when this entity was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the UTC timestamp when this entity was last updated.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Gets or sets the UTC timestamp when this entity was soft-deleted.</summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>Gets a value indicating whether this entity has been soft-deleted.</summary>
    public bool IsDeleted => DeletedAt.HasValue;
}
