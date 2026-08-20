using System.ComponentModel.DataAnnotations.Schema;
using ShopFresherz.Domain.Common;

namespace ShopFresherz.Domain.Entities;

/// <summary>
/// A single line item within an order, capturing quantity, price, and a product snapshot.
/// The snapshot preserves product details at the time of purchase.
/// </summary>
public class OrderItem : BaseEntity
{
    /// <summary>Gets or sets the parent order ID.</summary>
    public Guid OrderId { get; set; }

    /// <summary>Gets or sets the parent order navigation property.</summary>
    public Order Order { get; set; } = null!;

    /// <summary>Gets or sets the product ID at time of purchase.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Gets or sets the product navigation property.</summary>
    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;

    /// <summary>Gets or sets the selected variant ID (null if no variants).</summary>
    public Guid? VariantId { get; set; }

    /// <summary>Gets or sets the selected variant navigation property.</summary>
    public ProductVariant? Variant { get; set; }

    /// <summary>Gets or sets the number of units ordered.</summary>
    public int Quantity { get; set; }

    /// <summary>Gets or sets the per-unit price at the time of order (server-calculated).</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Gets or sets the line total (UnitPrice × Quantity).</summary>
    public decimal LineTotal { get; set; }

    /// <summary>
    /// Gets or sets the JSON snapshot of the product at order time
    /// (name, SKU, images, variant attributes — immutable history).
    /// </summary>
    public string ProductSnapshotJson { get; set; } = string.Empty;
}
