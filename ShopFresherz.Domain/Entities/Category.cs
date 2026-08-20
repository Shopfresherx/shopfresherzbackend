using ShopFresherz.Domain.Common;

namespace ShopFresherz.Domain.Entities;

/// <summary>
/// Represents a product category node in the three-level taxonomy tree.
/// Uses INT identity PK (lookup table convention per PRD).
/// </summary>
public class Category : IAuditableEntity
{
    /// <summary>Gets or sets the category's integer identity primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the parent category ID (null for top-level categories).</summary>
    public int? ParentId { get; set; }

    /// <summary>Gets or sets the parent category navigation property.</summary>
    public Category? Parent { get; set; }

    /// <summary>Gets or sets the child categories.</summary>
    public ICollection<Category> Children { get; set; } = new List<Category>();

    /// <summary>Gets or sets the category display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the URL-safe slug for /category/{slug} routes.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Gets or sets the CDN URL for the category icon/image.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Gets or sets the display order position in the navigation bar.</summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>Gets or sets a value indicating whether this category is visible on the storefront.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets the SEO meta title (max 70 chars).</summary>
    public string? MetaTitle { get; set; }

    /// <summary>Gets or sets the SEO meta description (max 160 chars).</summary>
    public string? MetaDescription { get; set; }

    /// <summary>Gets or sets the products belonging to this category leaf node.</summary>
    public ICollection<Product> Products { get; set; } = new List<Product>();

    /// <inheritdoc />
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <inheritdoc />
    public DateTime? UpdatedAt { get; set; }
}
