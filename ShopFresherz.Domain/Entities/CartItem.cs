using ShopFresherz.Domain.Common;

namespace ShopFresherz.Domain.Entities;

/// <summary>
/// A single product line in a shopping cart.
/// Quantity is capped server-side at min(availableStock, 10) per line item.
/// </summary>
public class CartItem : BaseEntity
{
    /// <summary>Gets or sets the parent cart ID.</summary>
    public Guid CartId { get; set; }

    /// <summary>Gets or sets the parent cart navigation property.</summary>
    public Cart Cart { get; set; } = null!;

    /// <summary>Gets or sets the product ID.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Gets or sets the product navigation property.</summary>
    public Product Product { get; set; } = null!;

    /// <summary>Gets or sets the selected variant ID (null if the product has no variants).</summary>
    public Guid? VariantId { get; set; }

    /// <summary>Gets or sets the selected variant navigation property.</summary>
    public ProductVariant? Variant { get; set; }

    /// <summary>Gets or sets the desired quantity (max 10 per line item).</summary>
    public int Quantity { get; set; }
}
