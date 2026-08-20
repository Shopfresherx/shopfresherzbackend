using ShopFresherz.Domain.Common;

namespace ShopFresherz.Domain.Entities;

/// <summary>
/// Represents a specific SKU variant of a product (e.g., 128GB Black, 256GB White).
/// Attributes stored as JSON (e.g. {"color":"Black","storage":"128GB"}).
/// </summary>
public class ProductVariant : BaseEntity
{
    /// <summary>Gets or sets the owning product ID.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Gets or sets the owning product navigation property.</summary>
    public Product Product { get; set; } = null!;

    /// <summary>Gets or sets the variant-level Stock Keeping Unit.</summary>
    public string SKU { get; set; } = string.Empty;

    /// <summary>Gets or sets the JSON blob of variant attributes (color, storage, RAM, etc.).</summary>
    public string AttributesJson { get; set; } = string.Empty;

    /// <summary>Gets or sets the variant-specific sell price (overrides base product price).</summary>
    public decimal Price { get; set; }

    /// <summary>Gets or sets the total stock quantity for this variant.</summary>
    public int StockQty { get; set; } = 0;

    /// <summary>Gets or sets the quantity reserved by pending orders.</summary>
    public int ReservedQty { get; set; } = 0;

    /// <summary>Gets the quantity available for new orders (StockQty − ReservedQty).</summary>
    public int AvailableQty => StockQty - ReservedQty;
}
