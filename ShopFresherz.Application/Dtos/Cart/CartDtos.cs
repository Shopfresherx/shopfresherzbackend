namespace ShopFresherz.Application.Dtos.Cart;

/// <summary>Represents a single line item in a shopping cart.</summary>
public sealed class CartItemDto
{
    /// <summary>Gets or sets the cart item ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the product ID.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Gets or sets the product name.</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Gets or sets the product URL slug.</summary>
    public string ProductSlug { get; set; } = string.Empty;

    /// <summary>Gets or sets the product's primary 540px display image URL.</summary>
    public string? ProductImageUrl { get; set; }

    /// <summary>Gets or sets the selected variant ID (null if no variants).</summary>
    public Guid? VariantId { get; set; }

    /// <summary>Gets or sets the variant attributes JSON (null if no variant).</summary>
    public string? VariantAttributesJson { get; set; }

    /// <summary>Gets or sets the effective unit price (variant price or base product price).</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Gets or sets the requested quantity.</summary>
    public int Quantity { get; set; }

    /// <summary>Gets the computed line total (UnitPrice × Quantity).</summary>
    public decimal LineTotal => UnitPrice * Quantity;
}

/// <summary>Full cart DTO returned to client.</summary>
public sealed class CartDto
{
    /// <summary>Gets or sets the cart ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the owning user ID (null for guest carts).</summary>
    public Guid? UserId { get; set; }

    /// <summary>Gets or sets the session ID for guest carts.</summary>
    public string? SessionId { get; set; }

    /// <summary>Gets or sets the cart expiry timestamp.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Gets or sets the applied coupon code.</summary>
    public string? CouponCode { get; set; }

    /// <summary>Gets or sets the line items in this cart.</summary>
    public IReadOnlyList<CartItemDto> Items { get; set; } = [];

    /// <summary>Gets the total number of items across all lines.</summary>
    public int TotalItems => Items.Sum(i => i.Quantity);

    /// <summary>Gets the computed cart subtotal.</summary>
    public decimal Subtotal => Items.Sum(i => i.LineTotal);
}

/// <summary>Request payload for adding a product to the cart.</summary>
public sealed class AddToCartRequest
{
    /// <summary>Gets or sets the product ID to add.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Gets or sets the specific variant ID (null if no variants).</summary>
    public Guid? VariantId { get; set; }

    /// <summary>Gets or sets the quantity to add (1–10).</summary>
    public int Quantity { get; set; } = 1;
}

/// <summary>Request payload for updating a cart line item quantity.</summary>
public sealed class UpdateCartItemRequest
{
    /// <summary>Gets or sets the new quantity (1–10).</summary>
    public int Quantity { get; set; }
}

/// <summary>Request payload for merging a guest cart into the authenticated user's cart on login.</summary>
public sealed class MergeCartRequest
{
    /// <summary>Gets or sets the guest session ID whose cart should be merged.</summary>
    public string SessionId { get; set; } = string.Empty;
}
