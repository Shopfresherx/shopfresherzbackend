using ShopFresherz.Domain.Common;

namespace ShopFresherz.Domain.Entities;

/// <summary>
/// A customer product review. Only verified purchasers may submit reviews.
/// Reviews require admin approval before appearing on the PDP.
/// </summary>
public class Review : BaseEntity
{
    /// <summary>Gets or sets the reviewer's user ID.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the reviewer navigation property.</summary>
    public User User { get; set; } = null!;

    /// <summary>Gets or sets the reviewed product ID.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Gets or sets the reviewed product navigation property.</summary>
    public Product Product { get; set; } = null!;

    /// <summary>Gets or sets the star rating (1–5).</summary>
    public int Rating { get; set; }

    /// <summary>Gets or sets the optional review headline.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the full review body text.</summary>
    public string? Body { get; set; }

    /// <summary>Gets or sets a value indicating whether the reviewer has a confirmed purchase of this product.</summary>
    public bool IsVerifiedPurchase { get; set; } = false;

    /// <summary>Gets or sets a value indicating whether an admin has approved this review for public display.</summary>
    public bool IsApproved { get; set; } = false;
}
