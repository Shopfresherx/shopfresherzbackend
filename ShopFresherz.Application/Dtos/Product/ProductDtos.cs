using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShopFresherz.Application.Dtos.Product;

/// <summary>Brand summary DTO used in product listings.</summary>
public sealed class BrandDto
{
    /// <summary>Gets or sets the brand's unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the brand name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the brand slug.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Gets or sets the CDN URL for the brand logo.</summary>
    public string? LogoUrl { get; set; }
}

/// <summary>Variant attribute key-value pair.</summary>
public sealed class VariantAttributeDto
{
    /// <summary>Gets or sets the attribute name (e.g., "Color", "Storage").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the attribute value (e.g., "Black", "128GB").</summary>
    public string Value { get; set; } = string.Empty;
}

/// <summary>Category summary DTO used in product listings.</summary>
public sealed class CategoryDto
{
    /// <summary>Gets or sets the category's integer primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the parent category ID.</summary>
    public int? ParentId { get; set; }

    /// <summary>Gets or sets the category display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the category URL slug.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Gets or sets the CDN URL for the category image.</summary>
    public string? ImageUrl { get; set; }
}

/// <summary>Product image DTO with all four zoom-pipeline derivative URLs.</summary>
public sealed class ProductImageDto
{
    /// <summary>Gets or sets the image record ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the 80×80 thumbnail URL (LQIP).</summary>
    public string ThumbUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the 540×540 display image URL (WebP).</summary>
    public string DisplayUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the 1600×1600 zoom source URL (WebP).</summary>
    public string ZoomUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the original upload URL.</summary>
    public string OriginalUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the sort order position.</summary>
    public int SortOrder { get; set; }

    /// <summary>Gets or sets a value indicating whether this slot is a video.</summary>
    public bool IsVideo { get; set; }
}

/// <summary>Product variant DTO representing a specific SKU (e.g., 128GB Black).</summary>
public sealed class ProductVariantDto
{
    /// <summary>Gets or sets the variant ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the variant SKU.</summary>
    public string SKU { get; set; } = string.Empty;

    /// <summary>Gets or sets the variant attributes as key-value pairs.</summary>
    public IReadOnlyList<VariantAttributeDto> Attributes { get; set; } = [];

    /// <summary>Gets or sets the variant-specific sell price.</summary>
    public decimal Price { get; set; }

    /// <summary>Gets or sets the total stock quantity for this variant.</summary>
    public int StockQty { get; set; }

    /// <summary>Gets or sets the available quantity (StockQty − ReservedQty).</summary>
    public int AvailableQty { get; set; }
}

/// <summary>Lightweight product summary used on listing cards.</summary>
public class ProductSummaryDto
{
    /// <summary>Gets or sets the product ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the product name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the URL slug.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Gets or sets the Stock Keeping Unit code.</summary>
    public string SKU { get; set; } = string.Empty;

    /// <summary>Gets or sets the current sell price.</summary>
    public decimal Price { get; set; }

    /// <summary>Gets or sets the original RRP for strikethrough display.</summary>
    public decimal? CompareAtPrice { get; set; }

    /// <summary>Gets or sets the primary display image URL (540px WebP).</summary>
    public string? PrimaryImageUrl { get; set; }

    /// <summary>Gets or sets the average star rating.</summary>
    public decimal AverageRating { get; set; }

    /// <summary>Gets or sets the total number of approved reviews.</summary>
    public int ReviewCount { get; set; }

    /// <summary>Gets or sets a value indicating whether the product is visible.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets a value indicating whether the product is featured.</summary>
    public bool IsFeatured { get; set; }

    /// <summary>Gets or sets the quantity available for purchase.</summary>
    public int AvailableQty { get; set; }

    /// <summary>Gets or sets the brand summary.</summary>
    public BrandDto? Brand { get; set; }

    /// <summary>Gets or sets the category summary.</summary>
    public CategoryDto? Category { get; set; }

    /// <summary>Gets or sets the product image URLs supplied through admin product input.</summary>
    public IReadOnlyList<string> ImageUrls { get; set; } = [];
}

