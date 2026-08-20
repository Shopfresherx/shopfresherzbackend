using ShopFresherz.Domain.Common;

namespace ShopFresherz.Domain.Entities;

/// <summary>
/// Represents a product image with all four zoom-pipeline derivatives
/// (80px thumb, 540px display, 1600px zoom, original).
/// </summary>
public class ProductImage : BaseEntity
{
    /// <summary>Gets or sets the owning product ID.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Gets or sets the owning product navigation property.</summary>
    public Product Product { get; set; } = null!;

    /// <summary>Gets or sets the CDN URL for the 80×80 LQIP thumbnail.</summary>
    public string ThumbUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the CDN URL for the 540×540 main display image (WebP).</summary>
    public string DisplayUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the CDN URL for the 1600×1600 zoom-source image (WebP).</summary>
    public string ZoomUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the CDN URL for the original upload (up to 4000×4000).</summary>
    public string OriginalUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the display sort order (0-based, drag-drop in admin).</summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>Gets or sets a value indicating whether this slot holds a product video.</summary>
    public bool IsVideo { get; set; } = false;
}
