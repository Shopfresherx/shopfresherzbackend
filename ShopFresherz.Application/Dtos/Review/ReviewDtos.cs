namespace ShopFresherz.Application.Dtos.Review;

/// <summary>Product review DTO returned to clients.</summary>
public sealed class ReviewDto
{
    /// <summary>Gets or sets the review ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the reviewer's user ID.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the reviewer's display name.</summary>
    public string ReviewerName { get; set; } = string.Empty;

    /// <summary>Gets or sets the star rating (1–5).</summary>
    public int Rating { get; set; }

    /// <summary>Gets or sets the review headline.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the review body text.</summary>
    public string? Body { get; set; }

    /// <summary>Gets or sets a value indicating whether the reviewer is a verified purchaser.</summary>
    public bool IsVerifiedPurchase { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the review was submitted.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>Request payload for submitting a product review.</summary>
public sealed class CreateReviewRequest
{
    /// <summary>Gets or sets the product ID being reviewed.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Gets or sets the star rating (1–5).</summary>
    public int Rating { get; set; }

    /// <summary>Gets or sets the review headline.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the review body text.</summary>
    public string? Body { get; set; }
}