/// <summary>Full product detail DTO for the Product Detail Page (PDP).</summary>
public sealed class ProductDetailDto : ProductSummaryDto
{
    /// <summary>Gets or sets the full rich-HTML product description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the short marketing description.</summary>
    public string? ShortDescription { get; set; }

    /// <summary>Gets or sets the product images ordered by SortOrder.</summary>
    public IReadOnlyList<ProductImageDto> Images { get; set; } = [];

    /// <summary>Gets or sets the available SKU variants.</summary>
    public IReadOnlyList<ProductVariantDto> Variants { get; set; } = [];

    /// <summary>Gets or sets the JSON blob of technical attributes.</summary>
    public string? AttributesJson { get; set; }

    /// <summary>Gets or sets the JSON array of searchable tags.</summary>
    public string? TagsJson { get; set; }

    /// <summary>Gets or sets the weight in kilograms.</summary>
    public decimal? WeightKg { get; set; }

    /// <summary>Gets or sets the SEO meta title.</summary>
    public string? MetaTitle { get; set; }

    /// <summary>Gets or sets the SEO meta description.</summary>
    public string? MetaDescription { get; set; }

    /// <summary>Gets or sets total units sold.</summary>
    public int SoldCount { get; set; }

    /// <summary>Gets or sets product SKU.</summary>
    public string SKU { get; set; } = string.Empty;
}

/// <summary>Request payload for creating a category (admin).</summary>
public sealed class CreateCategoryRequest
{
    /// <summary>Gets or sets the category name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the URL slug (auto-generated from name if omitted).</summary>
    public string? Slug { get; set; }

    /// <summary>Gets or sets the parent category ID (null = top-level).</summary>
    public int? ParentId { get; set; }

    /// <summary>Gets or sets the CDN URL for the category image.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Gets or sets the display sort order.</summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>Gets or sets whether the category is visible on the storefront.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets the SEO meta title.</summary>
    public string? MetaTitle { get; set; }

    /// <summary>Gets or sets the SEO meta description.</summary>
    public string? MetaDescription { get; set; }
}

/// <summary>Request payload for updating a category (admin). All fields optional.</summary>
public sealed class UpdateCategoryRequest
{
    /// <summary>Gets or sets the updated name.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the updated slug.</summary>
    public string? Slug { get; set; }

    /// <summary>Gets or sets the updated parent category ID.</summary>
    public int? ParentId { get; set; }

    /// <summary>Gets or sets the updated image URL.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Gets or sets the updated sort order.</summary>
    public int? SortOrder { get; set; }

    /// <summary>Gets or sets the updated active flag.</summary>
    public bool? IsActive { get; set; }

    /// <summary>Gets or sets the updated SEO meta title.</summary>
    public string? MetaTitle { get; set; }

    /// <summary>Gets or sets the updated SEO meta description.</summary>
    public string? MetaDescription { get; set; }
}

/// <summary>Request payload for creating a new product (admin).</summary>
public sealed class CreateProductRequest
{
    /// <summary>Gets or sets the SKU.</summary>
    public string SKU { get; set; } = string.Empty;

    /// <summary>Gets or sets the product name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the URL slug (auto-generated if omitted).</summary>
    public string? Slug { get; set; }

    /// <summary>Gets or sets the brand ID.</summary>
    public Guid BrandId { get; set; }

    /// <summary>Gets or sets the category ID.</summary>
    public int CategoryId { get; set; }

    /// <summary>Gets or sets the full product description HTML.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the short marketing description.</summary>
    public string? ShortDescription { get; set; }

    /// <summary>Gets or sets the sell price.</summary>
    public decimal Price { get; set; }

    /// <summary>Gets or sets the original RRP.</summary>
    public decimal? CompareAtPrice { get; set; }

    /// <summary>Gets or sets the internal cost price.</summary>
    public decimal? CostPrice { get; set; }

    /// <summary>Gets or sets the initial stock quantity.</summary>
    public int StockQty { get; set; }

    /// <summary>Gets or sets the weight in kilograms.</summary>
    public decimal? WeightKg { get; set; }

