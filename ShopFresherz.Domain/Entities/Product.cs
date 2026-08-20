using ShopFresherz.Domain.Common;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShopFresherz.Domain.Entities;
/// <summary>
/// Core product entity — the central aggregate in the ShopFresherz catalog.
/// Stock management uses pessimistic locking (SELECT WITH UPDLOCK) at checkout.
/// </summary>
public class Product : BaseEntity
{
    /// <summary>Gets or sets the unique Stock Keeping Unit code.</summary>
    public string SKU { get; set; } = string.Empty;

    /// <summary>Gets or sets the product display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the URL-safe slug for /product/{slug} routes.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Gets or sets the brand foreign key.</summary>
    public Guid BrandId { get; set; }

    /// <summary>Gets or sets the brand navigation property.</summary>
    public Brand Brand { get; set; } = null!;

    /// <summary>Gets or sets the leaf-node category foreign key (INT identity).</summary>
    public int CategoryId { get; set; }

    /// <summary>Gets or sets the category navigation property.</summary>
    public Category Category { get; set; } = null!;

    /// <summary>Gets or sets the full rich-HTML product description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the short marketing description (used on listing cards).</summary>
    public string? ShortDescription { get; set; }

    /// <summary>Gets or sets the current sell price (NGN). Price hierarchy: Flash &gt; Sale &gt; Base.</summary>
    public decimal Price { get; set; }

    /// <summary>Gets or sets the original RRP used to display strikethrough and discount badge.</summary>
    public decimal? CompareAtPrice { get; set; }

    /// <summary>Gets or sets the internal cost price (not exposed to API consumers).</summary>
    public decimal? CostPrice { get; set; }

    /// <summary>Gets or sets the total stock quantity across all variants/units.</summary>
    public int StockQty { get; set; } = 0;

    /// <summary>Gets or sets the quantity currently reserved by pending/awaiting-payment orders.</summary>
    public int ReservedQty { get; set; } = 0;

    /// <summary>Gets the quantity available for new cart additions (StockQty − ReservedQty).</summary>
    public int AvailableQty => StockQty - ReservedQty;

    /// <summary>Gets or sets the product weight in kilograms (used for delivery fee calculation).</summary>
    public decimal? WeightKg { get; set; }

    /// <summary>Gets or sets the JSON blob of category-specific technical attributes.</summary>
    public string? AttributesJson { get; set; }

    /// <summary>Gets or sets the JSON array of searchable tags.</summary>
    public string? TagsJson { get; set; }

    /// <summary>Gets or sets a value indicating whether this product is visible on the storefront.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether this product appears in featured sections.</summary>
    public bool IsFeatured { get; set; } = false;

    /// <summary>Gets or sets the denormalised average star rating (updated on review approval).</summary>
    public decimal AverageRating { get; set; } = 0;

    /// <summary>Gets or sets the denormalised review count (updated on review approval).</summary>
    public int ReviewCount { get; set; } = 0;

    /// <summary>Gets or sets the cumulative count of units sold (incremented on order delivery).</summary>
    public int SoldCount { get; set; } = 0;

    /// <summary>Gets or sets the product detail page view counter.</summary>
    public int ViewCount { get; set; } = 0;

    /// <summary>Gets or sets the SEO meta title (max 70 chars).</summary>
    public string? MetaTitle { get; set; }

    /// <summary>Gets or sets the SEO meta description (max 160 chars).</summary>
    public string? MetaDescription { get; set; }

    /// <summary>Gets or sets the ordered collection of product images including zoom derivatives.</summary>
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

    /// <summary>Gets or sets the collection of SKU variants (color, storage, RAM combinations).</summary>
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();

    /// <summary>Gets or sets the approved customer reviews for this product.</summary>
    public ICollection<Review> Reviews { get; set; } = new List<Review>();

    /// <summary>Gets or sets the order line items referencing this product.</summary>
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    /// <summary>Gets or sets the image URLs as a JSON array string (for admin input).</summary>
    [JsonIgnore]
    public string ImageUrlsJson { get; set; } = "[]";

    /// <summary>Gets the image URLs as a list of strings.</summary>
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public List<string> ImageUrls
    {
        get => string.IsNullOrWhiteSpace(ImageUrlsJson)
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(ImageUrlsJson)!;
        set => ImageUrlsJson = JsonSerializer.Serialize(value ?? new List<string>());
    }
}