    /// <summary>Gets or sets technical attributes JSON.</summary>
    public string? AttributesJson { get; set; }

    /// <summary>Gets or sets tags JSON array.</summary>
    public string? TagsJson { get; set; }

    /// <summary>Gets or sets a single tag value from admin product forms.</summary>
    public string? Tag
    {
        get => null;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                TagsJson = JsonSerializer.Serialize(new[] { value.Trim() });
            }
        }
    }

    /// <summary>Gets or sets a value indicating whether the product is active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the product is featured.</summary>
    public bool IsFeatured { get; set; } = false;

    /// <summary>Gets or sets the image URLs (array of strings).</summary>
    public List<string> ImageUrls { get; set; } = new List<string>();

    /// <summary>Gets or sets the initial average rating to seed before real reviews arrive (0-5).</summary>
    public decimal InitialRating { get; set; } = 0;

    /// <summary>Gets or sets the initial review count to seed before real reviews arrive.</summary>
    public int InitialReviewCount { get; set; } = 0;
}

/// <summary>Request payload for updating an existing product (admin). All fields optional.</summary>
public sealed class UpdateProductRequest
{
    /// <summary>Gets or sets the updated product name.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the updated URL slug.</summary>
    public string? Slug { get; set; }

    /// <summary>Gets or sets the updated brand ID.</summary>
    [JsonConverter(typeof(NullableGuidStringConverter))]
    public Guid? BrandId { get; set; }

    /// <summary>Gets or sets the updated category ID.</summary>
    public int? CategoryId { get; set; }

    /// <summary>Gets or sets the updated description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the updated short description.</summary>
    public string? ShortDescription { get; set; }

    /// <summary>Gets or sets the updated sell price.</summary>
    public decimal? Price { get; set; }

    /// <summary>Gets or sets the updated RRP.</summary>
    public decimal? CompareAtPrice { get; set; }

    /// <summary>Gets or sets the updated cost price.</summary>
    public decimal? CostPrice { get; set; }

    /// <summary>Gets or sets the updated stock quantity.</summary>
    public int? StockQty { get; set; }

    /// <summary>Gets or sets the updated weight.</summary>
    public decimal? WeightKg { get; set; }

    /// <summary>Gets or sets the updated attributes JSON.</summary>
    public string? AttributesJson { get; set; }

    /// <summary>Gets or sets the updated tags JSON.</summary>
    public string? TagsJson { get; set; }

    /// <summary>Gets or sets a single tag value from admin product forms.</summary>
    public string? Tag
    {
        get => null;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                TagsJson = JsonSerializer.Serialize(new[] { value.Trim() });
            }
        }
    }

    /// <summary>Gets or sets the updated active flag.</summary>
    public bool? IsActive { get; set; }

    /// <summary>Gets or sets the updated featured flag.</summary>
    public bool? IsFeatured { get; set; }

    /// <summary>Gets or sets replacement product image URLs from admin update flows.</summary>
    public List<string>? ImageUrls { get; set; }

    /// <summary>Gets or sets the updated SEO meta title.</summary>
    public string? MetaTitle { get; set; }

    /// <summary>Gets or sets the updated SEO meta description.</summary>
    public string? MetaDescription { get; set; }

    /// <summary>Gets or sets the initial average rating to seed before real reviews arrive (0-5).</summary>
    public decimal InitialRating { get; set; } = 0;

    /// <summary>Gets or sets the initial review count to seed before real reviews arrive.</summary>
    public int InitialReviewCount { get; set; } = 0;
}

/// <summary>Converts nullable GUID JSON values while treating blank strings as null.</summary>
public sealed class NullableGuidStringConverter : JsonConverter<Guid?>
{
    /// <inheritdoc />
    public override Guid? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            string? value = reader.GetString();
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (Guid.TryParse(value, out Guid parsed))
            {
                return parsed;
            }
        }

        throw new JsonException("The JSON value could not be converted to System.Nullable<System.Guid>.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Guid? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value);
            return;
        }

        writer.WriteNullValue();
    }
}
