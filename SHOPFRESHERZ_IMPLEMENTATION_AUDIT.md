# ShopFresherz Implementation Audit
Generated from source files under `ShopFresherz.API`, `ShopFresherz.Application`, `ShopFresherz.Domain`, and `ShopFresherz.Infrastructure`. `bin`, `obj`, logs, and published outputs were ignored. Secret-like appsettings values are masked.

## Report 1 - Cloudinary / Image Upload
NOT FOUND: CloudinaryService.cs - may not have been implemented yet

NOT FOUND: ICloudinaryService.cs - may not have been implemented yet

NOT FOUND: IImageService.cs - may not have been implemented yet

NOT FOUND: CloudinarySettings.cs - may not have been implemented yet

### ShopFresherz.API/Controllers/ProductsController.cs
Summary: ASP.NET Core controller exposing the related HTTP endpoints.
```csharp
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Product;
using ShopFresherz.Application.Features.Products.Commands;
using ShopFresherz.Application.Features.Products.Queries;

namespace ShopFresherz.API.Controllers;

/// <summary>Handles product catalog browsing and admin CRUD operations.</summary>
[ApiController]
[Route("api/v1/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>Initialises a new instance of <see cref="ProductsController"/>.</summary>
    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Returns a paginated product listing with optional filters.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int page       = 1,
        [FromQuery] int pageSize   = 20,
        [FromQuery] int? categoryId  = null,
        [FromQuery] Guid? brandId    = null,
        [FromQuery] decimal? priceMin  = null,
        [FromQuery] decimal? priceMax  = null,
        [FromQuery] decimal? ratingMin = null,
        [FromQuery] string? sortBy     = null,
        CancellationToken cancellationToken = default)
    {
        Result<PagedResult<ProductSummaryDto>> result =
            await _mediator.Send(
                new GetProductsQuery(page, pageSize, categoryId, brandId, priceMin, priceMax, ratingMin, sortBy),
                cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Returns full product detail for the product detail page (PDP).</summary>
    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlug(
        [FromRoute] string slug,
        CancellationToken cancellationToken)
    {
        Result<ProductDetailDto> result =
            await _mediator.Send(new GetProductBySlugQuery(slug), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Creates a new product. Requires Admin or SuperAdmin role.</summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        Result<Guid> result =
            await _mediator.Send(new CreateProductCommand(request), cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetBySlug), new { slug = string.Empty }, new { id = result.Value })
            : MapError(result.Error);
    }

    /// <summary>Updates an existing product. Requires Admin or SuperAdmin role.</summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        Result<bool> result =
            await _mediator.Send(new UpdateProductCommand(id, request), cancellationToken);

        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    /// <summary>Soft-deletes a product. Requires Admin or SuperAdmin role.</summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        Result<bool> result =
            await _mediator.Send(new DeleteProductCommand(id), cancellationToken);

        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    /// <summary>
    /// Uploads a product image, generates 4 WebP zoom derivatives
    /// (80px thumb, 540px display, 1600px zoom, original),
    /// and stores all URLs. Requires Admin role.
    /// Max file size: 8MB. Min dimensions: 800x800px.
    /// </summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpPost("{id:guid}/images")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ProductImageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadImage(
        [FromRoute] Guid id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { Code = "VALIDATION", Message = "No file uploaded." });
        }

        using Stream stream = file.OpenReadStream();

        Result<ProductImageDto> result = await _mediator.Send(
            new UploadProductImageCommand(id, stream, file.FileName, file.Length),
            cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetBySlug), new { slug = string.Empty }, result.Value)
            : MapError(result.Error);
    }

    private IActionResult MapError(Error error) => error.Code switch
    {
        "NOT_FOUND"  => NotFound(new { error.Code, error.Message }),
        "CONFLICT"   => Conflict(new { error.Code, error.Message }),
        "VALIDATION" => BadRequest(new { error.Code, error.Message }),
        _            => StatusCode(StatusCodes.Status500InternalServerError, new { error.Code, error.Message }),
    };
}
```

### ShopFresherz.Application/Features/Products/Commands/UploadProductImageCommand.cs
Summary: MediatR command, handler, and validation logic for the related workflow.
```csharp
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Product;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;
using ShopFresherz.Domain.Interfaces.Services;

namespace ShopFresherz.Application.Features.Products.Commands;

/// <summary>Command for uploading and processing a product image.</summary>
public sealed record UploadProductImageCommand(
    Guid ProductId,
    Stream ImageStream,
    string FileName,
    long FileSizeBytes) : IRequest<Result<ProductImageDto>>;

/// <summary>Handler for <see cref="UploadProductImageCommand"/>.</summary>
public sealed class UploadProductImageCommandHandler
    : IRequestHandler<UploadProductImageCommand, Result<ProductImageDto>>
{
    private const long MaxFileSizeBytes = 8 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
    };

    private readonly IUnitOfWork _uow;
    private readonly IImageProcessingService _imageProcessing;

    public UploadProductImageCommandHandler(IUnitOfWork uow, IImageProcessingService imageProcessing)
    {
        _uow = uow;
        _imageProcessing = imageProcessing;
    }

    /// <inheritdoc />
    public async Task<Result<ProductImageDto>> Handle(
        UploadProductImageCommand command,
        CancellationToken cancellationToken)
    {
        if (command.FileSizeBytes > MaxFileSizeBytes)
        {
            return Error.Validation("Image file size must be 8MB or less.");
        }

        string extension = Path.GetExtension(command.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            return Error.Validation("Image must be a JPG, PNG, or WebP file.");
        }

        Product? product = await _uow.Products.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            return Error.NotFound("Product");
        }

        ProductImageUrls urls;
        try
        {
            urls = await _imageProcessing.ProcessAndUploadAsync(
                command.ImageStream,
                command.FileName,
                command.ProductId,
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return Error.Validation(ex.Message);
        }

        ProductImage image = new()
        {
            ProductId = product.Id,
            ThumbUrl = urls.ThumbUrl,
            DisplayUrl = urls.DisplayUrl,
            ZoomUrl = urls.ZoomUrl,
            OriginalUrl = urls.OriginalUrl,
            SortOrder = product.Images.Count,
            IsVideo = false,
        };

        product.Images.Add(image);
        _uow.Products.Update(product);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<ProductImageDto>.Success(new ProductImageDto
        {
            Id = image.Id,
            ThumbUrl = image.ThumbUrl,
            DisplayUrl = image.DisplayUrl,
            ZoomUrl = image.ZoomUrl,
            OriginalUrl = image.OriginalUrl,
            SortOrder = image.SortOrder,
            IsVideo = image.IsVideo,
        });
    }
}
```

### ShopFresherz.Domain/Entities/Product.cs
Summary: Domain entity definition.
```csharp
using ShopFresherz.Domain.Common;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShopFresherz.Domain.Entities;
/// <summary>
/// Core product entity â€” the central aggregate in the ShopFresherz catalog.
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

    /// <summary>Gets the quantity available for new cart additions (StockQty âˆ’ ReservedQty).</summary>
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
```

### ShopFresherz.Domain/Interfaces/Services/IImageProcessingService.cs
Summary: Service implementation or abstraction for the related infrastructure capability.
```csharp
namespace ShopFresherz.Domain.Interfaces.Services;

/// <summary>
/// Contract for server-side image processing using ImageSharp.
/// On upload: validates min 2000Ã—2000px and max 8MB, then generates
/// four WebP derivatives: 80px, 540px, 1600px, and original.
/// </summary>
public interface IImageProcessingService
{
    /// <summary>
    /// Validates the uploaded image meets minimum requirements.
    /// </summary>
    /// <param name="stream">The raw upload stream.</param>
    /// <returns>True if valid; false if below 2000Ã—2000px or above 8MB.</returns>
    Task<bool> ValidateAsync(Stream stream);

    /// <summary>
    /// Generates all four WebP derivatives from the source image stream.
    /// Returns a record containing streams for each derivative size.
    /// </summary>
    Task<ImageDerivatives> GenerateDerivativesAsync(Stream sourceStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates, generates the zoom pipeline derivatives, uploads them, and returns public URLs.
    /// </summary>
    Task<ProductImageUrls> ProcessAndUploadAsync(
        Stream imageStream,
        string originalFileName,
        Guid productId,
        CancellationToken cancellationToken = default);
}

/// <summary>Contains the four processed image derivative streams.</summary>
public sealed record ImageDerivatives(
    Stream Thumb80,
    Stream Display540,
    Stream Zoom1600,
    Stream Original);

/// <summary>Contains uploaded URLs for the four product image derivatives.</summary>
public sealed record ProductImageUrls(
    string ThumbUrl,
    string DisplayUrl,
    string ZoomUrl,
    string OriginalUrl);
```

### ShopFresherz.Infrastructure/Extensions/ServiceCollectionExtensions.cs
Summary: Service implementation or abstraction for the related infrastructure capability.
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using ShopFresherz.Domain.Interfaces;
using ShopFresherz.Domain.Interfaces.Repositories;
using ShopFresherz.Domain.Interfaces.Services;
using ShopFresherz.Infrastructure.Configuration;
using ShopFresherz.Infrastructure.Persistence;
using ShopFresherz.Infrastructure.Persistence.Repositories;
using ShopFresherz.Infrastructure.Search;
using ShopFresherz.Infrastructure.Services;

namespace ShopFresherz.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering Infrastructure layer services with the DI container.
/// Call <see cref="AddInfrastructure"/> from the API layer's Program.cs.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the ShopFresherzDbContext, repositories, Unit of Work, and all
    /// Infrastructure services (token, cache, email, SMS, file storage, image processing, payments).
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // â”€â”€ EF Core â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        services.AddDbContext<ShopFresherzDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(ShopFresherzDbContext).Assembly.FullName)),
            optionsLifetime: ServiceLifetime.Singleton);
        services.AddDbContextFactory<ShopFresherzDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(ShopFresherzDbContext).Assembly.FullName)));

        // â”€â”€ Unit of Work â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // â”€â”€ Redis / Cache â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        string? redisConnection = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redisConnection))
        {
            redisConnection = configuration["Redis:ConnectionString"];
        }

        if (string.IsNullOrWhiteSpace(redisConnection))
        {
            // No Redis configured â€” fall back to in-memory cache (already registered in Program.cs).
            services.AddSingleton<ICacheService, MemoryCacheService>();
        }
        else
        {
            try
            {
                services.AddSingleton<IConnectionMultiplexer>(
                    _ => ConnectionMultiplexer.Connect(redisConnection));
                services.AddSingleton<ICacheService, RedisCacheService>();
            }
            catch
            {
                // If Redis connection fails, fall back to in-memory cache.
                services.AddSingleton<ICacheService, MemoryCacheService>();
            }
        }

        // â”€â”€ HTTP clients for third-party services â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        services.AddHttpClient(nameof(PaystackPaymentService));
        services.Configure<FlutterwaveOptions>(configuration.GetSection("Flutterwave"));
        services.AddHttpClient<IFlutterwavePaymentService, FlutterwavePaymentService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        // â”€â”€ Token / Auth â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();

        // â”€â”€ Email â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        services.AddTransient<IEmailService, SendGridEmailService>();
        services.AddScoped<IAuditLogService, EfAuditLogService>();

        // â”€â”€ SMS â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        services.Configure<TermiiOptions>(configuration.GetSection("Termii"));
        services.AddHttpClient<ISmsService, TermiiSmsService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        // â”€â”€ Image processing â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        services.AddScoped<IImageProcessingService, ImageProcessingService>();

        // â”€â”€ Search â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        services.Configure<ElasticsearchOptions>(configuration.GetSection("Elasticsearch"));
        services.AddScoped<ISearchService, ElasticsearchService>();

        // â”€â”€ Notifications â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        services.AddScoped<INotificationRepository, EfNotificationRepository>();

        // â”€â”€ Payment gateway â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        services.AddTransient<IPaymentService, PaystackPaymentService>();

        // â”€â”€ Chatbot â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        services.AddScoped<IChatbotService, ChatbotService>();

        return services;
    }
}
```

### ShopFresherz.Infrastructure/Persistence/Migrations/20260517142653_AddProductImageUrlsJson.cs
Summary: Source/configuration file used by the audited backend.
```csharp
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopFresherz.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductImageUrlsJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "NotificationWishlistReminders",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "NotificationReviewReminders",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "NotificationPromotions",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "NotificationOrderUpdates",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "NotificationBackInStock",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrlsJson",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LinkUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropColumn(
                name: "ImageUrlsJson",
                table: "Products");

            migrationBuilder.AlterColumn<bool>(
                name: "NotificationWishlistReminders",
                table: "Users",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "NotificationReviewReminders",
                table: "Users",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "NotificationPromotions",
                table: "Users",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "NotificationOrderUpdates",
                table: "Users",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "NotificationBackInStock",
                table: "Users",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);
        }
    }
}
```

### ShopFresherz.Infrastructure/Persistence/Migrations/20260517142653_AddProductImageUrlsJson.Designer.cs
Summary: Source/configuration file used by the audited backend.
```csharp
// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ShopFresherz.Infrastructure.Persistence;

#nullable disable

namespace ShopFresherz.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ShopFresherzDbContext))]
    [Migration("20260517142653_AddProductImageUrlsJson")]
    partial class AddProductImageUrlsJson
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.14")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Address", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("City")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsDefault")
                        .HasColumnType("bit");

                    b.Property<string>("Label")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("Line1")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("Line2")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("PostalCode")
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<string>("State")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Addresses");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.AuditLog", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<string>("Action")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<Guid?>("ActorUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("EntityId")
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<string>("EntityType")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<string>("IpAddress")
                        .HasMaxLength(80)
                        .HasColumnType("nvarchar(80)");

                    b.Property<string>("NewValues")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("OldValues")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("CreatedAt");

                    b.HasIndex("EntityType", "EntityId");

                    b.ToTable("AuditLogs");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Brand", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("LogoUrl")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("Slug")
                        .IsUnique();

                    b.ToTable("Brands");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Cart", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("CouponCode")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("ExpiresAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("SessionId")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId")
                        .IsUnique()
                        .HasFilter("[UserId] IS NOT NULL");

                    b.ToTable("Carts");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.CartItem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("CartId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Quantity")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("VariantId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("VariantId");

                    b.HasIndex("CartId", "ProductId", "VariantId")
                        .IsUnique()
                        .HasFilter("[VariantId] IS NOT NULL");

                    b.ToTable("CartItems");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Category", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ImageUrl")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("MetaDescription")
                        .HasMaxLength(160)
                        .HasColumnType("nvarchar(160)");

                    b.Property<string>("MetaTitle")
                        .HasMaxLength(70)
                        .HasColumnType("nvarchar(70)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<int?>("ParentId")
                        .HasColumnType("int");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("ParentId");

                    b.HasIndex("Slug")
                        .IsUnique();

                    b.ToTable("Categories");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Coupon", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Code")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("ExpiresAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<bool>("IsStackable")
                        .HasColumnType("bit");

                    b.Property<int?>("MaxUses")
                        .HasColumnType("int");

                    b.Property<int?>("MaxUsesPerUser")
                        .HasColumnType("int");

                    b.Property<decimal?>("MinimumOrderAmount")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int?>("RestrictToCategoryId")
                        .HasColumnType("int");

                    b.Property<Guid?>("RestrictToProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<int>("UsedCount")
                        .HasColumnType("int");

                    b.Property<decimal>("Value")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("Id");

                    b.HasIndex("Code")
                        .IsUnique();

                    b.HasIndex("RestrictToCategoryId");

                    b.HasIndex("RestrictToProductId");

                    b.ToTable("Coupons");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.FlashDeal", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("EndsAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<int?>("MaxQuantity")
                        .HasColumnType("int");

                    b.Property<decimal>("OriginalPrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal>("SalePrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("SoldQuantity")
                        .HasColumnType("int");

                    b.Property<DateTime>("StartsAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("VariantId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("VariantId");

                    b.ToTable("FlashDeals");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.HomepageBanner", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("CtaText")
                        .HasMaxLength(80)
                        .HasColumnType("nvarchar(80)");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("EndsAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ImageUrl")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("LinkUrl")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<DateTime?>("StartsAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("SubTitle")
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("IsActive", "SortOrder");

                    b.ToTable("HomepageBanners");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.LoyaltyTransaction", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("EventType")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<DateTime>("ExpiresAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("OrderId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Points")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("OrderId");

                    b.HasIndex("UserId");

                    b.ToTable("LoyaltyTransactions");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Notification", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsRead")
                        .HasColumnType("bit");

                    b.Property<string>("LinkUrl")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Notifications");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.NotifyRequest", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsNotified")
                        .HasColumnType("bit");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("UserId", "ProductId")
                        .IsUnique();

                    b.ToTable("NotifyRequests");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Order", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<int?>("CouponId")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("DeliveryAddressJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("DeliveryFee")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(18,2)")
                        .HasDefaultValue(0m);

                    b.Property<string>("DeliveryMethod")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<decimal>("DiscountAmount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(18,2)")
                        .HasDefaultValue(0m);

                    b.Property<DateTime?>("EstimatedDelivery")
                        .HasColumnType("datetime2");

                    b.Property<string>("GuestEmail")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("Notes")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<string>("OrderNumber")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<string>("PaymentMethod")
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<string>("PaymentReference")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("PaymentStatus")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<decimal>("Subtotal")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("TrackingNumber")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal>("VatAmount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(18,2)")
                        .HasDefaultValue(0m);

                    b.HasKey("Id");

                    b.HasIndex("CouponId");

                    b.HasIndex("OrderNumber")
                        .IsUnique();

                    b.HasIndex("UserId");

                    b.ToTable("Orders");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.OrderItem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("LineTotal")
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid>("OrderId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("ProductSnapshotJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Quantity")
                        .HasColumnType("int");

                    b.Property<decimal>("UnitPrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("VariantId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("OrderId");

                    b.HasIndex("ProductId");

                    b.HasIndex("VariantId");

                    b.ToTable("OrderItems");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Product", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AttributesJson")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("AverageRating")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(3,2)")
                        .HasDefaultValue(0m);

                    b.Property<Guid>("BrandId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("CategoryId")
                        .HasColumnType("int");

                    b.Property<decimal?>("CompareAtPrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal?>("CostPrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ImageUrlsJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<bool>("IsFeatured")
                        .HasColumnType("bit");

                    b.Property<string>("MetaDescription")
                        .HasMaxLength(160)
                        .HasColumnType("nvarchar(160)");

                    b.Property<string>("MetaTitle")
                        .HasMaxLength(70)
                        .HasColumnType("nvarchar(70)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.Property<decimal>("Price")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("ReservedQty")
                        .HasColumnType("int");

                    b.Property<int>("ReviewCount")
                        .HasColumnType("int");

                    b.Property<string>("SKU")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("ShortDescription")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(350)
                        .HasColumnType("nvarchar(350)");

                    b.Property<int>("SoldCount")
                        .HasColumnType("int");

                    b.Property<int>("StockQty")
                        .HasColumnType("int");

                    b.Property<string>("TagsJson")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("ViewCount")
                        .HasColumnType("int");

                    b.Property<decimal?>("WeightKg")
                        .HasColumnType("decimal(8,3)");

                    b.HasKey("Id");

                    b.HasIndex("BrandId");

                    b.HasIndex("CategoryId");

                    b.HasIndex("SKU")
                        .IsUnique();

                    b.HasIndex("Slug")
                        .IsUnique();

                    b.ToTable("Products");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.ProductImage", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("DisplayUrl")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<bool>("IsVideo")
                        .HasColumnType("bit");

                    b.Property<string>("OriginalUrl")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<string>("ThumbUrl")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ZoomUrl")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.ToTable("ProductImages");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.ProductVariant", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AttributesJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("Price")
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("ReservedQty")
                        .HasColumnType("int");

                    b.Property<string>("SKU")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<int>("StockQty")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("SKU")
                        .IsUnique();

                    b.ToTable("ProductVariants");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Review", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Body")
                        .HasMaxLength(2000)
                        .HasColumnType("nvarchar(2000)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsApproved")
                        .HasColumnType("bit");

                    b.Property<bool>("IsVerifiedPurchase")
                        .HasColumnType("bit");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Rating")
                        .HasColumnType("int");

                    b.Property<string>("Title")
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("UserId", "ProductId")
                        .IsUnique();

                    b.ToTable("Reviews");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AvatarUrl")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("GoogleId")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<bool>("IsVerified")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("LastLoginAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<int>("LoyaltyPoints")
                        .HasColumnType("int");

                    b.Property<bool>("NotificationBackInStock")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<bool>("NotificationOrderUpdates")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<bool>("NotificationPromotions")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<bool>("NotificationReviewReminders")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<bool>("NotificationWishlistReminders")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasMaxLength(512)
                        .HasColumnType("nvarchar(512)");

                    b.Property<string>("Phone")
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<DateTime?>("RefreshTokenExpires")
                        .HasColumnType("datetime2");

                    b.Property<string>("RefreshTokenHash")
                        .HasMaxLength(512)
                        .HasColumnType("nvarchar(512)");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.HasIndex("GoogleId")
                        .IsUnique()
                        .HasFilter("[GoogleId] IS NOT NULL");

                    b.HasIndex("Phone")
                        .IsUnique()
                        .HasFilter("[Phone] IS NOT NULL");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Wishlist", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("UserId", "ProductId")
                        .IsUnique();

                    b.ToTable("Wishlists");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Address", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("Addresses")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Cart", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.CartItem", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Cart", "Cart")
                        .WithMany("Items")
                        .HasForeignKey("CartId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.ProductVariant", "Variant")
                        .WithMany()
                        .HasForeignKey("VariantId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Cart");

                    b.Navigation("Product");

                    b.Navigation("Variant");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Category", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Category", "Parent")
                        .WithMany("Children")
                        .HasForeignKey("ParentId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Parent");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Coupon", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Category", null)
                        .WithMany()
                        .HasForeignKey("RestrictToCategoryId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("ShopFresherz.Domain.Entities.Product", null)
                        .WithMany()
                        .HasForeignKey("RestrictToProductId")
                        .OnDelete(DeleteBehavior.Restrict);
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.FlashDeal", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.ProductVariant", null)
                        .WithMany()
                        .HasForeignKey("VariantId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Product");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.LoyaltyTransaction", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Order", null)
                        .WithMany()
                        .HasForeignKey("OrderId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("LoyaltyTransactions")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Notification", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.NotifyRequest", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Product");

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Order", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Coupon", "Coupon")
                        .WithMany()
                        .HasForeignKey("CouponId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("Orders")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Coupon");

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.OrderItem", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Order", "Order")
                        .WithMany("Items")
                        .HasForeignKey("OrderId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany("OrderItems")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.ProductVariant", "Variant")
                        .WithMany()
                        .HasForeignKey("VariantId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Order");

                    b.Navigation("Product");

                    b.Navigation("Variant");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Product", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Brand", "Brand")
                        .WithMany("Products")
                        .HasForeignKey("BrandId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.Category", "Category")
                        .WithMany("Products")
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Brand");

                    b.Navigation("Category");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.ProductImage", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany("Images")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Product");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.ProductVariant", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany("Variants")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Product");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Review", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany("Reviews")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("Reviews")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Product");

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Wishlist", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("Wishlist")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Product");

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Brand", b =>
                {
                    b.Navigation("Products");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Cart", b =>
                {
                    b.Navigation("Items");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Category", b =>
                {
                    b.Navigation("Children");

                    b.Navigation("Products");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Order", b =>
                {
                    b.Navigation("Items");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Product", b =>
                {
                    b.Navigation("Images");

                    b.Navigation("OrderItems");

                    b.Navigation("Reviews");

                    b.Navigation("Variants");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.User", b =>
                {
                    b.Navigation("Addresses");

                    b.Navigation("LoyaltyTransactions");

                    b.Navigation("Orders");

                    b.Navigation("Reviews");

                    b.Navigation("Wishlist");
                });
#pragma warning restore 612, 618
        }
    }
}
```

### ShopFresherz.Infrastructure/Persistence/Migrations/20260518150644_AddAdminAppSettings.Designer.cs
Summary: Source/configuration file used by the audited backend.
```csharp
// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ShopFresherz.Infrastructure.Persistence;

#nullable disable

namespace ShopFresherz.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ShopFresherzDbContext))]
    [Migration("20260518150644_AddAdminAppSettings")]
    partial class AddAdminAppSettings
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.14")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Address", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("City")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsDefault")
                        .HasColumnType("bit");

                    b.Property<string>("Label")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("Line1")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("Line2")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("PostalCode")
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<string>("State")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Addresses");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.AppSetting", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ValueJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("Key")
                        .IsUnique();

                    b.ToTable("AppSettings", (string)null);
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.AuditLog", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<string>("Action")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<Guid?>("ActorUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("EntityId")
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<string>("EntityType")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<string>("IpAddress")
                        .HasMaxLength(80)
                        .HasColumnType("nvarchar(80)");

                    b.Property<string>("NewValues")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("OldValues")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("CreatedAt");

                    b.HasIndex("EntityType", "EntityId");

                    b.ToTable("AuditLogs");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Brand", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("LogoUrl")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("Slug")
                        .IsUnique();

                    b.ToTable("Brands");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Cart", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("CouponCode")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("ExpiresAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("SessionId")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId")
                        .IsUnique()
                        .HasFilter("[UserId] IS NOT NULL");

                    b.ToTable("Carts");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.CartItem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("CartId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Quantity")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("VariantId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("VariantId");

                    b.HasIndex("CartId", "ProductId", "VariantId")
                        .IsUnique()
                        .HasFilter("[VariantId] IS NOT NULL");

                    b.ToTable("CartItems");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Category", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ImageUrl")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("MetaDescription")
                        .HasMaxLength(160)
                        .HasColumnType("nvarchar(160)");

                    b.Property<string>("MetaTitle")
                        .HasMaxLength(70)
                        .HasColumnType("nvarchar(70)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<int?>("ParentId")
                        .HasColumnType("int");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("ParentId");

                    b.HasIndex("Slug")
                        .IsUnique();

                    b.ToTable("Categories");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Coupon", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Code")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("ExpiresAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<bool>("IsStackable")
                        .HasColumnType("bit");

                    b.Property<int?>("MaxUses")
                        .HasColumnType("int");

                    b.Property<int?>("MaxUsesPerUser")
                        .HasColumnType("int");

                    b.Property<decimal?>("MinimumOrderAmount")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int?>("RestrictToCategoryId")
                        .HasColumnType("int");

                    b.Property<Guid?>("RestrictToProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<int>("UsedCount")
                        .HasColumnType("int");

                    b.Property<decimal>("Value")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("Id");

                    b.HasIndex("Code")
                        .IsUnique();

                    b.HasIndex("RestrictToCategoryId");

                    b.HasIndex("RestrictToProductId");

                    b.ToTable("Coupons");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.FlashDeal", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("EndsAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<int?>("MaxQuantity")
                        .HasColumnType("int");

                    b.Property<decimal>("OriginalPrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal>("SalePrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("SoldQuantity")
                        .HasColumnType("int");

                    b.Property<DateTime>("StartsAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("VariantId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("VariantId");

                    b.ToTable("FlashDeals");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.HomepageBanner", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("CtaText")
                        .HasMaxLength(80)
                        .HasColumnType("nvarchar(80)");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("EndsAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ImageUrl")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("LinkUrl")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<DateTime?>("StartsAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("SubTitle")
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("IsActive", "SortOrder");

                    b.ToTable("HomepageBanners");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.LoyaltyTransaction", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("EventType")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<DateTime>("ExpiresAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("OrderId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Points")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("OrderId");

                    b.HasIndex("UserId");

                    b.ToTable("LoyaltyTransactions");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Notification", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsRead")
                        .HasColumnType("bit");

                    b.Property<string>("LinkUrl")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Notifications");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.NotifyRequest", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsNotified")
                        .HasColumnType("bit");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("UserId", "ProductId")
                        .IsUnique();

                    b.ToTable("NotifyRequests");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Order", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<int?>("CouponId")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("DeliveryAddressJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("DeliveryFee")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(18,2)")
                        .HasDefaultValue(0m);

                    b.Property<string>("DeliveryMethod")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<decimal>("DiscountAmount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(18,2)")
                        .HasDefaultValue(0m);

                    b.Property<DateTime?>("EstimatedDelivery")
                        .HasColumnType("datetime2");

                    b.Property<string>("GuestEmail")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("Notes")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<string>("OrderNumber")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<string>("PaymentMethod")
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<string>("PaymentReference")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("PaymentStatus")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<decimal>("Subtotal")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("TrackingNumber")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal>("VatAmount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(18,2)")
                        .HasDefaultValue(0m);

                    b.HasKey("Id");

                    b.HasIndex("CouponId");

                    b.HasIndex("OrderNumber")
                        .IsUnique();

                    b.HasIndex("UserId");

                    b.ToTable("Orders");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.OrderItem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("LineTotal")
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid>("OrderId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("ProductSnapshotJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Quantity")
                        .HasColumnType("int");

                    b.Property<decimal>("UnitPrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("VariantId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("OrderId");

                    b.HasIndex("ProductId");

                    b.HasIndex("VariantId");

                    b.ToTable("OrderItems");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Product", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AttributesJson")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("AverageRating")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(3,2)")
                        .HasDefaultValue(0m);

                    b.Property<Guid>("BrandId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("CategoryId")
                        .HasColumnType("int");

                    b.Property<decimal?>("CompareAtPrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal?>("CostPrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ImageUrlsJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<bool>("IsFeatured")
                        .HasColumnType("bit");

                    b.Property<string>("MetaDescription")
                        .HasMaxLength(160)
                        .HasColumnType("nvarchar(160)");

                    b.Property<string>("MetaTitle")
                        .HasMaxLength(70)
                        .HasColumnType("nvarchar(70)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.Property<decimal>("Price")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("ReservedQty")
                        .HasColumnType("int");

                    b.Property<int>("ReviewCount")
                        .HasColumnType("int");

                    b.Property<string>("SKU")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("ShortDescription")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(350)
                        .HasColumnType("nvarchar(350)");

                    b.Property<int>("SoldCount")
                        .HasColumnType("int");

                    b.Property<int>("StockQty")
                        .HasColumnType("int");

                    b.Property<string>("TagsJson")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("ViewCount")
                        .HasColumnType("int");

                    b.Property<decimal?>("WeightKg")
                        .HasColumnType("decimal(8,3)");

                    b.HasKey("Id");

                    b.HasIndex("BrandId");

                    b.HasIndex("CategoryId");

                    b.HasIndex("SKU")
                        .IsUnique();

                    b.HasIndex("Slug")
                        .IsUnique();

                    b.ToTable("Products");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.ProductImage", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("DisplayUrl")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<bool>("IsVideo")
                        .HasColumnType("bit");

                    b.Property<string>("OriginalUrl")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<string>("ThumbUrl")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ZoomUrl")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.ToTable("ProductImages");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.ProductVariant", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AttributesJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("Price")
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("ReservedQty")
                        .HasColumnType("int");

                    b.Property<string>("SKU")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<int>("StockQty")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("SKU")
                        .IsUnique();

                    b.ToTable("ProductVariants");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Review", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Body")
                        .HasMaxLength(2000)
                        .HasColumnType("nvarchar(2000)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsApproved")
                        .HasColumnType("bit");

                    b.Property<bool>("IsVerifiedPurchase")
                        .HasColumnType("bit");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Rating")
                        .HasColumnType("int");

                    b.Property<string>("Title")
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("UserId", "ProductId")
                        .IsUnique();

                    b.ToTable("Reviews");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AvatarUrl")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("GoogleId")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<bool>("IsVerified")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("LastLoginAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<int>("LoyaltyPoints")
                        .HasColumnType("int");

                    b.Property<bool>("NotificationBackInStock")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<bool>("NotificationOrderUpdates")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<bool>("NotificationPromotions")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<bool>("NotificationReviewReminders")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<bool>("NotificationWishlistReminders")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasMaxLength(512)
                        .HasColumnType("nvarchar(512)");

                    b.Property<string>("Phone")
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<DateTime?>("RefreshTokenExpires")
                        .HasColumnType("datetime2");

                    b.Property<string>("RefreshTokenHash")
                        .HasMaxLength(512)
                        .HasColumnType("nvarchar(512)");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.HasIndex("GoogleId")
                        .IsUnique()
                        .HasFilter("[GoogleId] IS NOT NULL");

                    b.HasIndex("Phone")
                        .IsUnique()
                        .HasFilter("[Phone] IS NOT NULL");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Wishlist", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("UserId", "ProductId")
                        .IsUnique();

                    b.ToTable("Wishlists");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Address", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("Addresses")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Cart", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.CartItem", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Cart", "Cart")
                        .WithMany("Items")
                        .HasForeignKey("CartId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.ProductVariant", "Variant")
                        .WithMany()
                        .HasForeignKey("VariantId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Cart");

                    b.Navigation("Product");

                    b.Navigation("Variant");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Category", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Category", "Parent")
                        .WithMany("Children")
                        .HasForeignKey("ParentId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Parent");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Coupon", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Category", null)
                        .WithMany()
                        .HasForeignKey("RestrictToCategoryId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("ShopFresherz.Domain.Entities.Product", null)
                        .WithMany()
                        .HasForeignKey("RestrictToProductId")
                        .OnDelete(DeleteBehavior.Restrict);
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.FlashDeal", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.ProductVariant", null)
                        .WithMany()
                        .HasForeignKey("VariantId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Product");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.LoyaltyTransaction", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Order", null)
                        .WithMany()
                        .HasForeignKey("OrderId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("LoyaltyTransactions")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Notification", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.NotifyRequest", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Product");

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Order", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Coupon", "Coupon")
                        .WithMany()
                        .HasForeignKey("CouponId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("Orders")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Coupon");

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.OrderItem", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Order", "Order")
                        .WithMany("Items")
                        .HasForeignKey("OrderId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany("OrderItems")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.ProductVariant", "Variant")
                        .WithMany()
                        .HasForeignKey("VariantId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Order");

                    b.Navigation("Product");

                    b.Navigation("Variant");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Product", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Brand", "Brand")
                        .WithMany("Products")
                        .HasForeignKey("BrandId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.Category", "Category")
                        .WithMany("Products")
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Brand");

                    b.Navigation("Category");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.ProductImage", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany("Images")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Product");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.ProductVariant", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany("Variants")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Product");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Review", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany("Reviews")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("Reviews")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Product");

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Wishlist", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("Wishlist")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Product");

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Brand", b =>
                {
                    b.Navigation("Products");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Cart", b =>
                {
                    b.Navigation("Items");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Category", b =>
                {
                    b.Navigation("Children");

                    b.Navigation("Products");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Order", b =>
                {
                    b.Navigation("Items");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Product", b =>
                {
                    b.Navigation("Images");

                    b.Navigation("OrderItems");

                    b.Navigation("Reviews");

                    b.Navigation("Variants");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.User", b =>
                {
                    b.Navigation("Addresses");

                    b.Navigation("LoyaltyTransactions");

                    b.Navigation("Orders");

                    b.Navigation("Reviews");

                    b.Navigation("Wishlist");
                });
#pragma warning restore 612, 618
        }
    }
}
```

### ShopFresherz.Infrastructure/Persistence/Migrations/20260525111342_AddSavedCardsAndFeaturedSections.Designer.cs
Summary: Source/configuration file used by the audited backend.
```csharp
// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ShopFresherz.Infrastructure.Persistence;

#nullable disable

namespace ShopFresherz.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ShopFresherzDbContext))]
    [Migration("20260525111342_AddSavedCardsAndFeaturedSections")]
    partial class AddSavedCardsAndFeaturedSections
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.14")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Address", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("City")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsDefault")
                        .HasColumnType("bit");

                    b.Property<string>("Label")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("Line1")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("Line2")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("PostalCode")
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<string>("State")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Addresses");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.AppSetting", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ValueJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("Key")
                        .IsUnique();

                    b.ToTable("AppSettings", (string)null);
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.AuditLog", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<string>("Action")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<Guid?>("ActorUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("EntityId")
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<string>("EntityType")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<string>("IpAddress")
                        .HasMaxLength(80)
                        .HasColumnType("nvarchar(80)");

                    b.Property<string>("NewValues")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("OldValues")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("CreatedAt");

                    b.HasIndex("EntityType", "EntityId");

                    b.ToTable("AuditLogs");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Brand", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("LogoUrl")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("Slug")
                        .IsUnique();

                    b.ToTable("Brands");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Cart", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("CouponCode")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("ExpiresAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("SessionId")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId")
                        .IsUnique()
                        .HasFilter("[UserId] IS NOT NULL");

                    b.ToTable("Carts");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.CartItem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("CartId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Quantity")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("VariantId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("VariantId");

                    b.HasIndex("CartId", "ProductId", "VariantId")
                        .IsUnique()
                        .HasFilter("[VariantId] IS NOT NULL");

                    b.ToTable("CartItems");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Category", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ImageUrl")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("MetaDescription")
                        .HasMaxLength(160)
                        .HasColumnType("nvarchar(160)");

                    b.Property<string>("MetaTitle")
                        .HasMaxLength(70)
                        .HasColumnType("nvarchar(70)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<int?>("ParentId")
                        .HasColumnType("int");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("ParentId");

                    b.HasIndex("Slug")
                        .IsUnique();

                    b.ToTable("Categories");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Coupon", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Code")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("ExpiresAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<bool>("IsStackable")
                        .HasColumnType("bit");

                    b.Property<int?>("MaxUses")
                        .HasColumnType("int");

                    b.Property<int?>("MaxUsesPerUser")
                        .HasColumnType("int");

                    b.Property<decimal?>("MinimumOrderAmount")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int?>("RestrictToCategoryId")
                        .HasColumnType("int");

                    b.Property<Guid?>("RestrictToProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<int>("UsedCount")
                        .HasColumnType("int");

                    b.Property<decimal>("Value")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("Id");

                    b.HasIndex("Code")
                        .IsUnique();

                    b.HasIndex("RestrictToCategoryId");

                    b.HasIndex("RestrictToProductId");

                    b.ToTable("Coupons");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.FeaturedSection", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("Section")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(350)
                        .HasColumnType("nvarchar(350)");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("Section");

                    b.ToTable("FeaturedSections");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.FlashDeal", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("EndsAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<int?>("MaxQuantity")
                        .HasColumnType("int");

                    b.Property<decimal>("OriginalPrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal>("SalePrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("SoldQuantity")
                        .HasColumnType("int");

                    b.Property<DateTime>("StartsAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("VariantId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("VariantId");

                    b.ToTable("FlashDeals");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.HomepageBanner", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("CtaText")
                        .HasMaxLength(80)
                        .HasColumnType("nvarchar(80)");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("EndsAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ImageUrl")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("LinkUrl")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<DateTime?>("StartsAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("SubTitle")
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("IsActive", "SortOrder");

                    b.ToTable("HomepageBanners");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.LoyaltyTransaction", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("EventType")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<DateTime>("ExpiresAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("OrderId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Points")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("OrderId");

                    b.HasIndex("UserId");

                    b.ToTable("LoyaltyTransactions");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Notification", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsRead")
                        .HasColumnType("bit");

                    b.Property<string>("LinkUrl")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Notifications");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.NotifyRequest", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsNotified")
                        .HasColumnType("bit");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("UserId", "ProductId")
                        .IsUnique();

                    b.ToTable("NotifyRequests");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Order", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<int?>("CouponId")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("DeliveryAddressJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("DeliveryFee")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(18,2)")
                        .HasDefaultValue(0m);

                    b.Property<string>("DeliveryMethod")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<decimal>("DiscountAmount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(18,2)")
                        .HasDefaultValue(0m);

                    b.Property<DateTime?>("EstimatedDelivery")
                        .HasColumnType("datetime2");

                    b.Property<string>("GuestEmail")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("Notes")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<string>("OrderNumber")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<string>("PaymentMethod")
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<string>("PaymentReference")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("PaymentStatus")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<decimal>("Subtotal")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("TrackingNumber")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal>("VatAmount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(18,2)")
                        .HasDefaultValue(0m);

                    b.HasKey("Id");

                    b.HasIndex("CouponId");

                    b.HasIndex("OrderNumber")
                        .IsUnique();

                    b.HasIndex("UserId");

                    b.ToTable("Orders");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.OrderItem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("LineTotal")
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid>("OrderId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("ProductSnapshotJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Quantity")
                        .HasColumnType("int");

                    b.Property<decimal>("UnitPrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("VariantId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("OrderId");

                    b.HasIndex("ProductId");

                    b.HasIndex("VariantId");

                    b.ToTable("OrderItems");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Product", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AttributesJson")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("AverageRating")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(3,2)")
                        .HasDefaultValue(0m);

                    b.Property<Guid>("BrandId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("CategoryId")
                        .HasColumnType("int");

                    b.Property<decimal?>("CompareAtPrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal?>("CostPrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ImageUrlsJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<bool>("IsFeatured")
                        .HasColumnType("bit");

                    b.Property<string>("MetaDescription")
                        .HasMaxLength(160)
                        .HasColumnType("nvarchar(160)");

                    b.Property<string>("MetaTitle")
                        .HasMaxLength(70)
                        .HasColumnType("nvarchar(70)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.Property<decimal>("Price")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("ReservedQty")
                        .HasColumnType("int");

                    b.Property<int>("ReviewCount")
                        .HasColumnType("int");

                    b.Property<string>("SKU")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("ShortDescription")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(350)
                        .HasColumnType("nvarchar(350)");

                    b.Property<int>("SoldCount")
                        .HasColumnType("int");

                    b.Property<int>("StockQty")
                        .HasColumnType("int");

                    b.Property<string>("TagsJson")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("ViewCount")
                        .HasColumnType("int");

                    b.Property<decimal?>("WeightKg")
                        .HasColumnType("decimal(8,3)");

                    b.HasKey("Id");

                    b.HasIndex("BrandId");

                    b.HasIndex("CategoryId");

                    b.HasIndex("SKU")
                        .IsUnique();

                    b.HasIndex("Slug")
                        .IsUnique();

                    b.ToTable("Products");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.ProductImage", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("DisplayUrl")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<bool>("IsVideo")
                        .HasColumnType("bit");

                    b.Property<string>("OriginalUrl")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<string>("ThumbUrl")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ZoomUrl")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.ToTable("ProductImages");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.ProductVariant", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AttributesJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("Price")
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("ReservedQty")
                        .HasColumnType("int");

                    b.Property<string>("SKU")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<int>("StockQty")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("SKU")
                        .IsUnique();

                    b.ToTable("ProductVariants");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Review", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Body")
                        .HasMaxLength(2000)
                        .HasColumnType("nvarchar(2000)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsApproved")
                        .HasColumnType("bit");

                    b.Property<bool>("IsVerifiedPurchase")
                        .HasColumnType("bit");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Rating")
                        .HasColumnType("int");

                    b.Property<string>("Title")
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("UserId", "ProductId")
                        .IsUnique();

                    b.ToTable("Reviews");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.SavedCard", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AuthorizationCode")
                        .HasMaxLength(512)
                        .HasColumnType("nvarchar(512)");

                    b.Property<string>("CardHolderName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("CardType")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("ExpiryMonth")
                        .HasColumnType("int");

                    b.Property<int>("ExpiryYear")
                        .HasColumnType("int");

                    b.Property<bool>("IsDefault")
                        .HasColumnType("bit");

                    b.Property<string>("Last4")
                        .IsRequired()
                        .HasMaxLength(4)
                        .HasColumnType("nvarchar(4)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("SavedCards");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AvatarUrl")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("GoogleId")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<bool>("IsVerified")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("LastLoginAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<int>("LoyaltyPoints")
                        .HasColumnType("int");

                    b.Property<bool>("NotificationBackInStock")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<bool>("NotificationOrderUpdates")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<bool>("NotificationPromotions")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<bool>("NotificationReviewReminders")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<bool>("NotificationWishlistReminders")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasMaxLength(512)
                        .HasColumnType("nvarchar(512)");

                    b.Property<string>("Phone")
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<DateTime?>("RefreshTokenExpires")
                        .HasColumnType("datetime2");

                    b.Property<string>("RefreshTokenHash")
                        .HasMaxLength(512)
                        .HasColumnType("nvarchar(512)");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.HasIndex("GoogleId")
                        .IsUnique()
                        .HasFilter("[GoogleId] IS NOT NULL");

                    b.HasIndex("Phone")
                        .IsUnique()
                        .HasFilter("[Phone] IS NOT NULL");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Wishlist", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("UserId", "ProductId")
                        .IsUnique();

                    b.ToTable("Wishlists");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Address", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("Addresses")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Cart", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.CartItem", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Cart", "Cart")
                        .WithMany("Items")
                        .HasForeignKey("CartId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.ProductVariant", "Variant")
                        .WithMany()
                        .HasForeignKey("VariantId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Cart");

                    b.Navigation("Product");

                    b.Navigation("Variant");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Category", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Category", "Parent")
                        .WithMany("Children")
                        .HasForeignKey("ParentId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Parent");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Coupon", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Category", null)
                        .WithMany()
                        .HasForeignKey("RestrictToCategoryId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("ShopFresherz.Domain.Entities.Product", null)
                        .WithMany()
                        .HasForeignKey("RestrictToProductId")
                        .OnDelete(DeleteBehavior.Restrict);
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.FlashDeal", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.ProductVariant", null)
                        .WithMany()
                        .HasForeignKey("VariantId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Product");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.LoyaltyTransaction", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Order", null)
                        .WithMany()
                        .HasForeignKey("OrderId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("LoyaltyTransactions")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Notification", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.NotifyRequest", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Product");

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Order", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Coupon", "Coupon")
                        .WithMany()
                        .HasForeignKey("CouponId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("Orders")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Coupon");

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.OrderItem", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Order", "Order")
                        .WithMany("Items")
                        .HasForeignKey("OrderId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany("OrderItems")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.ProductVariant", "Variant")
                        .WithMany()
                        .HasForeignKey("VariantId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Order");

                    b.Navigation("Product");

                    b.Navigation("Variant");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Product", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Brand", "Brand")
                        .WithMany("Products")
                        .HasForeignKey("BrandId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.Category", "Category")
                        .WithMany("Products")
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Brand");

                    b.Navigation("Category");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.ProductImage", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany("Images")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Product");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.ProductVariant", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany("Variants")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Product");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Review", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany("Reviews")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("Reviews")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Product");

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.SavedCard", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("SavedCards")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Wishlist", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("Wishlist")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Product");

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Brand", b =>
                {
                    b.Navigation("Products");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Cart", b =>
                {
                    b.Navigation("Items");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Category", b =>
                {
                    b.Navigation("Children");

                    b.Navigation("Products");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Order", b =>
                {
                    b.Navigation("Items");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Product", b =>
                {
                    b.Navigation("Images");

                    b.Navigation("OrderItems");

                    b.Navigation("Reviews");

                    b.Navigation("Variants");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.User", b =>
                {
                    b.Navigation("Addresses");

                    b.Navigation("LoyaltyTransactions");

                    b.Navigation("Orders");

                    b.Navigation("Reviews");

                    b.Navigation("SavedCards");

                    b.Navigation("Wishlist");
                });
#pragma warning restore 612, 618
        }
    }
}
```

### ShopFresherz.Infrastructure/Persistence/Migrations/20260525173045_AddPromotionalSections.Designer.cs
Summary: Source/configuration file used by the audited backend.
```csharp
// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ShopFresherz.Infrastructure.Persistence;

#nullable disable

namespace ShopFresherz.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ShopFresherzDbContext))]
    [Migration("20260525173045_AddPromotionalSections")]
    partial class AddPromotionalSections
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.14")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Address", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("City")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsDefault")
                        .HasColumnType("bit");

                    b.Property<string>("Label")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("Line1")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("Line2")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("PostalCode")
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<string>("State")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Addresses");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.AppSetting", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ValueJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("Key")
                        .IsUnique();

                    b.ToTable("AppSettings", (string)null);
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.AuditLog", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<string>("Action")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<Guid?>("ActorUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("EntityId")
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<string>("EntityType")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<string>("IpAddress")
                        .HasMaxLength(80)
                        .HasColumnType("nvarchar(80)");

                    b.Property<string>("NewValues")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("OldValues")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("CreatedAt");

                    b.HasIndex("EntityType", "EntityId");

                    b.ToTable("AuditLogs");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Brand", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("LogoUrl")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("Slug")
                        .IsUnique();

                    b.ToTable("Brands");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Cart", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("CouponCode")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("ExpiresAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("SessionId")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId")
                        .IsUnique()
                        .HasFilter("[UserId] IS NOT NULL");

                    b.ToTable("Carts");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.CartItem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("CartId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Quantity")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("VariantId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("VariantId");

                    b.HasIndex("CartId", "ProductId", "VariantId")
                        .IsUnique()
                        .HasFilter("[VariantId] IS NOT NULL");

                    b.ToTable("CartItems");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Category", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ImageUrl")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("MetaDescription")
                        .HasMaxLength(160)
                        .HasColumnType("nvarchar(160)");

                    b.Property<string>("MetaTitle")
                        .HasMaxLength(70)
                        .HasColumnType("nvarchar(70)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<int?>("ParentId")
                        .HasColumnType("int");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("ParentId");

                    b.HasIndex("Slug")
                        .IsUnique();

                    b.ToTable("Categories");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Coupon", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Code")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("ExpiresAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<bool>("IsStackable")
                        .HasColumnType("bit");

                    b.Property<int?>("MaxUses")
                        .HasColumnType("int");

                    b.Property<int?>("MaxUsesPerUser")
                        .HasColumnType("int");

                    b.Property<decimal?>("MinimumOrderAmount")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int?>("RestrictToCategoryId")
                        .HasColumnType("int");

                    b.Property<Guid?>("RestrictToProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<int>("UsedCount")
                        .HasColumnType("int");

                    b.Property<decimal>("Value")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("Id");

                    b.HasIndex("Code")
                        .IsUnique();

                    b.HasIndex("RestrictToCategoryId");

                    b.HasIndex("RestrictToProductId");

                    b.ToTable("Coupons");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.FeaturedSection", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("Section")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(350)
                        .HasColumnType("nvarchar(350)");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("Section");

                    b.ToTable("FeaturedSections");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.FlashDeal", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("EndsAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<int?>("MaxQuantity")
                        .HasColumnType("int");

                    b.Property<decimal>("OriginalPrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal>("SalePrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("SoldQuantity")
                        .HasColumnType("int");

                    b.Property<DateTime>("StartsAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("VariantId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("VariantId");

                    b.ToTable("FlashDeals");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.HomepageBanner", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("CtaText")
                        .HasMaxLength(80)
                        .HasColumnType("nvarchar(80)");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("EndsAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ImageUrl")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("LinkUrl")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<DateTime?>("StartsAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("SubTitle")
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("IsActive", "SortOrder");

                    b.ToTable("HomepageBanners");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.LoyaltyTransaction", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("EventType")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<DateTime>("ExpiresAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("OrderId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Points")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("OrderId");

                    b.HasIndex("UserId");

                    b.ToTable("LoyaltyTransactions");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Notification", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsRead")
                        .HasColumnType("bit");

                    b.Property<string>("LinkUrl")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Notifications");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.NotifyRequest", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsNotified")
                        .HasColumnType("bit");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("UserId", "ProductId")
                        .IsUnique();

                    b.ToTable("NotifyRequests");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Order", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<int?>("CouponId")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("DeliveryAddressJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("DeliveryFee")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(18,2)")
                        .HasDefaultValue(0m);

                    b.Property<string>("DeliveryMethod")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<decimal>("DiscountAmount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(18,2)")
                        .HasDefaultValue(0m);

                    b.Property<DateTime?>("EstimatedDelivery")
                        .HasColumnType("datetime2");

                    b.Property<string>("GuestEmail")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("Notes")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<string>("OrderNumber")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<string>("PaymentMethod")
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<string>("PaymentReference")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("PaymentStatus")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<decimal>("Subtotal")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("TrackingNumber")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal>("VatAmount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(18,2)")
                        .HasDefaultValue(0m);

                    b.HasKey("Id");

                    b.HasIndex("CouponId");

                    b.HasIndex("OrderNumber")
                        .IsUnique();

                    b.HasIndex("UserId");

                    b.ToTable("Orders");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.OrderItem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("LineTotal")
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid>("OrderId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("ProductSnapshotJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Quantity")
                        .HasColumnType("int");

                    b.Property<decimal>("UnitPrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("VariantId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("OrderId");

                    b.HasIndex("ProductId");

                    b.HasIndex("VariantId");

                    b.ToTable("OrderItems");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Product", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AttributesJson")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("AverageRating")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(3,2)")
                        .HasDefaultValue(0m);

                    b.Property<Guid>("BrandId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("CategoryId")
                        .HasColumnType("int");

                    b.Property<decimal?>("CompareAtPrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal?>("CostPrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ImageUrlsJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<bool>("IsFeatured")
                        .HasColumnType("bit");

                    b.Property<string>("MetaDescription")
                        .HasMaxLength(160)
                        .HasColumnType("nvarchar(160)");

                    b.Property<string>("MetaTitle")
                        .HasMaxLength(70)
                        .HasColumnType("nvarchar(70)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.Property<decimal>("Price")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("ReservedQty")
                        .HasColumnType("int");

                    b.Property<int>("ReviewCount")
                        .HasColumnType("int");

                    b.Property<string>("SKU")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("ShortDescription")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(350)
                        .HasColumnType("nvarchar(350)");

                    b.Property<int>("SoldCount")
                        .HasColumnType("int");

                    b.Property<int>("StockQty")
                        .HasColumnType("int");

                    b.Property<string>("TagsJson")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("ViewCount")
                        .HasColumnType("int");

                    b.Property<decimal?>("WeightKg")
                        .HasColumnType("decimal(8,3)");

                    b.HasKey("Id");

                    b.HasIndex("BrandId");

                    b.HasIndex("CategoryId");

                    b.HasIndex("SKU")
                        .IsUnique();

                    b.HasIndex("Slug")
                        .IsUnique();

                    b.ToTable("Products");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.ProductImage", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("DisplayUrl")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<bool>("IsVideo")
                        .HasColumnType("bit");

                    b.Property<string>("OriginalUrl")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<string>("ThumbUrl")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ZoomUrl")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.ToTable("ProductImages");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.ProductVariant", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AttributesJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("Price")
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("ReservedQty")
                        .HasColumnType("int");

                    b.Property<string>("SKU")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<int>("StockQty")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("SKU")
                        .IsUnique();

                    b.ToTable("ProductVariants");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.PromotionalSection", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Badge")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("ButtonText")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("ContentType")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("CtaText")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<string>("Headline")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("ImageAlt")
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.Property<string>("ImageUrl")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("OriginalPriceText")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("PriceBadge")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("PriceLabel")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("PriceText")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("PriceValue")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<decimal?>("Rating")
                        .HasPrecision(3, 2)
                        .HasColumnType("decimal(3,2)");

                    b.Property<string>("SalePriceText")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("SectionKey")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("SlugId")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<string>("Subtitle")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("Tag")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("Title")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("SectionKey");

                    b.HasIndex("SlugId")
                        .IsUnique();

                    b.ToTable("PromotionalSections");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Review", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Body")
                        .HasMaxLength(2000)
                        .HasColumnType("nvarchar(2000)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsApproved")
                        .HasColumnType("bit");

                    b.Property<bool>("IsVerifiedPurchase")
                        .HasColumnType("bit");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Rating")
                        .HasColumnType("int");

                    b.Property<string>("Title")
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("UserId", "ProductId")
                        .IsUnique();

                    b.ToTable("Reviews");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.SavedCard", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AuthorizationCode")
                        .HasMaxLength(512)
                        .HasColumnType("nvarchar(512)");

                    b.Property<string>("CardHolderName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("CardType")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("ExpiryMonth")
                        .HasColumnType("int");

                    b.Property<int>("ExpiryYear")
                        .HasColumnType("int");

                    b.Property<bool>("IsDefault")
                        .HasColumnType("bit");

                    b.Property<string>("Last4")
                        .IsRequired()
                        .HasMaxLength(4)
                        .HasColumnType("nvarchar(4)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("SavedCards");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AvatarUrl")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("GoogleId")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<bool>("IsVerified")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("LastLoginAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<int>("LoyaltyPoints")
                        .HasColumnType("int");

                    b.Property<bool>("NotificationBackInStock")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<bool>("NotificationOrderUpdates")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<bool>("NotificationPromotions")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<bool>("NotificationReviewReminders")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<bool>("NotificationWishlistReminders")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasMaxLength(512)
                        .HasColumnType("nvarchar(512)");

                    b.Property<string>("Phone")
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<DateTime?>("RefreshTokenExpires")
                        .HasColumnType("datetime2");

                    b.Property<string>("RefreshTokenHash")
                        .HasMaxLength(512)
                        .HasColumnType("nvarchar(512)");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.HasIndex("GoogleId")
                        .IsUnique()
                        .HasFilter("[GoogleId] IS NOT NULL");

                    b.HasIndex("Phone")
                        .IsUnique()
                        .HasFilter("[Phone] IS NOT NULL");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Wishlist", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("UserId", "ProductId")
                        .IsUnique();

                    b.ToTable("Wishlists");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Address", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("Addresses")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Cart", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.CartItem", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Cart", "Cart")
                        .WithMany("Items")
                        .HasForeignKey("CartId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.ProductVariant", "Variant")
                        .WithMany()
                        .HasForeignKey("VariantId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Cart");

                    b.Navigation("Product");

                    b.Navigation("Variant");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Category", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Category", "Parent")
                        .WithMany("Children")
                        .HasForeignKey("ParentId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Parent");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Coupon", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Category", null)
                        .WithMany()
                        .HasForeignKey("RestrictToCategoryId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("ShopFresherz.Domain.Entities.Product", null)
                        .WithMany()
                        .HasForeignKey("RestrictToProductId")
                        .OnDelete(DeleteBehavior.Restrict);
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.FlashDeal", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.ProductVariant", null)
                        .WithMany()
                        .HasForeignKey("VariantId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Product");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.LoyaltyTransaction", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Order", null)
                        .WithMany()
                        .HasForeignKey("OrderId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("LoyaltyTransactions")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Notification", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.NotifyRequest", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Product");

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Order", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Coupon", "Coupon")
                        .WithMany()
                        .HasForeignKey("CouponId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("Orders")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Coupon");

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.OrderItem", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Order", "Order")
                        .WithMany("Items")
                        .HasForeignKey("OrderId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany("OrderItems")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.ProductVariant", "Variant")
                        .WithMany()
                        .HasForeignKey("VariantId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Order");

                    b.Navigation("Product");

                    b.Navigation("Variant");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Product", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Brand", "Brand")
                        .WithMany("Products")
                        .HasForeignKey("BrandId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.Category", "Category")
                        .WithMany("Products")
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Brand");

                    b.Navigation("Category");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.ProductImage", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany("Images")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Product");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.ProductVariant", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany("Variants")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Product");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Review", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany("Reviews")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("Reviews")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Product");

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.SavedCard", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("SavedCards")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Wishlist", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("Wishlist")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Product");

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Brand", b =>
                {
                    b.Navigation("Products");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Cart", b =>
                {
                    b.Navigation("Items");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Category", b =>
                {
                    b.Navigation("Children");

                    b.Navigation("Products");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Order", b =>
                {
                    b.Navigation("Items");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Product", b =>
                {
                    b.Navigation("Images");

                    b.Navigation("OrderItems");

                    b.Navigation("Reviews");

                    b.Navigation("Variants");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.User", b =>
                {
                    b.Navigation("Addresses");

                    b.Navigation("LoyaltyTransactions");

                    b.Navigation("Orders");

                    b.Navigation("Reviews");

                    b.Navigation("SavedCards");

                    b.Navigation("Wishlist");
                });
#pragma warning restore 612, 618
        }
    }
}
```

### ShopFresherz.Infrastructure/Persistence/Migrations/20260526085717_RenameCardLast4ToCardNumber.Designer.cs
Summary: Source/configuration file used by the audited backend.
```csharp
// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ShopFresherz.Infrastructure.Persistence;

#nullable disable

namespace ShopFresherz.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ShopFresherzDbContext))]
    [Migration("20260526085717_RenameCardLast4ToCardNumber")]
    partial class RenameCardLast4ToCardNumber
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.14")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Address", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("City")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsDefault")
                        .HasColumnType("bit");

                    b.Property<string>("Label")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("Line1")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("Line2")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("PostalCode")
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<string>("State")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Addresses");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.AppSetting", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ValueJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("Key")
                        .IsUnique();

                    b.ToTable("AppSettings", (string)null);
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.AuditLog", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<string>("Action")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<Guid?>("ActorUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("EntityId")
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<string>("EntityType")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<string>("IpAddress")
                        .HasMaxLength(80)
                        .HasColumnType("nvarchar(80)");

                    b.Property<string>("NewValues")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("OldValues")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("CreatedAt");

                    b.HasIndex("EntityType", "EntityId");

                    b.ToTable("AuditLogs");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Brand", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("LogoUrl")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("Slug")
                        .IsUnique();

                    b.ToTable("Brands");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Cart", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("CouponCode")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("ExpiresAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("SessionId")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId")
                        .IsUnique()
                        .HasFilter("[UserId] IS NOT NULL");

                    b.ToTable("Carts");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.CartItem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("CartId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Quantity")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("VariantId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("VariantId");

                    b.HasIndex("CartId", "ProductId", "VariantId")
                        .IsUnique()
                        .HasFilter("[VariantId] IS NOT NULL");

                    b.ToTable("CartItems");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Category", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ImageUrl")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("MetaDescription")
                        .HasMaxLength(160)
                        .HasColumnType("nvarchar(160)");

                    b.Property<string>("MetaTitle")
                        .HasMaxLength(70)
                        .HasColumnType("nvarchar(70)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<int?>("ParentId")
                        .HasColumnType("int");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("ParentId");

                    b.HasIndex("Slug")
                        .IsUnique();

                    b.ToTable("Categories");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Coupon", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Code")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("ExpiresAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<bool>("IsStackable")
                        .HasColumnType("bit");

                    b.Property<int?>("MaxUses")
                        .HasColumnType("int");

                    b.Property<int?>("MaxUsesPerUser")
                        .HasColumnType("int");

                    b.Property<decimal?>("MinimumOrderAmount")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int?>("RestrictToCategoryId")
                        .HasColumnType("int");

                    b.Property<Guid?>("RestrictToProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<int>("UsedCount")
                        .HasColumnType("int");

                    b.Property<decimal>("Value")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("Id");

                    b.HasIndex("Code")
                        .IsUnique();

                    b.HasIndex("RestrictToCategoryId");

                    b.HasIndex("RestrictToProductId");

                    b.ToTable("Coupons");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.FeaturedSection", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("Section")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(350)
                        .HasColumnType("nvarchar(350)");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("Section");

                    b.ToTable("FeaturedSections");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.FlashDeal", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("EndsAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<int?>("MaxQuantity")
                        .HasColumnType("int");

                    b.Property<decimal>("OriginalPrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal>("SalePrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("SoldQuantity")
                        .HasColumnType("int");

                    b.Property<DateTime>("StartsAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("VariantId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("VariantId");

                    b.ToTable("FlashDeals");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.HomepageBanner", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("CtaText")
                        .HasMaxLength(80)
                        .HasColumnType("nvarchar(80)");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("EndsAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ImageUrl")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("LinkUrl")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<DateTime?>("StartsAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("SubTitle")
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("IsActive", "SortOrder");

                    b.ToTable("HomepageBanners");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.LoyaltyTransaction", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("EventType")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<DateTime>("ExpiresAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("OrderId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Points")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("OrderId");

                    b.HasIndex("UserId");

                    b.ToTable("LoyaltyTransactions");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Notification", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsRead")
                        .HasColumnType("bit");

                    b.Property<string>("LinkUrl")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Notifications");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.NotifyRequest", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsNotified")
                        .HasColumnType("bit");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("UserId", "ProductId")
                        .IsUnique();

                    b.ToTable("NotifyRequests");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Order", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<int?>("CouponId")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("DeliveryAddressJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("DeliveryFee")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(18,2)")
                        .HasDefaultValue(0m);

                    b.Property<string>("DeliveryMethod")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<decimal>("DiscountAmount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(18,2)")
                        .HasDefaultValue(0m);

                    b.Property<DateTime?>("EstimatedDelivery")
                        .HasColumnType("datetime2");

                    b.Property<string>("GuestEmail")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("Notes")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<string>("OrderNumber")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<string>("PaymentMethod")
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<string>("PaymentReference")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("PaymentStatus")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<decimal>("Subtotal")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("TrackingNumber")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal>("VatAmount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(18,2)")
                        .HasDefaultValue(0m);

                    b.HasKey("Id");

                    b.HasIndex("CouponId");

                    b.HasIndex("OrderNumber")
                        .IsUnique();

                    b.HasIndex("UserId");

                    b.ToTable("Orders");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.OrderItem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("LineTotal")
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid>("OrderId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("ProductSnapshotJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Quantity")
                        .HasColumnType("int");

                    b.Property<decimal>("UnitPrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("VariantId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("OrderId");

                    b.HasIndex("ProductId");

                    b.HasIndex("VariantId");

                    b.ToTable("OrderItems");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Product", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AttributesJson")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("AverageRating")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(3,2)")
                        .HasDefaultValue(0m);

                    b.Property<Guid>("BrandId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("CategoryId")
                        .HasColumnType("int");

                    b.Property<decimal?>("CompareAtPrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal?>("CostPrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ImageUrlsJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<bool>("IsFeatured")
                        .HasColumnType("bit");

                    b.Property<string>("MetaDescription")
                        .HasMaxLength(160)
                        .HasColumnType("nvarchar(160)");

                    b.Property<string>("MetaTitle")
                        .HasMaxLength(70)
                        .HasColumnType("nvarchar(70)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.Property<decimal>("Price")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("ReservedQty")
                        .HasColumnType("int");

                    b.Property<int>("ReviewCount")
                        .HasColumnType("int");

                    b.Property<string>("SKU")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("ShortDescription")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(350)
                        .HasColumnType("nvarchar(350)");

                    b.Property<int>("SoldCount")
                        .HasColumnType("int");

                    b.Property<int>("StockQty")
                        .HasColumnType("int");

                    b.Property<string>("TagsJson")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("ViewCount")
                        .HasColumnType("int");

                    b.Property<decimal?>("WeightKg")
                        .HasColumnType("decimal(8,3)");

                    b.HasKey("Id");

                    b.HasIndex("BrandId");

                    b.HasIndex("CategoryId");

                    b.HasIndex("SKU")
                        .IsUnique();

                    b.HasIndex("Slug")
                        .IsUnique();

                    b.ToTable("Products");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.ProductImage", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("DisplayUrl")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<bool>("IsVideo")
                        .HasColumnType("bit");

                    b.Property<string>("OriginalUrl")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<string>("ThumbUrl")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ZoomUrl")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.ToTable("ProductImages");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.ProductVariant", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AttributesJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("Price")
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("ReservedQty")
                        .HasColumnType("int");

                    b.Property<string>("SKU")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<int>("StockQty")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("SKU")
                        .IsUnique();

                    b.ToTable("ProductVariants");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.PromotionalSection", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Badge")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("ButtonText")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("ContentType")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("CtaText")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<string>("Headline")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("ImageAlt")
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.Property<string>("ImageUrl")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("OriginalPriceText")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("PriceBadge")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("PriceLabel")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("PriceText")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("PriceValue")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<decimal?>("Rating")
                        .HasPrecision(3, 2)
                        .HasColumnType("decimal(3,2)");

                    b.Property<string>("SalePriceText")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("SectionKey")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("SlugId")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<string>("Subtitle")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("Tag")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("Title")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("SectionKey");

                    b.HasIndex("SlugId")
                        .IsUnique();

                    b.ToTable("PromotionalSections");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Review", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Body")
                        .HasMaxLength(2000)
                        .HasColumnType("nvarchar(2000)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsApproved")
                        .HasColumnType("bit");

                    b.Property<bool>("IsVerifiedPurchase")
                        .HasColumnType("bit");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Rating")
                        .HasColumnType("int");

                    b.Property<string>("Title")
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("UserId", "ProductId")
                        .IsUnique();

                    b.ToTable("Reviews");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.SavedCard", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AuthorizationCode")
                        .HasMaxLength(512)
                        .HasColumnType("nvarchar(512)");

                    b.Property<string>("CardHolderName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("CardNumber")
                        .IsRequired()
                        .HasMaxLength(19)
                        .HasColumnType("nvarchar(19)");

                    b.Property<string>("CardType")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("ExpiryMonth")
                        .HasColumnType("int");

                    b.Property<int>("ExpiryYear")
                        .HasColumnType("int");

                    b.Property<bool>("IsDefault")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("SavedCards");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AvatarUrl")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("GoogleId")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<bool>("IsVerified")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("LastLoginAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<int>("LoyaltyPoints")
                        .HasColumnType("int");

                    b.Property<bool>("NotificationBackInStock")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<bool>("NotificationOrderUpdates")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<bool>("NotificationPromotions")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<bool>("NotificationReviewReminders")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<bool>("NotificationWishlistReminders")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasMaxLength(512)
                        .HasColumnType("nvarchar(512)");

                    b.Property<string>("Phone")
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<DateTime?>("RefreshTokenExpires")
                        .HasColumnType("datetime2");

                    b.Property<string>("RefreshTokenHash")
                        .HasMaxLength(512)
                        .HasColumnType("nvarchar(512)");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.HasIndex("GoogleId")
                        .IsUnique()
                        .HasFilter("[GoogleId] IS NOT NULL");

                    b.HasIndex("Phone")
                        .IsUnique()
                        .HasFilter("[Phone] IS NOT NULL");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Wishlist", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("UserId", "ProductId")
                        .IsUnique();

                    b.ToTable("Wishlists");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Address", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("Addresses")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Cart", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.CartItem", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Cart", "Cart")
                        .WithMany("Items")
                        .HasForeignKey("CartId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.ProductVariant", "Variant")
                        .WithMany()
                        .HasForeignKey("VariantId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Cart");

                    b.Navigation("Product");

                    b.Navigation("Variant");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Category", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Category", "Parent")
                        .WithMany("Children")
                        .HasForeignKey("ParentId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Parent");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Coupon", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Category", null)
                        .WithMany()
                        .HasForeignKey("RestrictToCategoryId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("ShopFresherz.Domain.Entities.Product", null)
                        .WithMany()
                        .HasForeignKey("RestrictToProductId")
                        .OnDelete(DeleteBehavior.Restrict);
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.FlashDeal", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.ProductVariant", null)
                        .WithMany()
                        .HasForeignKey("VariantId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Product");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.LoyaltyTransaction", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Order", null)
                        .WithMany()
                        .HasForeignKey("OrderId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("LoyaltyTransactions")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Notification", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.NotifyRequest", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Product");

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Order", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Coupon", "Coupon")
                        .WithMany()
                        .HasForeignKey("CouponId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("Orders")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Coupon");

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.OrderItem", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Order", "Order")
                        .WithMany("Items")
                        .HasForeignKey("OrderId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany("OrderItems")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.ProductVariant", "Variant")
                        .WithMany()
                        .HasForeignKey("VariantId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Order");

                    b.Navigation("Product");

                    b.Navigation("Variant");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Product", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Brand", "Brand")
                        .WithMany("Products")
                        .HasForeignKey("BrandId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.Category", "Category")
                        .WithMany("Products")
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Brand");

                    b.Navigation("Category");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.ProductImage", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany("Images")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Product");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.ProductVariant", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany("Variants")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Product");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Review", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany("Reviews")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("Reviews")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Product");

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.SavedCard", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("SavedCards")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Wishlist", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("Wishlist")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Product");

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Brand", b =>
                {
                    b.Navigation("Products");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Cart", b =>
                {
                    b.Navigation("Items");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Category", b =>
                {
                    b.Navigation("Children");

                    b.Navigation("Products");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Order", b =>
                {
                    b.Navigation("Items");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Product", b =>
                {
                    b.Navigation("Images");

                    b.Navigation("OrderItems");

                    b.Navigation("Reviews");

                    b.Navigation("Variants");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.User", b =>
                {
                    b.Navigation("Addresses");

                    b.Navigation("LoyaltyTransactions");

                    b.Navigation("Orders");

                    b.Navigation("Reviews");

                    b.Navigation("SavedCards");

                    b.Navigation("Wishlist");
                });
#pragma warning restore 612, 618
        }
    }
}
```

### ShopFresherz.Infrastructure/Persistence/Migrations/20260526143711_AddFeaturedSectionProductFields.Designer.cs
Summary: Source/configuration file used by the audited backend.
```csharp
// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ShopFresherz.Infrastructure.Persistence;

#nullable disable

namespace ShopFresherz.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ShopFresherzDbContext))]
    [Migration("20260526143711_AddFeaturedSectionProductFields")]
    partial class AddFeaturedSectionProductFields
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.14")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Address", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("City")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsDefault")
                        .HasColumnType("bit");

                    b.Property<string>("Label")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("Line1")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("Line2")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("PostalCode")
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<string>("State")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Addresses");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.AppSetting", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ValueJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("Key")
                        .IsUnique();

                    b.ToTable("AppSettings", (string)null);
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.AuditLog", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<string>("Action")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<Guid?>("ActorUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("EntityId")
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<string>("EntityType")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<string>("IpAddress")
                        .HasMaxLength(80)
                        .HasColumnType("nvarchar(80)");

                    b.Property<string>("NewValues")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("OldValues")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("CreatedAt");

                    b.HasIndex("EntityType", "EntityId");

                    b.ToTable("AuditLogs");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Brand", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("LogoUrl")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("Slug")
                        .IsUnique();

                    b.ToTable("Brands");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Cart", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("CouponCode")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("ExpiresAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("SessionId")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId")
                        .IsUnique()
                        .HasFilter("[UserId] IS NOT NULL");

                    b.ToTable("Carts");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.CartItem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("CartId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Quantity")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("VariantId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("VariantId");

                    b.HasIndex("CartId", "ProductId", "VariantId")
                        .IsUnique()
                        .HasFilter("[VariantId] IS NOT NULL");

                    b.ToTable("CartItems");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Category", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ImageUrl")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("MetaDescription")
                        .HasMaxLength(160)
                        .HasColumnType("nvarchar(160)");

                    b.Property<string>("MetaTitle")
                        .HasMaxLength(70)
                        .HasColumnType("nvarchar(70)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<int?>("ParentId")
                        .HasColumnType("int");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("ParentId");

                    b.HasIndex("Slug")
                        .IsUnique();

                    b.ToTable("Categories");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Coupon", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Code")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("ExpiresAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<bool>("IsStackable")
                        .HasColumnType("bit");

                    b.Property<int?>("MaxUses")
                        .HasColumnType("int");

                    b.Property<int?>("MaxUsesPerUser")
                        .HasColumnType("int");

                    b.Property<decimal?>("MinimumOrderAmount")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int?>("RestrictToCategoryId")
                        .HasColumnType("int");

                    b.Property<Guid?>("RestrictToProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<int>("UsedCount")
                        .HasColumnType("int");

                    b.Property<decimal>("Value")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("Id");

                    b.HasIndex("Code")
                        .IsUnique();

                    b.HasIndex("RestrictToCategoryId");

                    b.HasIndex("RestrictToProductId");

                    b.ToTable("Coupons");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.FeaturedSection", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Badge")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("CtaText")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("ImageUrl")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("Price")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Section")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(350)
                        .HasColumnType("nvarchar(350)");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<string>("Tag")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("Section");

                    b.ToTable("FeaturedSections");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.FlashDeal", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("EndsAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<int?>("MaxQuantity")
                        .HasColumnType("int");

                    b.Property<decimal>("OriginalPrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal>("SalePrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("SoldQuantity")
                        .HasColumnType("int");

                    b.Property<DateTime>("StartsAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("VariantId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("VariantId");

                    b.ToTable("FlashDeals");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.HomepageBanner", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("CtaText")
                        .HasMaxLength(80)
                        .HasColumnType("nvarchar(80)");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("EndsAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ImageUrl")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("LinkUrl")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<DateTime?>("StartsAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("SubTitle")
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("IsActive", "SortOrder");

                    b.ToTable("HomepageBanners");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.LoyaltyTransaction", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("EventType")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<DateTime>("ExpiresAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("OrderId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Points")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("OrderId");

                    b.HasIndex("UserId");

                    b.ToTable("LoyaltyTransactions");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Notification", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsRead")
                        .HasColumnType("bit");

                    b.Property<string>("LinkUrl")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Notifications");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.NotifyRequest", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsNotified")
                        .HasColumnType("bit");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("UserId", "ProductId")
                        .IsUnique();

                    b.ToTable("NotifyRequests");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Order", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<int?>("CouponId")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("DeliveryAddressJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("DeliveryFee")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(18,2)")
                        .HasDefaultValue(0m);

                    b.Property<string>("DeliveryMethod")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<decimal>("DiscountAmount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(18,2)")
                        .HasDefaultValue(0m);

                    b.Property<DateTime?>("EstimatedDelivery")
                        .HasColumnType("datetime2");

                    b.Property<string>("GuestEmail")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("Notes")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<string>("OrderNumber")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<string>("PaymentMethod")
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<string>("PaymentReference")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("PaymentStatus")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<decimal>("Subtotal")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("TrackingNumber")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal>("VatAmount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(18,2)")
                        .HasDefaultValue(0m);

                    b.HasKey("Id");

                    b.HasIndex("CouponId");

                    b.HasIndex("OrderNumber")
                        .IsUnique();

                    b.HasIndex("UserId");

                    b.ToTable("Orders");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.OrderItem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("LineTotal")
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid>("OrderId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("ProductSnapshotJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Quantity")
                        .HasColumnType("int");

                    b.Property<decimal>("UnitPrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("VariantId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("OrderId");

                    b.HasIndex("ProductId");

                    b.HasIndex("VariantId");

                    b.ToTable("OrderItems");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Product", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AttributesJson")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("AverageRating")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(3,2)")
                        .HasDefaultValue(0m);

                    b.Property<Guid>("BrandId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("CategoryId")
                        .HasColumnType("int");

                    b.Property<decimal?>("CompareAtPrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal?>("CostPrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ImageUrlsJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<bool>("IsFeatured")
                        .HasColumnType("bit");

                    b.Property<string>("MetaDescription")
                        .HasMaxLength(160)
                        .HasColumnType("nvarchar(160)");

                    b.Property<string>("MetaTitle")
                        .HasMaxLength(70)
                        .HasColumnType("nvarchar(70)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.Property<decimal>("Price")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("ReservedQty")
                        .HasColumnType("int");

                    b.Property<int>("ReviewCount")
                        .HasColumnType("int");

                    b.Property<string>("SKU")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("ShortDescription")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(350)
                        .HasColumnType("nvarchar(350)");

                    b.Property<int>("SoldCount")
                        .HasColumnType("int");

                    b.Property<int>("StockQty")
                        .HasColumnType("int");

                    b.Property<string>("TagsJson")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("ViewCount")
                        .HasColumnType("int");

                    b.Property<decimal?>("WeightKg")
                        .HasColumnType("decimal(8,3)");

                    b.HasKey("Id");

                    b.HasIndex("BrandId");

                    b.HasIndex("CategoryId");

                    b.HasIndex("SKU")
                        .IsUnique();

                    b.HasIndex("Slug")
                        .IsUnique();

                    b.ToTable("Products");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.ProductImage", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("DisplayUrl")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<bool>("IsVideo")
                        .HasColumnType("bit");

                    b.Property<string>("OriginalUrl")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<string>("ThumbUrl")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ZoomUrl")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.ToTable("ProductImages");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.ProductVariant", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AttributesJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("Price")
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("ReservedQty")
                        .HasColumnType("int");

                    b.Property<string>("SKU")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<int>("StockQty")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("SKU")
                        .IsUnique();

                    b.ToTable("ProductVariants");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.PromotionalSection", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Badge")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("ButtonText")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("ContentType")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("CtaText")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<string>("Headline")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("ImageAlt")
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.Property<string>("ImageUrl")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("OriginalPriceText")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("PriceBadge")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("PriceLabel")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("PriceText")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("PriceValue")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<decimal?>("Rating")
                        .HasPrecision(3, 2)
                        .HasColumnType("decimal(3,2)");

                    b.Property<string>("SalePriceText")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("SectionKey")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("SlugId")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<string>("Subtitle")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("Tag")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("Title")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("SectionKey");

                    b.HasIndex("SlugId")
                        .IsUnique();

                    b.ToTable("PromotionalSections");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Review", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Body")
                        .HasMaxLength(2000)
                        .HasColumnType("nvarchar(2000)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsApproved")
                        .HasColumnType("bit");

                    b.Property<bool>("IsVerifiedPurchase")
                        .HasColumnType("bit");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Rating")
                        .HasColumnType("int");

                    b.Property<string>("Title")
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("UserId", "ProductId")
                        .IsUnique();

                    b.ToTable("Reviews");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.SavedCard", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AuthorizationCode")
                        .HasMaxLength(512)
                        .HasColumnType("nvarchar(512)");

                    b.Property<string>("CardHolderName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("CardNumber")
                        .IsRequired()
                        .HasMaxLength(19)
                        .HasColumnType("nvarchar(19)");

                    b.Property<string>("CardType")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("ExpiryMonth")
                        .HasColumnType("int");

                    b.Property<int>("ExpiryYear")
                        .HasColumnType("int");

                    b.Property<bool>("IsDefault")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("SavedCards");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AvatarUrl")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("GoogleId")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<bool>("IsVerified")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("LastLoginAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<int>("LoyaltyPoints")
                        .HasColumnType("int");

                    b.Property<bool>("NotificationBackInStock")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<bool>("NotificationOrderUpdates")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<bool>("NotificationPromotions")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<bool>("NotificationReviewReminders")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<bool>("NotificationWishlistReminders")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasMaxLength(512)
                        .HasColumnType("nvarchar(512)");

                    b.Property<string>("Phone")
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<DateTime?>("RefreshTokenExpires")
                        .HasColumnType("datetime2");

                    b.Property<string>("RefreshTokenHash")
                        .HasMaxLength(512)
                        .HasColumnType("nvarchar(512)");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.HasIndex("GoogleId")
                        .IsUnique()
                        .HasFilter("[GoogleId] IS NOT NULL");

                    b.HasIndex("Phone")
                        .IsUnique()
                        .HasFilter("[Phone] IS NOT NULL");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Wishlist", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("UserId", "ProductId")
                        .IsUnique();

                    b.ToTable("Wishlists");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Address", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("Addresses")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Cart", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.CartItem", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Cart", "Cart")
                        .WithMany("Items")
                        .HasForeignKey("CartId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.ProductVariant", "Variant")
                        .WithMany()
                        .HasForeignKey("VariantId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Cart");

                    b.Navigation("Product");

                    b.Navigation("Variant");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Category", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Category", "Parent")
                        .WithMany("Children")
                        .HasForeignKey("ParentId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Parent");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Coupon", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Category", null)
                        .WithMany()
                        .HasForeignKey("RestrictToCategoryId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("ShopFresherz.Domain.Entities.Product", null)
                        .WithMany()
                        .HasForeignKey("RestrictToProductId")
                        .OnDelete(DeleteBehavior.Restrict);
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.FlashDeal", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.ProductVariant", null)
                        .WithMany()
                        .HasForeignKey("VariantId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Product");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.LoyaltyTransaction", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Order", null)
                        .WithMany()
                        .HasForeignKey("OrderId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("LoyaltyTransactions")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Notification", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.NotifyRequest", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Product");

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Order", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Coupon", "Coupon")
                        .WithMany()
                        .HasForeignKey("CouponId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("Orders")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Coupon");

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.OrderItem", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Order", "Order")
                        .WithMany("Items")
                        .HasForeignKey("OrderId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany("OrderItems")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.ProductVariant", "Variant")
                        .WithMany()
                        .HasForeignKey("VariantId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Order");

                    b.Navigation("Product");

                    b.Navigation("Variant");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Product", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Brand", "Brand")
                        .WithMany("Products")
                        .HasForeignKey("BrandId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.Category", "Category")
                        .WithMany("Products")
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Brand");

                    b.Navigation("Category");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.ProductImage", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany("Images")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Product");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.ProductVariant", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany("Variants")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Product");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Review", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany("Reviews")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("Reviews")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Product");

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.SavedCard", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("SavedCards")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Wishlist", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("Wishlist")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Product");

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Brand", b =>
                {
                    b.Navigation("Products");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Cart", b =>
                {
                    b.Navigation("Items");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Category", b =>
                {
                    b.Navigation("Children");

                    b.Navigation("Products");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Order", b =>
                {
                    b.Navigation("Items");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Product", b =>
                {
                    b.Navigation("Images");

                    b.Navigation("OrderItems");

                    b.Navigation("Reviews");

                    b.Navigation("Variants");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.User", b =>
                {
                    b.Navigation("Addresses");

                    b.Navigation("LoyaltyTransactions");

                    b.Navigation("Orders");

                    b.Navigation("Reviews");

                    b.Navigation("SavedCards");

                    b.Navigation("Wishlist");
                });
#pragma warning restore 612, 618
        }
    }
}
```

### ShopFresherz.Infrastructure/Persistence/Migrations/20260528092418_AddPromotionalSectionSlug.Designer.cs
Summary: Source/configuration file used by the audited backend.
```csharp
// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ShopFresherz.Infrastructure.Persistence;

#nullable disable

namespace ShopFresherz.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ShopFresherzDbContext))]
    [Migration("20260528092418_AddPromotionalSectionSlug")]
    partial class AddPromotionalSectionSlug
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.14")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Address", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("City")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsDefault")
                        .HasColumnType("bit");

                    b.Property<string>("Label")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("Line1")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("Line2")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("PostalCode")
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<string>("State")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Addresses");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.AppSetting", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ValueJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("Key")
                        .IsUnique();

                    b.ToTable("AppSettings", (string)null);
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.AuditLog", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<string>("Action")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<Guid?>("ActorUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("EntityId")
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<string>("EntityType")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<string>("IpAddress")
                        .HasMaxLength(80)
                        .HasColumnType("nvarchar(80)");

                    b.Property<string>("NewValues")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("OldValues")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("CreatedAt");

                    b.HasIndex("EntityType", "EntityId");

                    b.ToTable("AuditLogs");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Brand", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("LogoUrl")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("Slug")
                        .IsUnique();

                    b.ToTable("Brands");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Cart", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("CouponCode")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("ExpiresAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("SessionId")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId")
                        .IsUnique()
                        .HasFilter("[UserId] IS NOT NULL");

                    b.ToTable("Carts");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.CartItem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("CartId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Quantity")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("VariantId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("VariantId");

                    b.HasIndex("CartId", "ProductId", "VariantId")
                        .IsUnique()
                        .HasFilter("[VariantId] IS NOT NULL");

                    b.ToTable("CartItems");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Category", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ImageUrl")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("MetaDescription")
                        .HasMaxLength(160)
                        .HasColumnType("nvarchar(160)");

                    b.Property<string>("MetaTitle")
                        .HasMaxLength(70)
                        .HasColumnType("nvarchar(70)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<int?>("ParentId")
                        .HasColumnType("int");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("ParentId");

                    b.HasIndex("Slug")
                        .IsUnique();

                    b.ToTable("Categories");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Coupon", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Code")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("ExpiresAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<bool>("IsStackable")
                        .HasColumnType("bit");

                    b.Property<int?>("MaxUses")
                        .HasColumnType("int");

                    b.Property<int?>("MaxUsesPerUser")
                        .HasColumnType("int");

                    b.Property<decimal?>("MinimumOrderAmount")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int?>("RestrictToCategoryId")
                        .HasColumnType("int");

                    b.Property<Guid?>("RestrictToProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<int>("UsedCount")
                        .HasColumnType("int");

                    b.Property<decimal>("Value")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("Id");

                    b.HasIndex("Code")
                        .IsUnique();

                    b.HasIndex("RestrictToCategoryId");

                    b.HasIndex("RestrictToProductId");

                    b.ToTable("Coupons");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.FeaturedSection", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Badge")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("CtaText")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("ImageUrl")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("Price")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Section")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(350)
                        .HasColumnType("nvarchar(350)");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<string>("Tag")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("Section");

                    b.ToTable("FeaturedSections");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.FlashDeal", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("EndsAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<int?>("MaxQuantity")
                        .HasColumnType("int");

                    b.Property<decimal>("OriginalPrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal>("SalePrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("SoldQuantity")
                        .HasColumnType("int");

                    b.Property<DateTime>("StartsAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("VariantId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("VariantId");

                    b.ToTable("FlashDeals");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.HomepageBanner", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("CtaText")
                        .HasMaxLength(80)
                        .HasColumnType("nvarchar(80)");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("EndsAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ImageUrl")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("LinkUrl")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<DateTime?>("StartsAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("SubTitle")
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("IsActive", "SortOrder");

                    b.ToTable("HomepageBanners");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.LoyaltyTransaction", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("EventType")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<DateTime>("ExpiresAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("OrderId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Points")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("OrderId");

                    b.HasIndex("UserId");

                    b.ToTable("LoyaltyTransactions");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Notification", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsRead")
                        .HasColumnType("bit");

                    b.Property<string>("LinkUrl")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Notifications");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.NotifyRequest", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsNotified")
                        .HasColumnType("bit");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("UserId", "ProductId")
                        .IsUnique();

                    b.ToTable("NotifyRequests");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Order", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<int?>("CouponId")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("DeliveryAddressJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("DeliveryFee")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(18,2)")
                        .HasDefaultValue(0m);

                    b.Property<string>("DeliveryMethod")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<decimal>("DiscountAmount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(18,2)")
                        .HasDefaultValue(0m);

                    b.Property<DateTime?>("EstimatedDelivery")
                        .HasColumnType("datetime2");

                    b.Property<string>("GuestEmail")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("Notes")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<string>("OrderNumber")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<string>("PaymentMethod")
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<string>("PaymentReference")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("PaymentStatus")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<decimal>("Subtotal")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("TrackingNumber")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal>("VatAmount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(18,2)")
                        .HasDefaultValue(0m);

                    b.HasKey("Id");

                    b.HasIndex("CouponId");

                    b.HasIndex("OrderNumber")
                        .IsUnique();

                    b.HasIndex("UserId");

                    b.ToTable("Orders");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.OrderItem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("LineTotal")
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid>("OrderId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("ProductSnapshotJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Quantity")
                        .HasColumnType("int");

                    b.Property<decimal>("UnitPrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("VariantId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("OrderId");

                    b.HasIndex("ProductId");

                    b.HasIndex("VariantId");

                    b.ToTable("OrderItems");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Product", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AttributesJson")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("AverageRating")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(3,2)")
                        .HasDefaultValue(0m);

                    b.Property<Guid>("BrandId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("CategoryId")
                        .HasColumnType("int");

                    b.Property<decimal?>("CompareAtPrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal?>("CostPrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ImageUrlsJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<bool>("IsFeatured")
                        .HasColumnType("bit");

                    b.Property<string>("MetaDescription")
                        .HasMaxLength(160)
                        .HasColumnType("nvarchar(160)");

                    b.Property<string>("MetaTitle")
                        .HasMaxLength(70)
                        .HasColumnType("nvarchar(70)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.Property<decimal>("Price")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("ReservedQty")
                        .HasColumnType("int");

                    b.Property<int>("ReviewCount")
                        .HasColumnType("int");

                    b.Property<string>("SKU")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("ShortDescription")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(350)
                        .HasColumnType("nvarchar(350)");

                    b.Property<int>("SoldCount")
                        .HasColumnType("int");

                    b.Property<int>("StockQty")
                        .HasColumnType("int");

                    b.Property<string>("TagsJson")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("ViewCount")
                        .HasColumnType("int");

                    b.Property<decimal?>("WeightKg")
                        .HasColumnType("decimal(8,3)");

                    b.HasKey("Id");

                    b.HasIndex("BrandId");

                    b.HasIndex("CategoryId");

                    b.HasIndex("SKU")
                        .IsUnique();

                    b.HasIndex("Slug")
                        .IsUnique();

                    b.ToTable("Products");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.ProductImage", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("DisplayUrl")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<bool>("IsVideo")
                        .HasColumnType("bit");

                    b.Property<string>("OriginalUrl")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<string>("ThumbUrl")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ZoomUrl")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.ToTable("ProductImages");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.ProductVariant", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AttributesJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("Price")
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("ReservedQty")
                        .HasColumnType("int");

                    b.Property<string>("SKU")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<int>("StockQty")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("SKU")
                        .IsUnique();

                    b.ToTable("ProductVariants");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.PromotionalSection", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Badge")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("ButtonText")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("ContentType")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("CtaText")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<string>("Headline")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("ImageAlt")
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.Property<string>("ImageUrl")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("OriginalPriceText")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("PriceBadge")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("PriceLabel")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("PriceText")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("PriceValue")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<decimal?>("Rating")
                        .HasPrecision(3, 2)
                        .HasColumnType("decimal(3,2)");

                    b.Property<string>("SalePriceText")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("SectionKey")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Slug")
                        .HasMaxLength(350)
                        .HasColumnType("nvarchar(350)");

                    b.Property<string>("SlugId")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<string>("Subtitle")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("Tag")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("Title")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("SectionKey");

                    b.HasIndex("SlugId")
                        .IsUnique();

                    b.ToTable("PromotionalSections");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Review", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Body")
                        .HasMaxLength(2000)
                        .HasColumnType("nvarchar(2000)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsApproved")
                        .HasColumnType("bit");

                    b.Property<bool>("IsVerifiedPurchase")
                        .HasColumnType("bit");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Rating")
                        .HasColumnType("int");

                    b.Property<string>("Title")
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("UserId", "ProductId")
                        .IsUnique();

                    b.ToTable("Reviews");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.SavedCard", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AuthorizationCode")
                        .HasMaxLength(512)
                        .HasColumnType("nvarchar(512)");

                    b.Property<string>("CardHolderName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("CardNumber")
                        .IsRequired()
                        .HasMaxLength(19)
                        .HasColumnType("nvarchar(19)");

                    b.Property<string>("CardType")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("ExpiryMonth")
                        .HasColumnType("int");

                    b.Property<int>("ExpiryYear")
                        .HasColumnType("int");

                    b.Property<bool>("IsDefault")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("SavedCards");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AvatarUrl")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("GoogleId")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<bool>("IsVerified")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("LastLoginAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<int>("LoyaltyPoints")
                        .HasColumnType("int");

                    b.Property<bool>("NotificationBackInStock")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<bool>("NotificationOrderUpdates")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<bool>("NotificationPromotions")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<bool>("NotificationReviewReminders")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<bool>("NotificationWishlistReminders")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasMaxLength(512)
                        .HasColumnType("nvarchar(512)");

                    b.Property<string>("Phone")
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<DateTime?>("RefreshTokenExpires")
                        .HasColumnType("datetime2");

                    b.Property<string>("RefreshTokenHash")
                        .HasMaxLength(512)
                        .HasColumnType("nvarchar(512)");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.HasIndex("GoogleId")
                        .IsUnique()
                        .HasFilter("[GoogleId] IS NOT NULL");

                    b.HasIndex("Phone")
                        .IsUnique()
                        .HasFilter("[Phone] IS NOT NULL");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Wishlist", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("UserId", "ProductId")
                        .IsUnique();

                    b.ToTable("Wishlists");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Address", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("Addresses")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Cart", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.CartItem", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Cart", "Cart")
                        .WithMany("Items")
                        .HasForeignKey("CartId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.ProductVariant", "Variant")
                        .WithMany()
                        .HasForeignKey("VariantId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Cart");

                    b.Navigation("Product");

                    b.Navigation("Variant");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Category", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Category", "Parent")
                        .WithMany("Children")
                        .HasForeignKey("ParentId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Parent");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Coupon", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Category", null)
                        .WithMany()
                        .HasForeignKey("RestrictToCategoryId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("ShopFresherz.Domain.Entities.Product", null)
                        .WithMany()
                        .HasForeignKey("RestrictToProductId")
                        .OnDelete(DeleteBehavior.Restrict);
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.FlashDeal", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.ProductVariant", null)
                        .WithMany()
                        .HasForeignKey("VariantId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Product");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.LoyaltyTransaction", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Order", null)
                        .WithMany()
                        .HasForeignKey("OrderId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("LoyaltyTransactions")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Notification", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.NotifyRequest", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Product");

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Order", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Coupon", "Coupon")
                        .WithMany()
                        .HasForeignKey("CouponId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("Orders")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Coupon");

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.OrderItem", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Order", "Order")
                        .WithMany("Items")
                        .HasForeignKey("OrderId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany("OrderItems")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.ProductVariant", "Variant")
                        .WithMany()
                        .HasForeignKey("VariantId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Order");

                    b.Navigation("Product");

                    b.Navigation("Variant");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Product", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Brand", "Brand")
                        .WithMany("Products")
                        .HasForeignKey("BrandId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.Category", "Category")
                        .WithMany("Products")
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Brand");

                    b.Navigation("Category");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.ProductImage", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany("Images")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Product");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.ProductVariant", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany("Variants")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Product");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Review", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany("Reviews")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("Reviews")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Product");

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.SavedCard", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("SavedCards")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Wishlist", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("Wishlist")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Product");

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Brand", b =>
                {
                    b.Navigation("Products");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Cart", b =>
                {
                    b.Navigation("Items");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Category", b =>
                {
                    b.Navigation("Children");

                    b.Navigation("Products");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Order", b =>
                {
                    b.Navigation("Items");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Product", b =>
                {
                    b.Navigation("Images");

                    b.Navigation("OrderItems");

                    b.Navigation("Reviews");

                    b.Navigation("Variants");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.User", b =>
                {
                    b.Navigation("Addresses");

                    b.Navigation("LoyaltyTransactions");

                    b.Navigation("Orders");

                    b.Navigation("Reviews");

                    b.Navigation("SavedCards");

                    b.Navigation("Wishlist");
                });
#pragma warning restore 612, 618
        }
    }
}
```

### ShopFresherz.Infrastructure/Persistence/Migrations/20260528101923_FixSlugIdFilteredUniqueIndex.Designer.cs
Summary: Source/configuration file used by the audited backend.
```csharp
// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ShopFresherz.Infrastructure.Persistence;

#nullable disable

namespace ShopFresherz.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ShopFresherzDbContext))]
    [Migration("20260528101923_FixSlugIdFilteredUniqueIndex")]
    partial class FixSlugIdFilteredUniqueIndex
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.14")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Address", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("City")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsDefault")
                        .HasColumnType("bit");

                    b.Property<string>("Label")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("Line1")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("Line2")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("PostalCode")
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<string>("State")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Addresses");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.AppSetting", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ValueJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("Key")
                        .IsUnique();

                    b.ToTable("AppSettings", (string)null);
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.AuditLog", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<string>("Action")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<Guid?>("ActorUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("EntityId")
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<string>("EntityType")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<string>("IpAddress")
                        .HasMaxLength(80)
                        .HasColumnType("nvarchar(80)");

                    b.Property<string>("NewValues")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("OldValues")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("CreatedAt");

                    b.HasIndex("EntityType", "EntityId");

                    b.ToTable("AuditLogs");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Brand", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("LogoUrl")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("Slug")
                        .IsUnique();

                    b.ToTable("Brands");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Cart", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("CouponCode")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("ExpiresAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("SessionId")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId")
                        .IsUnique()
                        .HasFilter("[UserId] IS NOT NULL");

                    b.ToTable("Carts");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.CartItem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("CartId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Quantity")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("VariantId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("VariantId");

                    b.HasIndex("CartId", "ProductId", "VariantId")
                        .IsUnique()
                        .HasFilter("[VariantId] IS NOT NULL");

                    b.ToTable("CartItems");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Category", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ImageUrl")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("MetaDescription")
                        .HasMaxLength(160)
                        .HasColumnType("nvarchar(160)");

                    b.Property<string>("MetaTitle")
                        .HasMaxLength(70)
                        .HasColumnType("nvarchar(70)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<int?>("ParentId")
                        .HasColumnType("int");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("ParentId");

                    b.HasIndex("Slug")
                        .IsUnique();

                    b.ToTable("Categories");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Coupon", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Code")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("ExpiresAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<bool>("IsStackable")
                        .HasColumnType("bit");

                    b.Property<int?>("MaxUses")
                        .HasColumnType("int");

                    b.Property<int?>("MaxUsesPerUser")
                        .HasColumnType("int");

                    b.Property<decimal?>("MinimumOrderAmount")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int?>("RestrictToCategoryId")
                        .HasColumnType("int");

                    b.Property<Guid?>("RestrictToProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<int>("UsedCount")
                        .HasColumnType("int");

                    b.Property<decimal>("Value")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("Id");

                    b.HasIndex("Code")
                        .IsUnique();

                    b.HasIndex("RestrictToCategoryId");

                    b.HasIndex("RestrictToProductId");

                    b.ToTable("Coupons");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.FeaturedSection", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Badge")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("CtaText")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("ImageUrl")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("Price")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Section")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(350)
                        .HasColumnType("nvarchar(350)");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<string>("Tag")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("Section");

                    b.ToTable("FeaturedSections");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.FlashDeal", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("EndsAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<int?>("MaxQuantity")
                        .HasColumnType("int");

                    b.Property<decimal>("OriginalPrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal>("SalePrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("SoldQuantity")
                        .HasColumnType("int");

                    b.Property<DateTime>("StartsAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("VariantId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("VariantId");

                    b.ToTable("FlashDeals");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.HomepageBanner", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("CtaText")
                        .HasMaxLength(80)
                        .HasColumnType("nvarchar(80)");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("EndsAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ImageUrl")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("LinkUrl")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<DateTime?>("StartsAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("SubTitle")
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("IsActive", "SortOrder");

                    b.ToTable("HomepageBanners");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.LoyaltyTransaction", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("EventType")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<DateTime>("ExpiresAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("OrderId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Points")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("OrderId");

                    b.HasIndex("UserId");

                    b.ToTable("LoyaltyTransactions");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Notification", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsRead")
                        .HasColumnType("bit");

                    b.Property<string>("LinkUrl")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Notifications");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.NotifyRequest", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsNotified")
                        .HasColumnType("bit");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("UserId", "ProductId")
                        .IsUnique();

                    b.ToTable("NotifyRequests");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Order", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<int?>("CouponId")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("DeliveryAddressJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("DeliveryFee")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(18,2)")
                        .HasDefaultValue(0m);

                    b.Property<string>("DeliveryMethod")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<decimal>("DiscountAmount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(18,2)")
                        .HasDefaultValue(0m);

                    b.Property<DateTime?>("EstimatedDelivery")
                        .HasColumnType("datetime2");

                    b.Property<string>("GuestEmail")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("Notes")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<string>("OrderNumber")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<string>("PaymentMethod")
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<string>("PaymentReference")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("PaymentStatus")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<decimal>("Subtotal")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("TrackingNumber")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal>("VatAmount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(18,2)")
                        .HasDefaultValue(0m);

                    b.HasKey("Id");

                    b.HasIndex("CouponId");

                    b.HasIndex("OrderNumber")
                        .IsUnique();

                    b.HasIndex("UserId");

                    b.ToTable("Orders");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.OrderItem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("LineTotal")
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid>("OrderId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("ProductSnapshotJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Quantity")
                        .HasColumnType("int");

                    b.Property<decimal>("UnitPrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("VariantId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("OrderId");

                    b.HasIndex("ProductId");

                    b.HasIndex("VariantId");

                    b.ToTable("OrderItems");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Product", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AttributesJson")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("AverageRating")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(3,2)")
                        .HasDefaultValue(0m);

                    b.Property<Guid>("BrandId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("CategoryId")
                        .HasColumnType("int");

                    b.Property<decimal?>("CompareAtPrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal?>("CostPrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ImageUrlsJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<bool>("IsFeatured")
                        .HasColumnType("bit");

                    b.Property<string>("MetaDescription")
                        .HasMaxLength(160)
                        .HasColumnType("nvarchar(160)");

                    b.Property<string>("MetaTitle")
                        .HasMaxLength(70)
                        .HasColumnType("nvarchar(70)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.Property<decimal>("Price")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("ReservedQty")
                        .HasColumnType("int");

                    b.Property<int>("ReviewCount")
                        .HasColumnType("int");

                    b.Property<string>("SKU")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("ShortDescription")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(350)
                        .HasColumnType("nvarchar(350)");

                    b.Property<int>("SoldCount")
                        .HasColumnType("int");

                    b.Property<int>("StockQty")
                        .HasColumnType("int");

                    b.Property<string>("TagsJson")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("ViewCount")
                        .HasColumnType("int");

                    b.Property<decimal?>("WeightKg")
                        .HasColumnType("decimal(8,3)");

                    b.HasKey("Id");

                    b.HasIndex("BrandId");

                    b.HasIndex("CategoryId");

                    b.HasIndex("SKU")
                        .IsUnique();

                    b.HasIndex("Slug")
                        .IsUnique();

                    b.ToTable("Products");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.ProductImage", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("DisplayUrl")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<bool>("IsVideo")
                        .HasColumnType("bit");

                    b.Property<string>("OriginalUrl")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<string>("ThumbUrl")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ZoomUrl")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.ToTable("ProductImages");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.ProductVariant", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AttributesJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("Price")
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("ReservedQty")
                        .HasColumnType("int");

                    b.Property<string>("SKU")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<int>("StockQty")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("SKU")
                        .IsUnique();

                    b.ToTable("ProductVariants");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.PromotionalSection", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Badge")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("ButtonText")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("ContentType")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("CtaText")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<string>("Headline")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("ImageAlt")
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.Property<string>("ImageUrl")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("OriginalPriceText")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("PriceBadge")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("PriceLabel")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("PriceText")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("PriceValue")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<decimal?>("Rating")
                        .HasPrecision(3, 2)
                        .HasColumnType("decimal(3,2)");

                    b.Property<string>("SalePriceText")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("SectionKey")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Slug")
                        .HasMaxLength(350)
                        .HasColumnType("nvarchar(350)");

                    b.Property<string>("SlugId")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<string>("Subtitle")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("Tag")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("Title")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("SectionKey");

                    b.HasIndex("SlugId")
                        .IsUnique()
                        .HasFilter("[DeletedAt] IS NULL");

                    b.ToTable("PromotionalSections");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Review", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Body")
                        .HasMaxLength(2000)
                        .HasColumnType("nvarchar(2000)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsApproved")
                        .HasColumnType("bit");

                    b.Property<bool>("IsVerifiedPurchase")
                        .HasColumnType("bit");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Rating")
                        .HasColumnType("int");

                    b.Property<string>("Title")
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("UserId", "ProductId")
                        .IsUnique();

                    b.ToTable("Reviews");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.SavedCard", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AuthorizationCode")
                        .HasMaxLength(512)
                        .HasColumnType("nvarchar(512)");

                    b.Property<string>("CardHolderName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("CardNumber")
                        .IsRequired()
                        .HasMaxLength(19)
                        .HasColumnType("nvarchar(19)");

                    b.Property<string>("CardType")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("ExpiryMonth")
                        .HasColumnType("int");

                    b.Property<int>("ExpiryYear")
                        .HasColumnType("int");

                    b.Property<bool>("IsDefault")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("SavedCards");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AvatarUrl")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("GoogleId")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<bool>("IsVerified")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("LastLoginAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<int>("LoyaltyPoints")
                        .HasColumnType("int");

                    b.Property<bool>("NotificationBackInStock")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<bool>("NotificationOrderUpdates")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<bool>("NotificationPromotions")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<bool>("NotificationReviewReminders")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<bool>("NotificationWishlistReminders")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasMaxLength(512)
                        .HasColumnType("nvarchar(512)");

                    b.Property<string>("Phone")
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<DateTime?>("RefreshTokenExpires")
                        .HasColumnType("datetime2");

                    b.Property<string>("RefreshTokenHash")
                        .HasMaxLength(512)
                        .HasColumnType("nvarchar(512)");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.HasIndex("GoogleId")
                        .IsUnique()
                        .HasFilter("[GoogleId] IS NOT NULL");

                    b.HasIndex("Phone")
                        .IsUnique()
                        .HasFilter("[Phone] IS NOT NULL");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Wishlist", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("UserId", "ProductId")
                        .IsUnique();

                    b.ToTable("Wishlists");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Address", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("Addresses")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Cart", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.CartItem", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Cart", "Cart")
                        .WithMany("Items")
                        .HasForeignKey("CartId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.ProductVariant", "Variant")
                        .WithMany()
                        .HasForeignKey("VariantId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Cart");

                    b.Navigation("Product");

                    b.Navigation("Variant");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Category", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Category", "Parent")
                        .WithMany("Children")
                        .HasForeignKey("ParentId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Parent");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Coupon", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Category", null)
                        .WithMany()
                        .HasForeignKey("RestrictToCategoryId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("ShopFresherz.Domain.Entities.Product", null)
                        .WithMany()
                        .HasForeignKey("RestrictToProductId")
                        .OnDelete(DeleteBehavior.Restrict);
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.FlashDeal", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.ProductVariant", null)
                        .WithMany()
                        .HasForeignKey("VariantId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Product");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.LoyaltyTransaction", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Order", null)
                        .WithMany()
                        .HasForeignKey("OrderId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("LoyaltyTransactions")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Notification", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.NotifyRequest", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Product");

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Order", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Coupon", "Coupon")
                        .WithMany()
                        .HasForeignKey("CouponId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("Orders")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Coupon");

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.OrderItem", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Order", "Order")
                        .WithMany("Items")
                        .HasForeignKey("OrderId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany("OrderItems")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.ProductVariant", "Variant")
                        .WithMany()
                        .HasForeignKey("VariantId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Order");

                    b.Navigation("Product");

                    b.Navigation("Variant");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Product", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Brand", "Brand")
                        .WithMany("Products")
                        .HasForeignKey("BrandId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.Category", "Category")
                        .WithMany("Products")
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Brand");

                    b.Navigation("Category");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.ProductImage", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany("Images")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Product");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.ProductVariant", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany("Variants")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Product");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Review", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany("Reviews")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("Reviews")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Product");

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.SavedCard", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("SavedCards")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Wishlist", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("Wishlist")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Product");

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Brand", b =>
                {
                    b.Navigation("Products");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Cart", b =>
                {
                    b.Navigation("Items");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Category", b =>
                {
                    b.Navigation("Children");

                    b.Navigation("Products");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Order", b =>
                {
                    b.Navigation("Items");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Product", b =>
                {
                    b.Navigation("Images");

                    b.Navigation("OrderItems");

                    b.Navigation("Reviews");

                    b.Navigation("Variants");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.User", b =>
                {
                    b.Navigation("Addresses");

                    b.Navigation("LoyaltyTransactions");

                    b.Navigation("Orders");

                    b.Navigation("Reviews");

                    b.Navigation("SavedCards");

                    b.Navigation("Wishlist");
                });
#pragma warning restore 612, 618
        }
    }
}
```

### ShopFresherz.Infrastructure/Persistence/Migrations/ShopFresherzDbContextModelSnapshot.cs
Summary: EF Core database context.
```csharp
// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ShopFresherz.Infrastructure.Persistence;

#nullable disable

namespace ShopFresherz.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ShopFresherzDbContext))]
    partial class ShopFresherzDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.14")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Address", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("City")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsDefault")
                        .HasColumnType("bit");

                    b.Property<string>("Label")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("Line1")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("Line2")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("PostalCode")
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<string>("State")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Addresses");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.AppSetting", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ValueJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("Key")
                        .IsUnique();

                    b.ToTable("AppSettings", (string)null);
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.AuditLog", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<string>("Action")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<Guid?>("ActorUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("EntityId")
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<string>("EntityType")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<string>("IpAddress")
                        .HasMaxLength(80)
                        .HasColumnType("nvarchar(80)");

                    b.Property<string>("NewValues")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("OldValues")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("CreatedAt");

                    b.HasIndex("EntityType", "EntityId");

                    b.ToTable("AuditLogs");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Brand", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("LogoUrl")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("Slug")
                        .IsUnique();

                    b.ToTable("Brands");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Cart", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("CouponCode")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("ExpiresAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("SessionId")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId")
                        .IsUnique()
                        .HasFilter("[UserId] IS NOT NULL");

                    b.ToTable("Carts");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.CartItem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("CartId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Quantity")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("VariantId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("VariantId");

                    b.HasIndex("CartId", "ProductId", "VariantId")
                        .IsUnique()
                        .HasFilter("[VariantId] IS NOT NULL");

                    b.ToTable("CartItems");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Category", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ImageUrl")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("MetaDescription")
                        .HasMaxLength(160)
                        .HasColumnType("nvarchar(160)");

                    b.Property<string>("MetaTitle")
                        .HasMaxLength(70)
                        .HasColumnType("nvarchar(70)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<int?>("ParentId")
                        .HasColumnType("int");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("ParentId");

                    b.HasIndex("Slug")
                        .IsUnique();

                    b.ToTable("Categories");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Coupon", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Code")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("ExpiresAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<bool>("IsStackable")
                        .HasColumnType("bit");

                    b.Property<int?>("MaxUses")
                        .HasColumnType("int");

                    b.Property<int?>("MaxUsesPerUser")
                        .HasColumnType("int");

                    b.Property<decimal?>("MinimumOrderAmount")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int?>("RestrictToCategoryId")
                        .HasColumnType("int");

                    b.Property<Guid?>("RestrictToProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<int>("UsedCount")
                        .HasColumnType("int");

                    b.Property<decimal>("Value")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("Id");

                    b.HasIndex("Code")
                        .IsUnique();

                    b.HasIndex("RestrictToCategoryId");

                    b.HasIndex("RestrictToProductId");

                    b.ToTable("Coupons");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.FeaturedSection", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Badge")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("CtaText")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("ImageUrl")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("Price")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Section")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(350)
                        .HasColumnType("nvarchar(350)");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<string>("Tag")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("Section");

                    b.ToTable("FeaturedSections");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.FlashDeal", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("EndsAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<int?>("MaxQuantity")
                        .HasColumnType("int");

                    b.Property<decimal>("OriginalPrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal>("SalePrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("SoldQuantity")
                        .HasColumnType("int");

                    b.Property<DateTime>("StartsAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("VariantId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("VariantId");

                    b.ToTable("FlashDeals");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.HomepageBanner", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("CtaText")
                        .HasMaxLength(80)
                        .HasColumnType("nvarchar(80)");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("EndsAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ImageUrl")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("LinkUrl")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<DateTime?>("StartsAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("SubTitle")
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("IsActive", "SortOrder");

                    b.ToTable("HomepageBanners");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.LoyaltyTransaction", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("EventType")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<DateTime>("ExpiresAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("OrderId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Points")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("OrderId");

                    b.HasIndex("UserId");

                    b.ToTable("LoyaltyTransactions");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Notification", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsRead")
                        .HasColumnType("bit");

                    b.Property<string>("LinkUrl")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Notifications");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.NotifyRequest", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsNotified")
                        .HasColumnType("bit");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("UserId", "ProductId")
                        .IsUnique();

                    b.ToTable("NotifyRequests");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Order", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<int?>("CouponId")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("DeliveryAddressJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("DeliveryFee")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(18,2)")
                        .HasDefaultValue(0m);

                    b.Property<string>("DeliveryMethod")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<decimal>("DiscountAmount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(18,2)")
                        .HasDefaultValue(0m);

                    b.Property<DateTime?>("EstimatedDelivery")
                        .HasColumnType("datetime2");

                    b.Property<string>("GuestEmail")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("Notes")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<string>("OrderNumber")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<string>("PaymentMethod")
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<string>("PaymentReference")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("PaymentStatus")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<decimal>("Subtotal")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("TrackingNumber")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal>("VatAmount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(18,2)")
                        .HasDefaultValue(0m);

                    b.HasKey("Id");

                    b.HasIndex("CouponId");

                    b.HasIndex("OrderNumber")
                        .IsUnique();

                    b.HasIndex("UserId");

                    b.ToTable("Orders");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.OrderItem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("LineTotal")
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid>("OrderId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("ProductSnapshotJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Quantity")
                        .HasColumnType("int");

                    b.Property<decimal>("UnitPrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("VariantId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("OrderId");

                    b.HasIndex("ProductId");

                    b.HasIndex("VariantId");

                    b.ToTable("OrderItems");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Product", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AttributesJson")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("AverageRating")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(3,2)")
                        .HasDefaultValue(0m);

                    b.Property<Guid>("BrandId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("CategoryId")
                        .HasColumnType("int");

                    b.Property<decimal?>("CompareAtPrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal?>("CostPrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ImageUrlsJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<bool>("IsFeatured")
                        .HasColumnType("bit");

                    b.Property<string>("MetaDescription")
                        .HasMaxLength(160)
                        .HasColumnType("nvarchar(160)");

                    b.Property<string>("MetaTitle")
                        .HasMaxLength(70)
                        .HasColumnType("nvarchar(70)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.Property<decimal>("Price")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("ReservedQty")
                        .HasColumnType("int");

                    b.Property<int>("ReviewCount")
                        .HasColumnType("int");

                    b.Property<string>("SKU")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("ShortDescription")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(350)
                        .HasColumnType("nvarchar(350)");

                    b.Property<int>("SoldCount")
                        .HasColumnType("int");

                    b.Property<int>("StockQty")
                        .HasColumnType("int");

                    b.Property<string>("TagsJson")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("ViewCount")
                        .HasColumnType("int");

                    b.Property<decimal?>("WeightKg")
                        .HasColumnType("decimal(8,3)");

                    b.HasKey("Id");

                    b.HasIndex("BrandId");

                    b.HasIndex("CategoryId");

                    b.HasIndex("SKU")
                        .IsUnique();

                    b.HasIndex("Slug")
                        .IsUnique();

                    b.ToTable("Products");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.ProductImage", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("DisplayUrl")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<bool>("IsVideo")
                        .HasColumnType("bit");

                    b.Property<string>("OriginalUrl")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<string>("ThumbUrl")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ZoomUrl")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.ToTable("ProductImages");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.ProductVariant", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AttributesJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("Price")
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("ReservedQty")
                        .HasColumnType("int");

                    b.Property<string>("SKU")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<int>("StockQty")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("SKU")
                        .IsUnique();

                    b.ToTable("ProductVariants");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.PromotionalSection", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Badge")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("ButtonText")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("ContentType")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("CtaText")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<string>("Headline")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("ImageAlt")
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.Property<string>("ImageUrl")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("OriginalPriceText")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("PriceBadge")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("PriceLabel")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("PriceText")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("PriceValue")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<decimal?>("Rating")
                        .HasPrecision(3, 2)
                        .HasColumnType("decimal(3,2)");

                    b.Property<string>("SalePriceText")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("SectionKey")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Slug")
                        .HasMaxLength(350)
                        .HasColumnType("nvarchar(350)");

                    b.Property<string>("SlugId")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<string>("Subtitle")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("Tag")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("Title")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("SectionKey");

                    b.HasIndex("SlugId")
                        .IsUnique()
                        .HasFilter("[DeletedAt] IS NULL");

                    b.ToTable("PromotionalSections");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Review", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Body")
                        .HasMaxLength(2000)
                        .HasColumnType("nvarchar(2000)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsApproved")
                        .HasColumnType("bit");

                    b.Property<bool>("IsVerifiedPurchase")
                        .HasColumnType("bit");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Rating")
                        .HasColumnType("int");

                    b.Property<string>("Title")
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("UserId", "ProductId")
                        .IsUnique();

                    b.ToTable("Reviews");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.SavedCard", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AuthorizationCode")
                        .HasMaxLength(512)
                        .HasColumnType("nvarchar(512)");

                    b.Property<string>("CardHolderName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("CardNumber")
                        .IsRequired()
                        .HasMaxLength(19)
                        .HasColumnType("nvarchar(19)");

                    b.Property<string>("CardType")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("ExpiryMonth")
                        .HasColumnType("int");

                    b.Property<int>("ExpiryYear")
                        .HasColumnType("int");

                    b.Property<bool>("IsDefault")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("SavedCards");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AvatarUrl")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("GoogleId")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<bool>("IsVerified")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("LastLoginAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<int>("LoyaltyPoints")
                        .HasColumnType("int");

                    b.Property<bool>("NotificationBackInStock")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<bool>("NotificationOrderUpdates")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<bool>("NotificationPromotions")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<bool>("NotificationReviewReminders")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<bool>("NotificationWishlistReminders")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasMaxLength(512)
                        .HasColumnType("nvarchar(512)");

                    b.Property<string>("Phone")
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<DateTime?>("RefreshTokenExpires")
                        .HasColumnType("datetime2");

                    b.Property<string>("RefreshTokenHash")
                        .HasMaxLength(512)
                        .HasColumnType("nvarchar(512)");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.HasIndex("GoogleId")
                        .IsUnique()
                        .HasFilter("[GoogleId] IS NOT NULL");

                    b.HasIndex("Phone")
                        .IsUnique()
                        .HasFilter("[Phone] IS NOT NULL");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Wishlist", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("UserId", "ProductId")
                        .IsUnique();

                    b.ToTable("Wishlists");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Address", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("Addresses")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Cart", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.CartItem", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Cart", "Cart")
                        .WithMany("Items")
                        .HasForeignKey("CartId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.ProductVariant", "Variant")
                        .WithMany()
                        .HasForeignKey("VariantId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Cart");

                    b.Navigation("Product");

                    b.Navigation("Variant");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Category", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Category", "Parent")
                        .WithMany("Children")
                        .HasForeignKey("ParentId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Parent");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Coupon", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Category", null)
                        .WithMany()
                        .HasForeignKey("RestrictToCategoryId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("ShopFresherz.Domain.Entities.Product", null)
                        .WithMany()
                        .HasForeignKey("RestrictToProductId")
                        .OnDelete(DeleteBehavior.Restrict);
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.FlashDeal", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.ProductVariant", null)
                        .WithMany()
                        .HasForeignKey("VariantId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Product");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.LoyaltyTransaction", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Order", null)
                        .WithMany()
                        .HasForeignKey("OrderId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("LoyaltyTransactions")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Notification", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.NotifyRequest", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Product");

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Order", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Coupon", "Coupon")
                        .WithMany()
                        .HasForeignKey("CouponId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("Orders")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Coupon");

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.OrderItem", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Order", "Order")
                        .WithMany("Items")
                        .HasForeignKey("OrderId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany("OrderItems")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.ProductVariant", "Variant")
                        .WithMany()
                        .HasForeignKey("VariantId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Order");

                    b.Navigation("Product");

                    b.Navigation("Variant");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Product", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Brand", "Brand")
                        .WithMany("Products")
                        .HasForeignKey("BrandId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.Category", "Category")
                        .WithMany("Products")
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Brand");

                    b.Navigation("Category");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.ProductImage", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany("Images")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Product");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.ProductVariant", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany("Variants")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Product");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Review", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany("Reviews")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("Reviews")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Product");

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.SavedCard", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("SavedCards")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Wishlist", b =>
                {
                    b.HasOne("ShopFresherz.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ShopFresherz.Domain.Entities.User", "User")
                        .WithMany("Wishlist")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Product");

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Brand", b =>
                {
                    b.Navigation("Products");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Cart", b =>
                {
                    b.Navigation("Items");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Category", b =>
                {
                    b.Navigation("Children");

                    b.Navigation("Products");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Order", b =>
                {
                    b.Navigation("Items");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.Product", b =>
                {
                    b.Navigation("Images");

                    b.Navigation("OrderItems");

                    b.Navigation("Reviews");

                    b.Navigation("Variants");
                });

            modelBuilder.Entity("ShopFresherz.Domain.Entities.User", b =>
                {
                    b.Navigation("Addresses");

                    b.Navigation("LoyaltyTransactions");

                    b.Navigation("Orders");

                    b.Navigation("Reviews");

                    b.Navigation("SavedCards");

                    b.Navigation("Wishlist");
                });
#pragma warning restore 612, 618
        }
    }
}
```

### ShopFresherz.Infrastructure/Services/ImageProcessingService.cs
Summary: Service implementation or abstraction for the related infrastructure capability.
```csharp
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using ShopFresherz.Domain.Interfaces.Services;

namespace ShopFresherz.Infrastructure.Services;

/// <summary>ImageSharp implementation of the product image zoom pipeline.</summary>
public sealed class ImageProcessingService : IImageProcessingService
{
    private const int MinDimension = 800;
    private const long MaxSizeBytes = 8 * 1024 * 1024;

    private static readonly (string Key, int Width, int Height, int Quality)[] Sizes =
    {
        ("thumb", 80, 80, 85),
        ("display", 540, 540, 90),
        ("zoom", 1600, 1600, 92),
    };

    private readonly IFileStorageService _storage;
    private readonly ILogger<ImageProcessingService> _logger;

    public ImageProcessingService(IFileStorageService storage, ILogger<ImageProcessingService> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> ValidateAsync(Stream stream)
    {
        if (stream.CanSeek && stream.Length > MaxSizeBytes)
        {
            return false;
        }

        if (stream.CanSeek) stream.Position = 0;
        ImageInfo? info = await Image.IdentifyAsync(stream);
        if (stream.CanSeek) stream.Position = 0;

        return info is not null &&
               info.Width >= MinDimension &&
               info.Height >= MinDimension;
    }

    /// <inheritdoc />
    public async Task<ImageDerivatives> GenerateDerivativesAsync(
        Stream sourceStream,
        CancellationToken cancellationToken = default)
    {
        if (sourceStream.CanSeek) sourceStream.Position = 0;
        using Image source = await Image.LoadAsync(sourceStream, cancellationToken);

        if (source.Width < MinDimension || source.Height < MinDimension)
        {
            throw new InvalidOperationException(
                $"Image must be at least 800x800px. Uploaded: {source.Width}x{source.Height}px.");
        }

        MemoryStream thumb = await CreateDerivativeAsync(source, 80, 80, 85, cancellationToken);
        MemoryStream display = await CreateDerivativeAsync(source, 540, 540, 90, cancellationToken);
        MemoryStream zoom = await CreateDerivativeAsync(source, 1600, 1600, 92, cancellationToken);

        MemoryStream original = new();
        await source.SaveAsWebpAsync(original, new WebpEncoder { Quality = 95 }, cancellationToken);
        original.Position = 0;

        return new ImageDerivatives(thumb, display, zoom, original);
    }

    /// <inheritdoc />
    public async Task<ProductImageUrls> ProcessAndUploadAsync(
        Stream imageStream,
        string originalFileName,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        if (imageStream.CanSeek && imageStream.Length > MaxSizeBytes)
        {
            throw new InvalidOperationException("Image file size must be 8MB or less.");
        }

        if (imageStream.CanSeek) imageStream.Position = 0;
        using Image sourceImage = await Image.LoadAsync(imageStream, cancellationToken);

        if (sourceImage.Width < MinDimension || sourceImage.Height < MinDimension)
        {
            throw new InvalidOperationException(
                $"Image must be at least 800x800px. Uploaded: {sourceImage.Width}x{sourceImage.Height}px.");
        }

        string baseKey = $"products/{productId}/images/{Guid.NewGuid()}";
        Dictionary<string, string> urls = new();

        foreach ((string key, int width, int height, int quality) in Sizes)
        {
            using Image resized = sourceImage.Clone(ctx =>
                ctx.Resize(new ResizeOptions
                {
                    Size = new Size(width, height),
                    Mode = ResizeMode.Crop,
                    Position = AnchorPositionMode.Center,
                }));

            using MemoryStream ms = new();
            await resized.SaveAsWebpAsync(ms, new WebpEncoder { Quality = quality }, cancellationToken);
            ms.Position = 0;

            string blobKey = $"{baseKey}/{key}.webp";
            urls[key] = await _storage.UploadAsync(ms, blobKey, "image/webp", cancellationToken);
        }

        using MemoryStream originalMs = new();
        await sourceImage.SaveAsWebpAsync(originalMs, new WebpEncoder { Quality = 95 }, cancellationToken);
        originalMs.Position = 0;

        urls["original"] = await _storage.UploadAsync(
            originalMs,
            $"{baseKey}/original.webp",
            "image/webp",
            cancellationToken);

        _logger.LogInformation(
            "Processed product image {FileName} for product {ProductId}.",
            originalFileName,
            productId);

        return new ProductImageUrls(
            ThumbUrl: urls["thumb"],
            DisplayUrl: urls["display"],
            ZoomUrl: urls["zoom"],
            OriginalUrl: urls["original"]);
    }

    private static async Task<MemoryStream> CreateDerivativeAsync(
        Image source,
        int width,
        int height,
        int quality,
        CancellationToken cancellationToken)
    {
        using Image resized = source.Clone(ctx =>
            ctx.Resize(new ResizeOptions
            {
                Size = new Size(width, height),
                Mode = ResizeMode.Crop,
                Position = AnchorPositionMode.Center,
            }));

        MemoryStream output = new();
        await resized.SaveAsWebpAsync(output, new WebpEncoder { Quality = quality }, cancellationToken);
        output.Position = 0;
        return output;
    }
}
```

### ShopFresherz.Infrastructure/Services/ImageSharpProcessingService.cs
Summary: Service implementation or abstraction for the related infrastructure capability.
```csharp
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using ShopFresherz.Domain.Interfaces.Services;

namespace ShopFresherz.Infrastructure.Services;

/// <summary>
/// Server-side image processing using SixLabors.ImageSharp.
/// Validates min 2000Ã—2000 px, max 8 MB, then produces four WebP derivatives:
/// Thumb (80px), Display (540px), Zoom (1600px), and lossless Original.
/// </summary>
public sealed class ImageSharpProcessingService : IImageProcessingService
{
    private const int MinDimension  = 2000;
    private const long MaxSizeBytes = 8 * 1024 * 1024; // 8 MB

    private static readonly WebpEncoder HighQualityEncoder = new()
    {
        Quality    = 85,
        FileFormat = WebpFileFormatType.Lossy,
    };

    private static readonly WebpEncoder LosslessEncoder = new()
    {
        FileFormat = WebpFileFormatType.Lossless,
    };

    /// <inheritdoc />
    public async Task<bool> ValidateAsync(Stream stream)
    {
        if (stream.Length > MaxSizeBytes)
        {
            return false;
        }

        // Reset to allow re-reading for image info.
        stream.Position = 0;

        ImageInfo? info = await Image.IdentifyAsync(stream);

        if (info == null)
        {
            return false;
        }

        stream.Position = 0;
        return info.Width >= MinDimension && info.Height >= MinDimension;
    }

    /// <inheritdoc />
    public async Task<ImageDerivatives> GenerateDerivativesAsync(Stream sourceStream, CancellationToken cancellationToken = default)
    {
        sourceStream.Position = 0;

        using Image source = await Image.LoadAsync(sourceStream, cancellationToken);

        MemoryStream thumb80   = new();
        MemoryStream display540 = new();
        MemoryStream zoom1600  = new();
        MemoryStream original  = new();

        // Thumb â€” 80px longest edge, lossy WebP
        using (Image thumbImage = source.Clone(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(80, 80),
            Mode = ResizeMode.Max,
        })))
        {
            await thumbImage.SaveAsync(thumb80, HighQualityEncoder, cancellationToken);
        }

        // Display â€” 540px longest edge, lossy WebP
        using (Image displayImage = source.Clone(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(540, 540),
            Mode = ResizeMode.Max,
        })))
        {
            await displayImage.SaveAsync(display540, HighQualityEncoder, cancellationToken);
        }

        // Zoom â€” 1600px longest edge, lossy WebP
        using (Image zoomImage = source.Clone(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(1600, 1600),
            Mode = ResizeMode.Max,
        })))
        {
            await zoomImage.SaveAsync(zoom1600, HighQualityEncoder, cancellationToken);
        }

        // Original â€” lossless WebP (re-encoded to strip metadata)
        await source.SaveAsync(original, LosslessEncoder, cancellationToken);

        // Rewind all streams for consumption by the caller.
        thumb80.Position    = 0;
        display540.Position = 0;
        zoom1600.Position   = 0;
        original.Position   = 0;

        return new ImageDerivatives(thumb80, display540, zoom1600, original);
    }

    /// <inheritdoc />
    public Task<ProductImageUrls> ProcessAndUploadAsync(
        Stream imageStream,
        string originalFileName,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "Use ImageProcessingService for the upload-backed product image pipeline.");
    }
}
```

Cloudinary section in ShopFresherz.API/appsettings.json:
NOT FOUND: Cloudinary section - may not have been implemented yet

Cloudinary section in ShopFresherz.API/appsettings.Development.json:
NOT FOUND: Cloudinary section - may not have been implemented yet

Cloudinary section in ShopFresherz.API/appsettings.Production.json:
NOT FOUND: Cloudinary section - may not have been implemented yet

## Report 2 - Product Endpoints (Admin + Public)
NOT FOUND: AdminProductsController.cs - may not have been implemented yet

### ShopFresherz.API/Controllers/ProductsController.cs
Summary: ASP.NET Core controller exposing the related HTTP endpoints.
```csharp
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Product;
using ShopFresherz.Application.Features.Products.Commands;
using ShopFresherz.Application.Features.Products.Queries;

namespace ShopFresherz.API.Controllers;

/// <summary>Handles product catalog browsing and admin CRUD operations.</summary>
[ApiController]
[Route("api/v1/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>Initialises a new instance of <see cref="ProductsController"/>.</summary>
    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Returns a paginated product listing with optional filters.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int page       = 1,
        [FromQuery] int pageSize   = 20,
        [FromQuery] int? categoryId  = null,
        [FromQuery] Guid? brandId    = null,
        [FromQuery] decimal? priceMin  = null,
        [FromQuery] decimal? priceMax  = null,
        [FromQuery] decimal? ratingMin = null,
        [FromQuery] string? sortBy     = null,
        CancellationToken cancellationToken = default)
    {
        Result<PagedResult<ProductSummaryDto>> result =
            await _mediator.Send(
                new GetProductsQuery(page, pageSize, categoryId, brandId, priceMin, priceMax, ratingMin, sortBy),
                cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Returns full product detail for the product detail page (PDP).</summary>
    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlug(
        [FromRoute] string slug,
        CancellationToken cancellationToken)
    {
        Result<ProductDetailDto> result =
            await _mediator.Send(new GetProductBySlugQuery(slug), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Creates a new product. Requires Admin or SuperAdmin role.</summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        Result<Guid> result =
            await _mediator.Send(new CreateProductCommand(request), cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetBySlug), new { slug = string.Empty }, new { id = result.Value })
            : MapError(result.Error);
    }

    /// <summary>Updates an existing product. Requires Admin or SuperAdmin role.</summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        Result<bool> result =
            await _mediator.Send(new UpdateProductCommand(id, request), cancellationToken);

        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    /// <summary>Soft-deletes a product. Requires Admin or SuperAdmin role.</summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        Result<bool> result =
            await _mediator.Send(new DeleteProductCommand(id), cancellationToken);

        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    /// <summary>
    /// Uploads a product image, generates 4 WebP zoom derivatives
    /// (80px thumb, 540px display, 1600px zoom, original),
    /// and stores all URLs. Requires Admin role.
    /// Max file size: 8MB. Min dimensions: 800x800px.
    /// </summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpPost("{id:guid}/images")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ProductImageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadImage(
        [FromRoute] Guid id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { Code = "VALIDATION", Message = "No file uploaded." });
        }

        using Stream stream = file.OpenReadStream();

        Result<ProductImageDto> result = await _mediator.Send(
            new UploadProductImageCommand(id, stream, file.FileName, file.Length),
            cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetBySlug), new { slug = string.Empty }, result.Value)
            : MapError(result.Error);
    }

    private IActionResult MapError(Error error) => error.Code switch
    {
        "NOT_FOUND"  => NotFound(new { error.Code, error.Message }),
        "CONFLICT"   => Conflict(new { error.Code, error.Message }),
        "VALIDATION" => BadRequest(new { error.Code, error.Message }),
        _            => StatusCode(StatusCodes.Status500InternalServerError, new { error.Code, error.Message }),
    };
}
```

### ShopFresherz.Application/Features/Products/Commands/CreateProductCommand.cs
Summary: MediatR command, handler, and validation logic for the related workflow.
```csharp
using FluentValidation;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Product;
using ShopFresherz.Application.Features.Search.Commands;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;
using ShopFresherz.Domain.Interfaces.Services;

namespace ShopFresherz.Application.Features.Products.Commands;

/// <summary>Command for creating a new product (admin only).</summary>
/// <param name="Request">The product creation payload.</param>
public sealed record CreateProductCommand(CreateProductRequest Request) : IRequest<Result<Guid>>;

/// <summary>Handler for <see cref="CreateProductCommand"/>.</summary>
public sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    private readonly IUnitOfWork _uow;
    private readonly ISearchService _search;

    /// <summary>Initialises the handler.</summary>
    public CreateProductCommandHandler(IUnitOfWork uow, ISearchService search)
    {
        _uow = uow;
        _search = search;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        CreateProductRequest req = command.Request;

        string slug = string.IsNullOrWhiteSpace(req.Slug)
            ? GenerateSlug(req.Name)
            : req.Slug.Trim().ToLowerInvariant();

        if (await _uow.Products.SlugExistsAsync(slug, cancellationToken))
        {
            return Error.Conflict($"A product with slug '{slug}' already exists.");
        }

        Product product = new()
        {
            SKU = req.SKU.Trim().ToUpperInvariant(),
            Name = req.Name.Trim(),
            Slug = slug,
            BrandId = req.BrandId,
            CategoryId = req.CategoryId,
            Description = req.Description,
            ShortDescription = req.ShortDescription,
            Price = req.Price,
            CompareAtPrice = req.CompareAtPrice,
            CostPrice = req.CostPrice,
            StockQty = req.StockQty,
            WeightKg = req.WeightKg,
            AttributesJson = req.AttributesJson,
            TagsJson = req.TagsJson,
            IsActive = req.IsActive,
            IsFeatured = req.IsFeatured,
            // CreateProductRequest currently does not expose MetaTitle/MetaDescription in this codebase.
            // Persist meta SEO fields only if/when the DTO supports them.

            ImageUrls = req.ImageUrls, // Set the image URLs
        };

        await _uow.Products.AddAsync(product, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        ProductSearchDocument doc = IndexProductCommandHandler.Map(product);
        _ = Task.Run(() => _search.IndexProductAsync(doc, CancellationToken.None), CancellationToken.None);

        return Result<Guid>.Success(product.Id);
    }

    private static string GenerateSlug(string name)
    {
        string lower = name.Trim().ToLowerInvariant();
        System.Text.StringBuilder sb = new();
        foreach (char c in lower)
        {
            if (char.IsLetterOrDigit(c)) sb.Append(c);
            else if (c == ' ') sb.Append('-');
        }
        return sb.ToString().Trim('-');
    }
}

/// <summary>Validator for <see cref="CreateProductCommand"/>.</summary>
public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Request.SKU).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Request.BrandId).NotEmpty();
        RuleFor(x => x.Request.CategoryId).GreaterThan(0);
        RuleFor(x => x.Request.Price).GreaterThan(0);
        RuleFor(x => x.Request.StockQty).GreaterThanOrEqualTo(0);

        // imageUrls: Array of Strings (URLs)
        RuleFor(x => x.Request.ImageUrls)
            .NotNull();

        // Allow empty array, but disallow invalid URL entries.
        RuleForEach(x => x.Request.ImageUrls)
            .NotEmpty()
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Each imageUrl must be a valid absolute URL.");
    }
}
```

### ShopFresherz.Application/Features/Products/Commands/UpdateProductCommand.cs
Summary: MediatR command, handler, and validation logic for the related workflow.
```csharp
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Product;
using ShopFresherz.Application.Features.Search.Commands;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;
using ShopFresherz.Domain.Interfaces.Services;

namespace ShopFresherz.Application.Features.Products.Commands;

/// <summary>Command for updating an existing product (admin only).</summary>
/// <param name="Id">The product ID to update.</param>
/// <param name="Request">The partial update payload.</param>
public sealed record UpdateProductCommand(Guid Id, UpdateProductRequest Request) : IRequest<Result<bool>>;

/// <summary>Handler for <see cref="UpdateProductCommand"/>.</summary>
public sealed class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;
    private readonly ISearchService _search;

    /// <summary>Initialises the handler.</summary>
    public UpdateProductCommandHandler(IUnitOfWork uow, ISearchService search)
    {
        _uow = uow;
        _search = search;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        UpdateProductCommand command,
        CancellationToken cancellationToken)
    {
        Product? product = await _uow.Products.GetByIdAsync(command.Id, cancellationToken);
        if (product is null)
        {
            return Error.NotFound("Product");
        }

        UpdateProductRequest req = command.Request;

        if (req.Name         is not null) product.Name             = req.Name.Trim();
        if (req.BrandId      is not null) product.BrandId          = req.BrandId.Value;
        if (req.CategoryId   is not null) product.CategoryId       = req.CategoryId.Value;
        if (req.Description  is not null) product.Description      = req.Description;
        if (req.ShortDescription is not null) product.ShortDescription = req.ShortDescription;
        if (req.Price        is not null) product.Price            = req.Price.Value;
        if (req.CompareAtPrice is not null) product.CompareAtPrice = req.CompareAtPrice;
        if (req.CostPrice    is not null) product.CostPrice        = req.CostPrice;
        if (req.StockQty     is not null) product.StockQty         = req.StockQty.Value;
        if (req.WeightKg     is not null) product.WeightKg         = req.WeightKg;
        if (req.AttributesJson is not null) product.AttributesJson = req.AttributesJson;
        if (req.TagsJson     is not null) product.TagsJson         = req.TagsJson;
        if (req.IsActive     is not null) product.IsActive         = req.IsActive.Value;
        if (req.IsFeatured   is not null) product.IsFeatured       = req.IsFeatured.Value;
        if (req.MetaTitle    is not null) product.MetaTitle        = req.MetaTitle;
        if (req.MetaDescription is not null) product.MetaDescription = req.MetaDescription;

        if (req.Slug is not null)
        {
            string newSlug = req.Slug.Trim().ToLowerInvariant();
            if (!string.Equals(newSlug, product.Slug, StringComparison.Ordinal)
                && await _uow.Products.SlugExistsAsync(newSlug, cancellationToken))
            {
                return Error.Conflict($"Slug '{newSlug}' is already taken.");
            }
            product.Slug = newSlug;
        }

        _uow.Products.Update(product);
        await _uow.SaveChangesAsync(cancellationToken);

        ProductSearchDocument doc = IndexProductCommandHandler.Map(product);
        _ = Task.Run(() => _search.IndexProductAsync(doc, CancellationToken.None), CancellationToken.None);

        return Result<bool>.Success(true);
    }
}
```

### ShopFresherz.Application/Features/Products/Commands/UploadProductImageCommand.cs
Summary: MediatR command, handler, and validation logic for the related workflow.
```csharp
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Product;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;
using ShopFresherz.Domain.Interfaces.Services;

namespace ShopFresherz.Application.Features.Products.Commands;

/// <summary>Command for uploading and processing a product image.</summary>
public sealed record UploadProductImageCommand(
    Guid ProductId,
    Stream ImageStream,
    string FileName,
    long FileSizeBytes) : IRequest<Result<ProductImageDto>>;

/// <summary>Handler for <see cref="UploadProductImageCommand"/>.</summary>
public sealed class UploadProductImageCommandHandler
    : IRequestHandler<UploadProductImageCommand, Result<ProductImageDto>>
{
    private const long MaxFileSizeBytes = 8 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
    };

    private readonly IUnitOfWork _uow;
    private readonly IImageProcessingService _imageProcessing;

    public UploadProductImageCommandHandler(IUnitOfWork uow, IImageProcessingService imageProcessing)
    {
        _uow = uow;
        _imageProcessing = imageProcessing;
    }

    /// <inheritdoc />
    public async Task<Result<ProductImageDto>> Handle(
        UploadProductImageCommand command,
        CancellationToken cancellationToken)
    {
        if (command.FileSizeBytes > MaxFileSizeBytes)
        {
            return Error.Validation("Image file size must be 8MB or less.");
        }

        string extension = Path.GetExtension(command.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            return Error.Validation("Image must be a JPG, PNG, or WebP file.");
        }

        Product? product = await _uow.Products.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            return Error.NotFound("Product");
        }

        ProductImageUrls urls;
        try
        {
            urls = await _imageProcessing.ProcessAndUploadAsync(
                command.ImageStream,
                command.FileName,
                command.ProductId,
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return Error.Validation(ex.Message);
        }

        ProductImage image = new()
        {
            ProductId = product.Id,
            ThumbUrl = urls.ThumbUrl,
            DisplayUrl = urls.DisplayUrl,
            ZoomUrl = urls.ZoomUrl,
            OriginalUrl = urls.OriginalUrl,
            SortOrder = product.Images.Count,
            IsVideo = false,
        };

        product.Images.Add(image);
        _uow.Products.Update(product);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<ProductImageDto>.Success(new ProductImageDto
        {
            Id = image.Id,
            ThumbUrl = image.ThumbUrl,
            DisplayUrl = image.DisplayUrl,
            ZoomUrl = image.ZoomUrl,
            OriginalUrl = image.OriginalUrl,
            SortOrder = image.SortOrder,
            IsVideo = image.IsVideo,
        });
    }
}
```

### ShopFresherz.Application/Features/Products/Queries/GetProductBySlugQuery.cs
Summary: MediatR query and handler for read-side API data.
```csharp
using AutoMapper;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Product;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Products.Queries;

/// <summary>Query for a single product detail page by URL slug.</summary>
/// <param name="Slug">The product's URL slug.</param>
public sealed record GetProductBySlugQuery(string Slug) : IRequest<Result<ProductDetailDto>>;

/// <summary>Handler for <see cref="GetProductBySlugQuery"/>.</summary>
public sealed class GetProductBySlugQueryHandler
    : IRequestHandler<GetProductBySlugQuery, Result<ProductDetailDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    /// <summary>Initialises the handler.</summary>
    public GetProductBySlugQueryHandler(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<Result<ProductDetailDto>> Handle(
        GetProductBySlugQuery query,
        CancellationToken cancellationToken)
    {
        Product? product = await _uow.Products.GetBySlugAsync(query.Slug, cancellationToken);
        if (product is null)
        {
            return Error.NotFound("Product");
        }

        product.ViewCount++;
        _uow.Products.Update(product);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<ProductDetailDto>.Success(_mapper.Map<ProductDetailDto>(product));
    }
}
```

### ShopFresherz.Application/Features/Products/Queries/GetProductsQuery.cs
Summary: MediatR query and handler for read-side API data.
```csharp
using AutoMapper;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Product;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Products.Queries;

/// <summary>Query for paginated product listing with optional filters.</summary>
public sealed record GetProductsQuery(
    int Page        = 1,
    int PageSize    = 20,
    int? CategoryId = null,
    Guid? BrandId   = null,
    decimal? PriceMin  = null,
    decimal? PriceMax  = null,
    decimal? RatingMin = null,
    string? SortBy     = null
) : IRequest<Result<PagedResult<ProductSummaryDto>>>;

/// <summary>Handler for <see cref="GetProductsQuery"/>.</summary>
public sealed class GetProductsQueryHandler
    : IRequestHandler<GetProductsQuery, Result<PagedResult<ProductSummaryDto>>>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    /// <summary>Initialises the handler.</summary>
    public GetProductsQueryHandler(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<ProductSummaryDto>>> Handle(
        GetProductsQuery query,
        CancellationToken cancellationToken)
    {
        int page     = Math.Max(1, query.Page);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);

        (IReadOnlyList<Product> items, int total) = await _uow.Products.GetPagedAsync(
            page,
            pageSize,
            query.CategoryId,
            query.BrandId,
            query.PriceMin,
            query.PriceMax,
            query.RatingMin,
            query.SortBy,
            cancellationToken);

        IReadOnlyList<ProductSummaryDto> dtos =
            _mapper.Map<IReadOnlyList<ProductSummaryDto>>(items);

        return Result<PagedResult<ProductSummaryDto>>.Success(
            new PagedResult<ProductSummaryDto>(dtos, total, page, pageSize));
    }
}
```

### ShopFresherz.Application/Dtos/Product/ProductDtos.cs
Summary: DTO/request/response model definitions for the related API contract.
```csharp
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

    /// <summary>Gets or sets the 80Ã—80 thumbnail URL (LQIP).</summary>
    public string ThumbUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the 540Ã—540 display image URL (WebP).</summary>
    public string DisplayUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the 1600Ã—1600 zoom source URL (WebP).</summary>
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

    /// <summary>Gets or sets the available quantity (StockQty âˆ’ ReservedQty).</summary>
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

    /// <summary>Gets or sets a value indicating whether the product is active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the product is featured.</summary>
    public bool IsFeatured { get; set; } = false;

    /// <summary>Gets or sets the image URLs (array of strings).</summary>
    public List<string> ImageUrls { get; set; } = new List<string>();
}

/// <summary>Request payload for updating an existing product (admin). All fields optional.</summary>
public sealed class UpdateProductRequest
{
    /// <summary>Gets or sets the updated product name.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the updated URL slug.</summary>
    public string? Slug { get; set; }

    /// <summary>Gets or sets the updated brand ID.</summary>
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

    /// <summary>Gets or sets the updated active flag.</summary>
    public bool? IsActive { get; set; }

    /// <summary>Gets or sets the updated featured flag.</summary>
    public bool? IsFeatured { get; set; }

    /// <summary>Gets or sets the updated SEO meta title.</summary>
    public string? MetaTitle { get; set; }

    /// <summary>Gets or sets the updated SEO meta description.</summary>
    public string? MetaDescription { get; set; }
}
```

### Product JSON Shape Notes
Derived from controller attributes and DTO records/classes in `ProductDtos.cs`. ASP.NET Core default JSON serialization is camelCase unless overridden in Program.cs.

## Report 3 - Payment Integration (Current State)
### ShopFresherz.Domain/Interfaces/Services/IPaymentService.cs
Summary: Service implementation or abstraction for the related infrastructure capability.
```csharp
namespace ShopFresherz.Domain.Interfaces.Services;

/// <summary>
/// Contract for payment gateway integration.
/// Primary: Paystack. Fallback: Flutterwave.
/// Card data is never stored â€” tokenisation only (PCI DSS compliant).
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Initialises a payment transaction and returns the hosted checkout URL and reference.
    /// </summary>
    /// <param name="email">Customer email for the gateway.</param>
    /// <param name="amountKobo">Amount in kobo (NGN Ã— 100).</param>
    /// <param name="reference">Unique order reference.</param>
    /// <param name="callbackUrl">URL for the gateway to redirect after payment.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The authorisation URL and the transaction reference.</returns>
    Task<PaymentInitResult> InitialiseAsync(
        string email,
        long amountKobo,
        string reference,
        string callbackUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a transaction status directly with the gateway (double-check after webhook).
    /// </summary>
    Task<PaymentVerifyResult> VerifyAsync(string reference, CancellationToken cancellationToken = default);

    /// <summary>Initiates a full or partial refund for a confirmed transaction.</summary>
    Task<bool> RefundAsync(string reference, long amountKobo, CancellationToken cancellationToken = default);

    /// <summary>Validates the HMAC-SHA512 webhook signature.</summary>
    bool VerifyWebhookSignature(string rawBody, string signature);
}

/// <summary>Result from payment initialisation.</summary>
public sealed record PaymentInitResult(string AuthorisationUrl, string Reference);

/// <summary>Result from payment verification.</summary>
public sealed record PaymentVerifyResult(bool IsSuccessful, string Status, long AmountKobo, string Reference);
```

### ShopFresherz.Domain/Interfaces/Services/IFlutterwavePaymentService.cs
Summary: Service implementation or abstraction for the related infrastructure capability.
```csharp
namespace ShopFresherz.Domain.Interfaces.Services;

/// <summary>Fallback payment gateway integration for Flutterwave.</summary>
public interface IFlutterwavePaymentService
{
    Task<PaymentInitResult?> InitializeAsync(
        string email,
        Guid orderId,
        string orderNumber,
        decimal totalNgn,
        CancellationToken cancellationToken = default);
}
```

### ShopFresherz.Infrastructure/Services/PaystackPaymentService.cs
Summary: Service implementation or abstraction for the related infrastructure capability.
```csharp
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ShopFresherz.Domain.Interfaces.Services;

namespace ShopFresherz.Infrastructure.Services;

/// <summary>
/// Paystack payment gateway implementation of <see cref="IPaymentService"/>.
/// Uses the Paystack v1 REST API (https://api.paystack.co).
/// Amounts are in kobo (NGN Ã— 100) as required by Paystack.
/// </summary>
public sealed class PaystackPaymentService : IPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly string     _secretKey;
    private readonly ILogger<PaystackPaymentService> _logger;

    private const string BaseUrl = "https://api.paystack.co";

    /// <summary>Initialises a new instance of <see cref="PaystackPaymentService"/>.</summary>
    public PaystackPaymentService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<PaystackPaymentService> logger)
    {
        _secretKey  = configuration["Paystack:SecretKey"]
            ?? throw new InvalidOperationException("Paystack:SecretKey is not configured.");
        _httpClient = httpClientFactory.CreateClient(nameof(PaystackPaymentService));
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _secretKey);
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PaymentInitResult> InitialiseAsync(
        string email,
        long amountKobo,
        string reference,
        string callbackUrl,
        CancellationToken cancellationToken = default)
    {
        object body = new
        {
            email        = email,
            amount       = amountKobo,
            reference    = reference,
            callback_url = callbackUrl,
            currency     = "NGN",
        };

        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            $"{BaseUrl}/transaction/initialize",
            body,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        PaystackInitResponse? result = await response.Content.ReadFromJsonAsync<PaystackInitResponse>(
            cancellationToken: cancellationToken);

        if (result?.Data == null)
        {
            throw new InvalidOperationException("Paystack returned an empty initialisation response.");
        }

        return new PaymentInitResult(result.Data.AuthorizationUrl, result.Data.Reference);
    }

    /// <inheritdoc />
    public async Task<PaymentVerifyResult> VerifyAsync(string reference, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{BaseUrl}/transaction/verify/{Uri.EscapeDataString(reference)}",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        PaystackVerifyResponse? result = await response.Content.ReadFromJsonAsync<PaystackVerifyResponse>(
            cancellationToken: cancellationToken);

        if (result?.Data == null)
        {
            return new PaymentVerifyResult(false, "failed", 0, reference);
        }

        bool isSuccessful = string.Equals(result.Data.Status, "success", StringComparison.OrdinalIgnoreCase);

        return new PaymentVerifyResult(
            isSuccessful,
            result.Data.Status,
            result.Data.Amount,
            result.Data.Reference);
    }

    /// <inheritdoc />
    public async Task<bool> RefundAsync(string reference, long amountKobo, CancellationToken cancellationToken = default)
    {
        object body = new
        {
            transaction       = reference,
            amount            = amountKobo,
            currency          = "NGN",
            customer_note     = "Refund processed by ShopFresherz",
            merchant_note     = "Auto-refund via API",
        };

        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            $"{BaseUrl}/refund",
            body,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Paystack refund for {Reference} failed with status {Status}.", reference, response.StatusCode);
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public bool VerifyWebhookSignature(string rawBody, string signature)
    {
        byte[] keyBytes    = Encoding.UTF8.GetBytes(_secretKey);
        byte[] bodyBytes   = Encoding.UTF8.GetBytes(rawBody);

        byte[] hash = HMACSHA512.HashData(keyBytes, bodyBytes);
        string computed = Convert.ToHexString(hash).ToLowerInvariant();

        return string.Equals(computed, signature, StringComparison.OrdinalIgnoreCase);
    }

    // â”€â”€ Paystack response DTOs â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private sealed class PaystackInitResponse
    {
        [JsonPropertyName("data")]
        public PaystackInitData? Data { get; init; }
    }

    private sealed class PaystackInitData
    {
        [JsonPropertyName("authorization_url")]
        public string AuthorizationUrl { get; init; } = string.Empty;

        [JsonPropertyName("reference")]
        public string Reference { get; init; } = string.Empty;
    }

    private sealed class PaystackVerifyResponse
    {
        [JsonPropertyName("data")]
        public PaystackVerifyData? Data { get; init; }
    }

    private sealed class PaystackVerifyData
    {
        [JsonPropertyName("status")]
        public string Status { get; init; } = string.Empty;

        [JsonPropertyName("amount")]
        public long Amount { get; init; }

        [JsonPropertyName("reference")]
        public string Reference { get; init; } = string.Empty;
    }
}
```

### ShopFresherz.Infrastructure/Services/FlutterwavePaymentService.cs
Summary: Service implementation or abstraction for the related infrastructure capability.
```csharp
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShopFresherz.Domain.Interfaces.Services;
using ShopFresherz.Infrastructure.Configuration;

namespace ShopFresherz.Infrastructure.Services;

/// <summary>Fallback hosted checkout integration for Flutterwave.</summary>
public sealed class FlutterwavePaymentService : IFlutterwavePaymentService
{
    private readonly HttpClient _http;
    private readonly FlutterwaveOptions _options;
    private readonly ILogger<FlutterwavePaymentService> _logger;

    public FlutterwavePaymentService(
        HttpClient http,
        IOptions<FlutterwaveOptions> options,
        ILogger<FlutterwavePaymentService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>Initialises a Flutterwave hosted payment and returns the payment link.</summary>
    public async Task<PaymentInitResult?> InitializeAsync(
        string email,
        Guid orderId,
        string orderNumber,
        decimal totalNgn,
        CancellationToken cancellationToken)
    {
        try
        {
            object payload = new
            {
                tx_ref = orderNumber,
                amount = totalNgn,
                currency = "NGN",
                redirect_url = _options.CallbackUrl,
                customer = new { email },
                customizations = new
                {
                    title = "ShopFresherz",
                    logo = "https://shopfresherz.com/logo.png",
                },
                meta = new { orderId = orderId.ToString() },
            };

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.SecretKey);

            HttpResponseMessage response = await _http.PostAsJsonAsync(
                "https://api.flutterwave.com/v3/payments",
                payload,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Flutterwave init failed for order {OrderNumber}: {Status} {Body}",
                    orderNumber,
                    response.StatusCode,
                    body);
                return null;
            }

            using JsonDocument doc = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(cancellationToken));

            string? link = doc.RootElement
                .GetProperty("data")
                .GetProperty("link")
                .GetString();

            return string.IsNullOrWhiteSpace(link)
                ? null
                : new PaymentInitResult(link, orderNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Flutterwave exception for order {OrderNumber}", orderNumber);
            return null;
        }
    }
}
```

NOT FOUND: PaymentProviderFactory.cs - may not have been implemented yet

### ShopFresherz.API/Controllers/PaymentsController.cs
Summary: ASP.NET Core controller exposing the related HTTP endpoints.
```csharp
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ShopFresherz.Application.Features.Payments.Commands;
using ShopFresherz.Infrastructure.Configuration;

namespace ShopFresherz.API.Controllers;

/// <summary>Receives and verifies inbound payment gateway webhook events.</summary>
[ApiController]
[Route("api/v1/payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly PaystackOptions _options;
    private readonly FlutterwaveOptions _flutterwaveOptions;

    public PaymentsController(
        IMediator mediator,
        IOptions<PaystackOptions> options,
        IOptions<FlutterwaveOptions> flutterwaveOptions)
    {
        _mediator = mediator;
        _options = options.Value;
        _flutterwaveOptions = flutterwaveOptions.Value;
    }

    /// <summary>
    /// Receives Paystack webhook events.
    /// Verifies the HMAC-SHA512 signature before processing.
    /// Always returns 200 OK to prevent Paystack retry storms.
    /// </summary>
    [HttpPost("webhook/paystack")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> PaystackWebhook(CancellationToken cancellationToken)
    {
        Request.EnableBuffering();
        using StreamReader reader = new(Request.Body, Encoding.UTF8, leaveOpen: true);
        string rawBody = await reader.ReadToEndAsync(cancellationToken);
        Request.Body.Position = 0;

        string? paystackSignature = Request.Headers["x-paystack-signature"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(paystackSignature))
        {
            return Ok();
        }

        string expectedHash = ComputeHmacSha512(rawBody, _options.WebhookSecret);
        if (!string.Equals(expectedHash, paystackSignature, StringComparison.OrdinalIgnoreCase))
        {
            return Ok();
        }

        using JsonDocument doc = JsonDocument.Parse(rawBody);
        JsonElement root = doc.RootElement;

        string eventName = root.GetProperty("event").GetString() ?? string.Empty;
        JsonElement data = root.GetProperty("data");
        string reference = data.GetProperty("reference").GetString() ?? string.Empty;
        decimal amountKobo = data.GetProperty("amount").GetDecimal();
        string email = data.GetProperty("customer")
            .GetProperty("email").GetString() ?? string.Empty;

        await _mediator.Send(
            new HandlePaystackWebhookCommand(eventName, reference, amountKobo, email),
            cancellationToken);

        return Ok();
    }

    /// <summary>Receives Flutterwave fallback payment webhook events.</summary>
    [HttpPost("webhook/flutterwave")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> FlutterwaveWebhook(CancellationToken cancellationToken)
    {
        Request.EnableBuffering();
        using StreamReader reader = new(Request.Body, Encoding.UTF8, leaveOpen: true);
        string rawBody = await reader.ReadToEndAsync(cancellationToken);
        Request.Body.Position = 0;

        string? signature = Request.Headers["verif-hash"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(signature) ||
            !string.Equals(signature, _flutterwaveOptions.WebhookSecret, StringComparison.Ordinal))
        {
            return Ok();
        }

        using JsonDocument doc = JsonDocument.Parse(rawBody);
        JsonElement root = doc.RootElement;

        string eventType = root.GetProperty("event").GetString() ?? string.Empty;
        if (!string.Equals(eventType, "charge.completed", StringComparison.OrdinalIgnoreCase))
        {
            return Ok();
        }

        JsonElement data = root.GetProperty("data");
        string reference = data.GetProperty("tx_ref").GetString() ?? string.Empty;
        decimal amount = data.GetProperty("amount").GetDecimal();
        string email = data.GetProperty("customer").GetProperty("email").GetString() ?? string.Empty;
        string status = data.GetProperty("status").GetString() ?? string.Empty;

        if (!string.Equals(status, "successful", StringComparison.OrdinalIgnoreCase))
        {
            return Ok();
        }

        await _mediator.Send(
            new HandlePaystackWebhookCommand("charge.success", reference, amount * 100, email),
            cancellationToken);

        return Ok();
    }

    private static string ComputeHmacSha512(string data, string secret)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(secret);
        byte[] dataBytes = Encoding.UTF8.GetBytes(data);
        byte[] hashBytes = HMACSHA512.HashData(keyBytes, dataBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
```

### ShopFresherz.Application/Features/Payments/Commands/HandlePaystackWebhookCommand.cs
Summary: MediatR command, handler, and validation logic for the related workflow.
```csharp
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Enums;
using ShopFresherz.Domain.Interfaces;
using ShopFresherz.Domain.Interfaces.Services;

namespace ShopFresherz.Application.Features.Payments.Commands;

/// <summary>
/// Command for processing an inbound Paystack webhook event.
/// Stock is deducted and order status updated only after cryptographic signature verification in the controller.
/// </summary>
public sealed record HandlePaystackWebhookCommand(
    string Event,
    string Reference,
    decimal AmountKobo,
    string CustomerEmail) : IRequest<Result<bool>>;

/// <summary>Handler for <see cref="HandlePaystackWebhookCommand"/>.</summary>
public sealed class HandlePaystackWebhookCommandHandler
    : IRequestHandler<HandlePaystackWebhookCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;
    private readonly IEmailService _email;
    private readonly ISmsService _sms;

    public HandlePaystackWebhookCommandHandler(IUnitOfWork uow, IEmailService email, ISmsService sms)
    {
        _uow = uow;
        _email = email;
        _sms = sms;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        HandlePaystackWebhookCommand command,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(command.Event, "charge.success", StringComparison.OrdinalIgnoreCase))
        {
            return Result<bool>.Success(true);
        }

        Order? order = await _uow.Orders.GetByPaymentReferenceAsync(
            command.Reference, cancellationToken);

        if (order is null || order.PaymentStatus == PaymentStatus.Paid)
        {
            return Result<bool>.Success(true);
        }

        foreach (OrderItem item in order.Items)
        {
            Product? product = await _uow.Products.GetByIdAsync(item.ProductId, cancellationToken);
            if (product is null) continue;

            if (item.VariantId.HasValue)
            {
                ProductVariant? variant = product.Variants
                    .FirstOrDefault(v => v.Id == item.VariantId);
                if (variant is not null)
                {
                    variant.StockQty = Math.Max(0, variant.StockQty - item.Quantity);
                    variant.ReservedQty = Math.Max(0, variant.ReservedQty - item.Quantity);
                    product.SoldCount += item.Quantity;
                }
            }
            else
            {
                product.StockQty = Math.Max(0, product.StockQty - item.Quantity);
                product.ReservedQty = Math.Max(0, product.ReservedQty - item.Quantity);
                product.SoldCount += item.Quantity;
            }

            _uow.Products.Update(product);
        }

        User? orderUser = null;
        if (order.UserId.HasValue)
        {
            orderUser = await _uow.Users.GetByIdAsync(order.UserId.Value, cancellationToken);
            if (orderUser is not null)
            {
                int pointsEarned = (int)Math.Floor(order.Total / 100m);
                orderUser.LoyaltyPoints += pointsEarned;

                await _uow.LoyaltyTransactions.AddAsync(new LoyaltyTransaction
                {
                    UserId = orderUser.Id,
                    EventType = LoyaltyEventType.Earned,
                    Points = pointsEarned,
                    Description = $"Earned from order {order.OrderNumber}",
                    OrderId = order.Id,
                    ExpiresAt = DateTime.UtcNow.AddYears(1),
                }, cancellationToken);

                _uow.Users.Update(orderUser);
            }
        }

        order.Status = OrderStatus.Processing;
        order.PaymentStatus = PaymentStatus.Paid;
        _uow.Orders.Update(order);

        await _uow.SaveChangesAsync(cancellationToken);

        string recipientEmail = orderUser?.Email ?? command.CustomerEmail;

        _ = _email.SendOrderConfirmationAsync(
            recipientEmail,
            orderUser?.FirstName ?? "Customer",
            order.OrderNumber,
            order.Total,
            CancellationToken.None);

        if (!string.IsNullOrWhiteSpace(orderUser?.Phone))
        {
            _ = _sms.SendOrderUpdateAsync(
                orderUser.Phone,
                order.OrderNumber,
                "confirmed & being processed",
                CancellationToken.None);
        }

        return Result<bool>.Success(true);
    }
}
```

NOT FOUND: HandleFlutterwaveWebhookCommand.cs - may not have been implemented yet

NOT FOUND: InitiatePaymentCommand.cs - may not have been implemented yet

### ShopFresherz.Application/Dtos/PaymentMethods/PaymentMethodDtos.cs
Summary: DTO/request/response model definitions for the related API contract.
```csharp
namespace ShopFresherz.Application.Dtos.PaymentMethods;

/// <summary>Saved payment method summary DTO.</summary>
public sealed class PaymentMethodDto
{
    /// <summary>Gets or sets the payment method ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the card type (e.g., "Visa", "Mastercard", "Verve").</summary>
    public string CardType { get; set; } = string.Empty;

    /// <summary>Gets or sets the card number.</summary>
    public string CardNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the cardholder name.</summary>
    public string CardHolderName { get; set; } = string.Empty;

    /// <summary>Gets or sets the expiry month (1â€“12).</summary>
    public int ExpiryMonth { get; set; }

    /// <summary>Gets or sets the expiry year (4-digit).</summary>
    public int ExpiryYear { get; set; }

    /// <summary>Gets or sets whether this is the default payment method.</summary>
    public bool IsDefault { get; set; }

    /// <summary>Gets or sets when the payment method was added.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>Request payload for adding a bank card.</summary>
public sealed class CreatePaymentMethodRequest
{
    /// <summary>Gets or sets the card type (e.g., "Visa", "Mastercard", "Verve").</summary>
    public string CardType { get; set; } = string.Empty;

    /// <summary>Gets or sets the full card number (up to 19 digits).</summary>
    public string CardNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the cardholder name as it appears on the card.</summary>
    public string CardHolderName { get; set; } = string.Empty;

    /// <summary>Gets or sets the expiry month (1â€“12).</summary>
    public int ExpiryMonth { get; set; }

    /// <summary>Gets or sets the expiry year (4-digit, e.g., 2027).</summary>
    public int ExpiryYear { get; set; }

    /// <summary>Gets or sets whether this should be the default payment method.</summary>
    public bool IsDefault { get; set; } = false;
}

/// <summary>Request payload for updating a payment method.</summary>
public sealed class UpdatePaymentMethodRequest
{
    /// <summary>Gets or sets whether this should be the default payment method.</summary>
    public bool? IsDefault { get; set; }

    /// <summary>Gets or sets the updated cardholder name.</summary>
    public string? CardHolderName { get; set; }

    /// <summary>Gets or sets the updated expiry month.</summary>
    public int? ExpiryMonth { get; set; }

    /// <summary>Gets or sets the updated expiry year.</summary>
    public int? ExpiryYear { get; set; }
}
```

### ShopFresherz.Application/Features/PaymentMethods/Commands/CreatePaymentMethodCommand.cs
Summary: MediatR command, handler, and validation logic for the related workflow.
```csharp
using FluentValidation;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.PaymentMethods;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.PaymentMethods.Commands;

/// <summary>Command for saving a new bank card to the user's profile.</summary>
public sealed record CreatePaymentMethodCommand(Guid UserId, CreatePaymentMethodRequest Request)
    : IRequest<Result<Guid>>;

/// <summary>Handler for <see cref="CreatePaymentMethodCommand"/>.</summary>
public sealed class CreatePaymentMethodCommandHandler
    : IRequestHandler<CreatePaymentMethodCommand, Result<Guid>>
{
    private readonly IUnitOfWork _uow;

    public CreatePaymentMethodCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(
        CreatePaymentMethodCommand command,
        CancellationToken cancellationToken)
    {
        if (command.UserId == Guid.Empty)
        {
            return Error.Unauthorized("Unable to resolve the current user identity.");
        }

        CreatePaymentMethodRequest req = command.Request;

        // If this card should be the default, clear existing defaults first.
        if (req.IsDefault)
        {
            IReadOnlyList<SavedCard> existing =
                await _uow.PaymentMethods.GetByUserIdAsync(command.UserId, cancellationToken);

            foreach (SavedCard pm in existing.Where(p => p.IsDefault))
            {
                pm.IsDefault = false;
                _uow.PaymentMethods.Update(pm);
            }
        }

        SavedCard card = new()
        {
            UserId         = command.UserId,
            CardType       = req.CardType.Trim(),
            CardNumber     = req.CardNumber.Trim(),
            CardHolderName = req.CardHolderName.Trim(),
            ExpiryMonth    = req.ExpiryMonth,
            ExpiryYear     = req.ExpiryYear,
            IsDefault      = req.IsDefault,
        };

        await _uow.PaymentMethods.AddAsync(card, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(card.Id);
    }
}

/// <summary>Validator for <see cref="CreatePaymentMethodCommand"/>.</summary>
public sealed class CreatePaymentMethodCommandValidator : AbstractValidator<CreatePaymentMethodCommand>
{
    public CreatePaymentMethodCommandValidator()
    {
        RuleFor(x => x.Request.CardType).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Request.CardNumber).NotEmpty().MinimumLength(13).MaximumLength(19)
            .Matches(@"^\d{13,19}$").WithMessage("Card number must be 13â€“19 numeric digits.");
        RuleFor(x => x.Request.CardHolderName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.ExpiryMonth).InclusiveBetween(1, 12);
        RuleFor(x => x.Request.ExpiryYear).GreaterThanOrEqualTo(DateTime.UtcNow.Year);
    }
}
```

### ShopFresherz.Application/Features/PaymentMethods/Commands/DeletePaymentMethodCommand.cs
Summary: MediatR command, handler, and validation logic for the related workflow.
```csharp
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.PaymentMethods.Commands;

/// <summary>Command for removing a saved bank card.</summary>
public sealed record DeletePaymentMethodCommand(Guid UserId, Guid PaymentMethodId) : IRequest<Result<bool>>;

/// <summary>Handler for <see cref="DeletePaymentMethodCommand"/>.</summary>
public sealed class DeletePaymentMethodCommandHandler
    : IRequestHandler<DeletePaymentMethodCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;

    public DeletePaymentMethodCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        DeletePaymentMethodCommand command,
        CancellationToken cancellationToken)
    {
        SavedCard? method =
            await _uow.PaymentMethods.GetByIdAsync(command.PaymentMethodId, cancellationToken);

        if (method is null) return Error.NotFound("Payment method");
        if (method.UserId != command.UserId) return Error.Forbidden();

        _uow.PaymentMethods.Delete(method);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
```

### ShopFresherz.Application/Features/PaymentMethods/Commands/UpdatePaymentMethodCommand.cs
Summary: MediatR command, handler, and validation logic for the related workflow.
```csharp
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.PaymentMethods;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.PaymentMethods.Commands;

/// <summary>Command for updating a saved bank card.</summary>
public sealed record UpdatePaymentMethodCommand(Guid UserId, Guid PaymentMethodId, UpdatePaymentMethodRequest Request)
    : IRequest<Result<bool>>;

/// <summary>Handler for <see cref="UpdatePaymentMethodCommand"/>.</summary>
public sealed class UpdatePaymentMethodCommandHandler
    : IRequestHandler<UpdatePaymentMethodCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;

    public UpdatePaymentMethodCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        UpdatePaymentMethodCommand command,
        CancellationToken cancellationToken)
    {
        SavedCard? method =
            await _uow.PaymentMethods.GetByIdAsync(command.PaymentMethodId, cancellationToken);

        if (method is null) return Error.NotFound("Payment method");
        if (method.UserId != command.UserId) return Error.Forbidden();

        UpdatePaymentMethodRequest req = command.Request;

        // Set as default â€” clear other defaults first.
        if (req.IsDefault == true && !method.IsDefault)
        {
            IReadOnlyList<SavedCard> existing =
                await _uow.PaymentMethods.GetByUserIdAsync(command.UserId, cancellationToken);

            foreach (SavedCard pm in existing.Where(p => p.IsDefault && p.Id != method.Id))
            {
                pm.IsDefault = false;
                _uow.PaymentMethods.Update(pm);
            }

            method.IsDefault = true;
        }

        if (req.CardHolderName is not null) method.CardHolderName = req.CardHolderName.Trim();
        if (req.ExpiryMonth.HasValue)        method.ExpiryMonth    = req.ExpiryMonth.Value;
        if (req.ExpiryYear.HasValue)         method.ExpiryYear     = req.ExpiryYear.Value;

        _uow.PaymentMethods.Update(method);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
```

### ShopFresherz.Application/Features/PaymentMethods/Queries/GetPaymentMethodsQuery.cs
Summary: MediatR query and handler for read-side API data.
```csharp
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.PaymentMethods;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.PaymentMethods.Queries;

/// <summary>Query for all saved bank cards of the authenticated user.</summary>
/// <param name="UserId">The authenticated user's ID.</param>
public sealed record GetPaymentMethodsQuery(Guid UserId) : IRequest<Result<IReadOnlyList<PaymentMethodDto>>>;

/// <summary>Handler for <see cref="GetPaymentMethodsQuery"/>.</summary>
public sealed class GetPaymentMethodsQueryHandler
    : IRequestHandler<GetPaymentMethodsQuery, Result<IReadOnlyList<PaymentMethodDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetPaymentMethodsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PaymentMethodDto>>> Handle(
        GetPaymentMethodsQuery query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<SavedCard> cards =
            await _uow.PaymentMethods.GetByUserIdAsync(query.UserId, cancellationToken);

        IReadOnlyList<PaymentMethodDto> dtos = cards.Select(m => new PaymentMethodDto
        {
            Id             = m.Id,
            CardType       = m.CardType,
            CardNumber     = m.CardNumber,
            CardHolderName = m.CardHolderName,
            ExpiryMonth    = m.ExpiryMonth,
            ExpiryYear     = m.ExpiryYear,
            IsDefault      = m.IsDefault,
            CreatedAt      = m.CreatedAt,
        }).ToList();

        return Result<IReadOnlyList<PaymentMethodDto>>.Success(dtos);
    }
}
```

### ShopFresherz.Application/Features/Payments/Commands/HandlePaystackWebhookCommand.cs
Summary: MediatR command, handler, and validation logic for the related workflow.
```csharp
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Enums;
using ShopFresherz.Domain.Interfaces;
using ShopFresherz.Domain.Interfaces.Services;

namespace ShopFresherz.Application.Features.Payments.Commands;

/// <summary>
/// Command for processing an inbound Paystack webhook event.
/// Stock is deducted and order status updated only after cryptographic signature verification in the controller.
/// </summary>
public sealed record HandlePaystackWebhookCommand(
    string Event,
    string Reference,
    decimal AmountKobo,
    string CustomerEmail) : IRequest<Result<bool>>;

/// <summary>Handler for <see cref="HandlePaystackWebhookCommand"/>.</summary>
public sealed class HandlePaystackWebhookCommandHandler
    : IRequestHandler<HandlePaystackWebhookCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;
    private readonly IEmailService _email;
    private readonly ISmsService _sms;

    public HandlePaystackWebhookCommandHandler(IUnitOfWork uow, IEmailService email, ISmsService sms)
    {
        _uow = uow;
        _email = email;
        _sms = sms;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        HandlePaystackWebhookCommand command,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(command.Event, "charge.success", StringComparison.OrdinalIgnoreCase))
        {
            return Result<bool>.Success(true);
        }

        Order? order = await _uow.Orders.GetByPaymentReferenceAsync(
            command.Reference, cancellationToken);

        if (order is null || order.PaymentStatus == PaymentStatus.Paid)
        {
            return Result<bool>.Success(true);
        }

        foreach (OrderItem item in order.Items)
        {
            Product? product = await _uow.Products.GetByIdAsync(item.ProductId, cancellationToken);
            if (product is null) continue;

            if (item.VariantId.HasValue)
            {
                ProductVariant? variant = product.Variants
                    .FirstOrDefault(v => v.Id == item.VariantId);
                if (variant is not null)
                {
                    variant.StockQty = Math.Max(0, variant.StockQty - item.Quantity);
                    variant.ReservedQty = Math.Max(0, variant.ReservedQty - item.Quantity);
                    product.SoldCount += item.Quantity;
                }
            }
            else
            {
                product.StockQty = Math.Max(0, product.StockQty - item.Quantity);
                product.ReservedQty = Math.Max(0, product.ReservedQty - item.Quantity);
                product.SoldCount += item.Quantity;
            }

            _uow.Products.Update(product);
        }

        User? orderUser = null;
        if (order.UserId.HasValue)
        {
            orderUser = await _uow.Users.GetByIdAsync(order.UserId.Value, cancellationToken);
            if (orderUser is not null)
            {
                int pointsEarned = (int)Math.Floor(order.Total / 100m);
                orderUser.LoyaltyPoints += pointsEarned;

                await _uow.LoyaltyTransactions.AddAsync(new LoyaltyTransaction
                {
                    UserId = orderUser.Id,
                    EventType = LoyaltyEventType.Earned,
                    Points = pointsEarned,
                    Description = $"Earned from order {order.OrderNumber}",
                    OrderId = order.Id,
                    ExpiresAt = DateTime.UtcNow.AddYears(1),
                }, cancellationToken);

                _uow.Users.Update(orderUser);
            }
        }

        order.Status = OrderStatus.Processing;
        order.PaymentStatus = PaymentStatus.Paid;
        _uow.Orders.Update(order);

        await _uow.SaveChangesAsync(cancellationToken);

        string recipientEmail = orderUser?.Email ?? command.CustomerEmail;

        _ = _email.SendOrderConfirmationAsync(
            recipientEmail,
            orderUser?.FirstName ?? "Customer",
            order.OrderNumber,
            order.Total,
            CancellationToken.None);

        if (!string.IsNullOrWhiteSpace(orderUser?.Phone))
        {
            _ = _sms.SendOrderUpdateAsync(
                orderUser.Phone,
                order.OrderNumber,
                "confirmed & being processed",
                CancellationToken.None);
        }

        return Result<bool>.Success(true);
    }
}
```

Payment-related section in ShopFresherz.API/appsettings.json:
NOT FOUND: Payment-related section - may not have been implemented yet

Payment-related section in ShopFresherz.API/appsettings.Development.json:
NOT FOUND: Payment-related section - may not have been implemented yet

Payment-related section in ShopFresherz.API/appsettings.Production.json:
NOT FOUND: Payment-related section - may not have been implemented yet

## Report 4 - Order Flow
### ShopFresherz.Application/Features/Orders/Commands/PlaceOrderCommand.cs
Summary: MediatR command, handler, and validation logic for the related workflow.
```csharp
using System.Text.Json;
using FluentValidation;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Order;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Enums;
using ShopFresherz.Domain.Interfaces;
using ShopFresherz.Domain.Interfaces.Services;
using CartEntity = ShopFresherz.Domain.Entities.Cart;

namespace ShopFresherz.Application.Features.Orders.Commands;

/// <summary>Command for converting the active cart into a confirmed order.</summary>
/// <param name="UserId">The authenticated user's ID (null for guest checkout).</param>
/// <param name="Request">The order creation payload.</param>
public sealed record PlaceOrderCommand(Guid? UserId, CreateOrderRequest Request)
    : IRequest<Result<CreateOrderResponse>>;

/// <summary>Handler for <see cref="PlaceOrderCommand"/>.</summary>
public sealed class PlaceOrderCommandHandler
    : IRequestHandler<PlaceOrderCommand, Result<CreateOrderResponse>>
{
    private readonly IUnitOfWork _uow;
    private readonly IPaymentService _payment;
    private readonly IFlutterwavePaymentService _flutterwavePayment;
    private readonly IEmailService _email;

    /// <summary>Initialises the handler.</summary>
    public PlaceOrderCommandHandler(
        IUnitOfWork uow,
        IPaymentService payment,
        IFlutterwavePaymentService flutterwavePayment,
        IEmailService email)
    {
        _uow = uow;
        _payment = payment;
        _flutterwavePayment = flutterwavePayment;
        _email = email;
    }

    /// <inheritdoc />
    public async Task<Result<CreateOrderResponse>> Handle(
        PlaceOrderCommand command,
        CancellationToken cancellationToken)
    {
        CreateOrderRequest req = command.Request;

        // Resolve cart.
        CartEntity? cart = command.UserId.HasValue
            ? await _uow.Carts.GetByUserIdAsync(command.UserId.Value, cancellationToken)
            : !string.IsNullOrWhiteSpace(req.GuestSessionId)
                ? await _uow.Carts.GetBySessionIdAsync(req.GuestSessionId, cancellationToken)
                : null;

        if (cart is null || !cart.Items.Any())
        {
            return Error.Validation("Cart is empty. Add items before placing an order.");
        }

        // Resolve delivery address JSON snapshot.
        string addressJson;
        if (req.AddressId.HasValue)
        {
            Address? addr = await _uow.Addresses.GetByIdAsync(req.AddressId.Value, cancellationToken);
            if (addr is null) return Error.NotFound("Delivery address");
            addressJson = JsonSerializer.Serialize(new
            {
                addr.Label, addr.Line1, addr.Line2,
                addr.City, addr.State, addr.PostalCode,
            });
        }
        else if (req.InlineAddress is not null)
        {
            addressJson = JsonSerializer.Serialize(req.InlineAddress);
        }
        else
        {
            return Error.Validation("A delivery address is required.");
        }

        // Apply coupon.
        decimal discount = 0m;
        Coupon? coupon = null;
        if (!string.IsNullOrWhiteSpace(req.CouponCode))
        {
            coupon = await _uow.Coupons.GetByCodeAsync(req.CouponCode, cancellationToken);
            if (coupon is not null && coupon.IsActive && (coupon.ExpiresAt is null || coupon.ExpiresAt > DateTime.UtcNow))
            {
                decimal subtotal = cart.Items.Sum(i =>
                    (i.Variant?.Price ?? i.Product.Price) * i.Quantity);

                if (coupon.MinimumOrderAmount is null || subtotal >= coupon.MinimumOrderAmount)
                {
                    discount = coupon.Type == Domain.Enums.CouponType.Percentage
                        ? subtotal * (coupon.Value / 100m)
                        : coupon.Value;
                }
            }
        }

        // Build order items + reserve stock.
        List<OrderItem> items = new();
        decimal itemsSubtotal = 0m;

        foreach (CartItem ci in cart.Items)
        {
            Product? product = await _uow.Products.GetByIdWithLockAsync(ci.ProductId, cancellationToken);
            if (product is null) return Error.NotFound($"Product {ci.ProductId}");

            decimal unitPrice = ci.VariantId.HasValue
                ? product.Variants.FirstOrDefault(v => v.Id == ci.VariantId)?.Price ?? product.Price
                : product.Price;

            int availableQty = ci.VariantId.HasValue
                ? product.Variants.FirstOrDefault(v => v.Id == ci.VariantId)?.AvailableQty ?? 0
                : product.AvailableQty;

            if (availableQty < ci.Quantity)
            {
                return Error.Validation($"Insufficient stock for '{product.Name}'. Only {availableQty} unit(s) available.");
            }

            // Reserve stock.
            if (ci.VariantId.HasValue)
            {
                ProductVariant? variant = product.Variants.FirstOrDefault(v => v.Id == ci.VariantId);
                if (variant is not null) variant.ReservedQty += ci.Quantity;
            }
            else
            {
                product.ReservedQty += ci.Quantity;
            }

            _uow.Products.Update(product);

            decimal lineTotal = unitPrice * ci.Quantity;
            itemsSubtotal += lineTotal;

            items.Add(new OrderItem
            {
                ProductId   = ci.ProductId,
                VariantId   = ci.VariantId,
                Quantity    = ci.Quantity,
                UnitPrice   = unitPrice,
                LineTotal   = lineTotal,
                ProductSnapshotJson = JsonSerializer.Serialize(new
                {
                    Name   = product.Name,
                    SKU    = product.SKU,
                    Slug   = product.Slug,
                    ImageUrl = product.Images.OrderBy(img => img.SortOrder).FirstOrDefault()?.DisplayUrl,
                }),
            });
        }

        decimal deliveryFee = req.DeliveryMethod == DeliveryMethod.Express ? 3500m : 1500m;
        decimal vatAmount   = (itemsSubtotal - discount) * 0.075m;
        decimal total       = itemsSubtotal - discount + deliveryFee + vatAmount;

        string orderNumber = await _uow.Orders.GenerateOrderNumberAsync(cancellationToken);

        Order order = new()
        {
            OrderNumber         = orderNumber,
            UserId              = command.UserId,
            GuestEmail          = req.GuestEmail,
            Status              = OrderStatus.Pending,
            PaymentStatus       = PaymentStatus.Unpaid,
            PaymentMethod       = req.PaymentMethod,
            Subtotal            = itemsSubtotal,
            DiscountAmount      = discount,
            DeliveryFee         = deliveryFee,
            VatAmount           = vatAmount,
            Total               = total,
            CouponId            = coupon?.Id,
            DeliveryAddressJson = addressJson,
            DeliveryMethod      = req.DeliveryMethod,
            EstimatedDelivery   = DateTime.UtcNow.AddDays(req.DeliveryMethod == DeliveryMethod.Express ? 2 : 5),
            Notes               = req.Notes,
            Items               = items,
        };

        await _uow.Orders.AddAsync(order, cancellationToken);

        if (coupon is not null)
        {
            coupon.UsedCount++;
            _uow.Coupons.Update(coupon);
        }

        // Clear the cart.
        cart.Items.Clear();
        _uow.Carts.Update(cart);

        await _uow.SaveChangesAsync(cancellationToken);

        // Initiate payment.
        string? paymentUrl  = null;
        string? paymentRef  = null;

        if (req.PaymentMethod != PaymentMethod.PayOnDelivery)
        {
            string recipientEmail = command.UserId.HasValue
                ? (await _uow.Users.GetByIdAsync(command.UserId.Value, cancellationToken))?.Email ?? req.GuestEmail ?? string.Empty
                : req.GuestEmail ?? string.Empty;

            long amountKobo = (long)(total * 100);

            Domain.Interfaces.Services.PaymentInitResult? payResult = null;
            try
            {
                payResult = await _payment.InitialiseAsync(
                    recipientEmail,
                    amountKobo,
                    orderNumber,
                    $"https://shopfresherz.com/checkout/verify?ref={orderNumber}",
                    cancellationToken);
            }
            catch
            {
                payResult = null;
            }

            if (payResult is null)
            {
                payResult = await _flutterwavePayment.InitializeAsync(
                    recipientEmail,
                    order.Id,
                    orderNumber,
                    total,
                    cancellationToken);
            }

            if (payResult is null)
            {
                return Error.Internal("Payment initialisation failed. Please try again.");
            }

            order.PaymentReference = payResult.Reference;
            _uow.Orders.Update(order);
            await _uow.SaveChangesAsync(cancellationToken);

            paymentUrl = payResult.AuthorisationUrl;
            paymentRef = payResult.Reference;
        }

        _ = _email.SendOrderConfirmationAsync(
            req.GuestEmail ?? string.Empty,
            "Customer",
            orderNumber,
            total,
            CancellationToken.None);

        return Result<CreateOrderResponse>.Success(new CreateOrderResponse
        {
            OrderId          = order.Id,
            OrderNumber      = orderNumber,
            PaymentUrl       = paymentUrl,
            PaymentReference = paymentRef,
            Total            = total,
        });
    }
}

/// <summary>Validator for <see cref="PlaceOrderCommand"/>.</summary>
public sealed class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public PlaceOrderCommandValidator()
    {
        RuleFor(x => x.Request.PaymentMethod)
            .IsInEnum().WithMessage("A valid payment method is required.");

        RuleFor(x => x.Request.DeliveryMethod)
            .IsInEnum().WithMessage("A valid delivery method is required.");

        RuleFor(x => x.Request.GuestEmail)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Request.GuestEmail));

        RuleFor(x => x)
            .Must(x => x.UserId.HasValue || !string.IsNullOrWhiteSpace(x.Request.GuestEmail))
            .WithMessage("Guest email is required for guest checkout.");
    }
}
```

### ShopFresherz.API/Controllers/OrdersController.cs
Summary: ASP.NET Core controller exposing the related HTTP endpoints.
```csharp
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Order;
using ShopFresherz.Application.Features.Orders.Commands;
using ShopFresherz.Application.Features.Orders.Queries;

namespace ShopFresherz.API.Controllers;

/// <summary>Handles order placement, history, detail, and cancellation.</summary>
[ApiController]
[Route("api/v1/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>Initialises a new instance of <see cref="OrdersController"/>.</summary>
    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Places a new order from the active cart.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateOrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PlaceOrder(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        Result<CreateOrderResponse> result = await _mediator.Send(
            new PlaceOrderCommand(GetUserId(), request), cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetByNumber), new { orderNumber = result.Value!.OrderNumber }, result.Value)
            : MapError(result.Error);
    }

    /// <summary>Returns paginated order history for the authenticated user.</summary>
    [Authorize]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders(
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        Guid userId = GetRequiredUserId();
        Result<PagedResult<OrderDto>> result = await _mediator.Send(
            new GetOrdersQuery(userId, page, pageSize), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Returns full detail of a single order by its order number.</summary>
    [HttpGet("{orderNumber}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByNumber(
        [FromRoute] string orderNumber,
        CancellationToken cancellationToken)
    {
        Result<OrderDto> result = await _mediator.Send(
            new GetOrderByNumberQuery(orderNumber, GetUserId()), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Cancels an order that has not yet been shipped.</summary>
    [Authorize]
    [HttpPost("{orderNumber}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(
        [FromRoute] string orderNumber,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await _mediator.Send(
            new CancelOrderCommand(orderNumber, GetUserId()), cancellationToken);

        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    private Guid? GetUserId()
    {
        string? sub =
            User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub) ??
            User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out Guid id) ? id : null;
    }

    private Guid GetRequiredUserId()
    {
        string? sub =
            User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub) ??
            User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out Guid id) ? id : Guid.Empty;
    }

    private IActionResult MapError(Error error) => error.Code switch
    {
        "NOT_FOUND"   => NotFound(new { error.Code, error.Message }),
        "FORBIDDEN"   => Forbid(),
        "VALIDATION"  => BadRequest(new { error.Code, error.Message }),
        "UNAUTHORIZED"=> Unauthorized(new { error.Code, error.Message }),
        _             => StatusCode(StatusCodes.Status500InternalServerError, new { error.Code, error.Message }),
    };
}
```

### ShopFresherz.Domain/Entities/Order.cs
Summary: Domain entity definition.
```csharp
using ShopFresherz.Domain.Common;
using ShopFresherz.Domain.Enums;

namespace ShopFresherz.Domain.Entities;

/// <summary>
/// Represents a customer order. Supports both registered users and guest checkout.
/// Follows a strict state machine (see PRD Section 9.2).
/// </summary>
public class Order : BaseEntity
{
    /// <summary>Gets or sets the human-readable order reference (e.g., SFZ-2026-00001).</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the registered user ID (null for guest orders).</summary>
    public Guid? UserId { get; set; }

    /// <summary>Gets or sets the registered user navigation property.</summary>
    public User? User { get; set; }

    /// <summary>Gets or sets the guest email for guest checkout orders.</summary>
    public string? GuestEmail { get; set; }

    /// <summary>Gets or sets the current order state machine status.</summary>
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    /// <summary>Gets or sets the payment confirmation status.</summary>
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

    /// <summary>Gets or sets the payment method used at checkout.</summary>
    public PaymentMethod? PaymentMethod { get; set; }

    /// <summary>Gets or sets the Paystack/Flutterwave transaction reference.</summary>
    public string? PaymentReference { get; set; }

    /// <summary>Gets or sets the order subtotal before discounts (server-calculated).</summary>
    public decimal Subtotal { get; set; }

    /// <summary>Gets or sets the total coupon/loyalty discount applied.</summary>
    public decimal DiscountAmount { get; set; } = 0;

    /// <summary>Gets or sets the delivery fee applied at checkout.</summary>
    public decimal DeliveryFee { get; set; } = 0;

    /// <summary>Gets or sets the Nigerian VAT amount (7.5% of subtotal minus discount).</summary>
    public decimal VatAmount { get; set; } = 0;

    /// <summary>Gets or sets the final charge amount (Subtotal âˆ’ Discount + Delivery + VAT).</summary>
    public decimal Total { get; set; }

    /// <summary>Gets or sets the applied coupon ID.</summary>
    public int? CouponId { get; set; }

    /// <summary>Gets or sets the applied coupon navigation property.</summary>
    public Coupon? Coupon { get; set; }

    /// <summary>
    /// Gets or sets the JSON snapshot of the delivery address at order time
    /// (immutable; decoupled from user's current saved addresses).
    /// </summary>
    public string DeliveryAddressJson { get; set; } = string.Empty;

    /// <summary>Gets or sets the selected delivery method.</summary>
    public DeliveryMethod DeliveryMethod { get; set; }

    /// <summary>Gets or sets the estimated delivery date.</summary>
    public DateTime? EstimatedDelivery { get; set; }

    /// <summary>Gets or sets the courier tracking number (set when status transitions to Shipped).</summary>
    public string? TrackingNumber { get; set; }

    /// <summary>Gets or sets the optional customer order note.</summary>
    public string? Notes { get; set; }

    /// <summary>Gets or sets the line items in this order.</summary>
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
```

### ShopFresherz.Domain/Entities/OrderItem.cs
Summary: Domain entity definition.
```csharp
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

    /// <summary>Gets or sets the line total (UnitPrice Ã— Quantity).</summary>
    public decimal LineTotal { get; set; }

    /// <summary>
    /// Gets or sets the JSON snapshot of the product at order time
    /// (name, SKU, images, variant attributes â€” immutable history).
    /// </summary>
    public string ProductSnapshotJson { get; set; } = string.Empty;
}
```

### ShopFresherz.Domain/Enums/OrderStatus.cs
Summary: Domain enum definition.
```csharp
namespace ShopFresherz.Domain.Enums;

/// <summary>Represents the lifecycle states of an order in the state machine.</summary>
public enum OrderStatus
{
    /// <summary>Order created; stock reserved; awaiting payment initiation.</summary>
    Pending = 0,

    /// <summary>Payment flow started; 30-minute timeout active.</summary>
    AwaitingPayment = 1,

    /// <summary>Payment confirmed via gateway webhook; stock deducted.</summary>
    Paid = 2,

    /// <summary>Admin confirmed; fulfilment triggered.</summary>
    Processing = 3,

    /// <summary>Tracking number added; in transit.</summary>
    Shipped = 4,

    /// <summary>Delivery confirmed; loyalty points awarded.</summary>
    Delivered = 5,

    /// <summary>Order cancelled; refund initiated if paid; stock released.</summary>
    Cancelled = 6,

    /// <summary>Customer initiated return within 7 days of delivery.</summary>
    RefundRequested = 7,

    /// <summary>Refund approved; stock restocked; gateway refund processed.</summary>
    Refunded = 8
}
```

### ShopFresherz.Domain/Enums/PaymentStatus.cs
Summary: Domain enum definition.
```csharp
namespace ShopFresherz.Domain.Enums;

/// <summary>Represents the payment state of an order.</summary>
public enum PaymentStatus
{
    /// <summary>No payment received yet.</summary>
    Unpaid = 0,

    /// <summary>Payment successfully confirmed by gateway webhook.</summary>
    Paid = 1,

    /// <summary>Payment has been refunded to customer.</summary>
    Refunded = 2
}
```

### ShopFresherz.Application/Dtos/Order/OrderDtos.cs
Summary: DTO/request/response model definitions for the related API contract.
```csharp
using ShopFresherz.Domain.Enums;

namespace ShopFresherz.Application.Dtos.Order;

/// <summary>Snapshot of the delivery address embedded in an order response.</summary>
public sealed class DeliveryAddressSnapshot
{
    /// <summary>Gets or sets the friendly label (e.g., "Home").</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the first address line.</summary>
    public string Line1 { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional second address line.</summary>
    public string? Line2 { get; set; }

    /// <summary>Gets or sets the city.</summary>
    public string City { get; set; } = string.Empty;

    /// <summary>Gets or sets the Nigerian state.</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>Gets or sets the postal code.</summary>
    public string? PostalCode { get; set; }
}

/// <summary>Snapshot of the product embedded in an order line item.</summary>
public sealed class ProductSnapshot
{
    /// <summary>Gets or sets the product name at order time.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the product SKU at order time.</summary>
    public string SKU { get; set; } = string.Empty;

    /// <summary>Gets or sets the primary 540px image URL at order time.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Gets or sets the product slug at order time.</summary>
    public string Slug { get; set; } = string.Empty;
}

/// <summary>Order line item DTO.</summary>
public sealed class OrderItemDto
{
    /// <summary>Gets or sets the order item ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the product ID.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Gets or sets the selected variant ID.</summary>
    public Guid? VariantId { get; set; }

    /// <summary>Gets or sets the product snapshot at time of order.</summary>
    public ProductSnapshot? ProductSnapshot { get; set; }

    /// <summary>Gets or sets the quantity ordered.</summary>
    public int Quantity { get; set; }

    /// <summary>Gets or sets the unit price at order time.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Gets or sets the computed line total.</summary>
    public decimal LineTotal { get; set; }
}

/// <summary>Full order DTO returned to clients and admins.</summary>
public sealed class OrderDto
{
    /// <summary>Gets or sets the order ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the human-readable order reference.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the owning user ID.</summary>
    public Guid? UserId { get; set; }

    /// <summary>Gets or sets the current order status.</summary>
    public OrderStatus Status { get; set; }

    /// <summary>Gets or sets the payment status.</summary>
    public PaymentStatus PaymentStatus { get; set; }

    /// <summary>Gets or sets the payment method.</summary>
    public PaymentMethod? PaymentMethod { get; set; }

    /// <summary>Gets or sets the subtotal before discounts.</summary>
    public decimal Subtotal { get; set; }

    /// <summary>Gets or sets the discount amount.</summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>Gets or sets the delivery fee.</summary>
    public decimal DeliveryFee { get; set; }

    /// <summary>Gets or sets the VAT amount (7.5%).</summary>
    public decimal VatAmount { get; set; }

    /// <summary>Gets or sets the final total.</summary>
    public decimal Total { get; set; }

    /// <summary>Gets or sets the delivery address snapshot.</summary>
    public DeliveryAddressSnapshot? DeliveryAddress { get; set; }

    /// <summary>Gets or sets the selected delivery method.</summary>
    public DeliveryMethod DeliveryMethod { get; set; }

    /// <summary>Gets or sets the estimated delivery date.</summary>
    public DateTime? EstimatedDelivery { get; set; }

    /// <summary>Gets or sets the courier tracking number.</summary>
    public string? TrackingNumber { get; set; }

    /// <summary>Gets or sets the customer order note.</summary>
    public string? Notes { get; set; }

    /// <summary>Gets or sets the order line items.</summary>
    public IReadOnlyList<OrderItemDto> Items { get; set; } = [];

    /// <summary>Gets or sets the UTC timestamp when the order was placed.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>Request payload for creating a new order from the active cart.</summary>
public sealed class CreateOrderRequest
{
    /// <summary>Gets or sets the saved address ID to deliver to (mutually exclusive with InlineAddress).</summary>
    public Guid? AddressId { get; set; }

    /// <summary>Gets or sets an inline address (mutually exclusive with AddressId).</summary>
    public InlineAddressRequest? InlineAddress { get; set; }

    /// <summary>Gets or sets the selected delivery method.</summary>
    public DeliveryMethod DeliveryMethod { get; set; }

    /// <summary>Gets or sets the selected payment method.</summary>
    public PaymentMethod PaymentMethod { get; set; }

    /// <summary>Gets or sets the coupon code to apply (optional).</summary>
    public string? CouponCode { get; set; }

    /// <summary>Gets or sets optional customer notes.</summary>
    public string? Notes { get; set; }

    /// <summary>Gets or sets the guest email for guest checkout (required if not authenticated).</summary>
    public string? GuestEmail { get; set; }

    /// <summary>Gets or sets the guest session ID for guest checkout.</summary>
    public string? GuestSessionId { get; set; }
}

/// <summary>Inline address for one-time checkout without saving to profile.</summary>
public sealed class InlineAddressRequest
{
    /// <summary>Gets or sets the address label.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the first address line.</summary>
    public string Line1 { get; set; } = string.Empty;

    /// <summary>Gets or sets the second address line.</summary>
    public string? Line2 { get; set; }

    /// <summary>Gets or sets the city.</summary>
    public string City { get; set; } = string.Empty;

    /// <summary>Gets or sets the Nigerian state.</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>Gets or sets the postal code.</summary>
    public string? PostalCode { get; set; }
}

/// <summary>Response returned after a successful order creation.</summary>
public sealed class CreateOrderResponse
{
    /// <summary>Gets or sets the created order ID.</summary>
    public Guid OrderId { get; set; }

    /// <summary>Gets or sets the order number.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the Paystack authorisation URL to redirect the customer to.</summary>
    public string? PaymentUrl { get; set; }

    /// <summary>Gets or sets the payment reference for subsequent verification.</summary>
    public string? PaymentReference { get; set; }

    /// <summary>Gets or sets the order total.</summary>
    public decimal Total { get; set; }
}

/// <summary>Request payload for updating order status (admin).</summary>
public sealed class UpdateOrderStatusRequest
{
    /// <summary>Gets or sets the new order status.</summary>
    public OrderStatus Status { get; set; }

    /// <summary>Gets or sets the courier tracking number (required when transitioning to Shipped).</summary>
    public string? TrackingNumber { get; set; }

    /// <summary>Gets or sets the estimated delivery date.</summary>
    public DateTime? EstimatedDelivery { get; set; }
}
```

## Report 5 - Auth & Security
### ShopFresherz.API/Controllers/AuthController.cs
Summary: ASP.NET Core controller exposing the related HTTP endpoints.
```csharp
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Auth;
using ShopFresherz.Application.Features.Auth.Commands.ForgotPassword;
using ShopFresherz.Application.Features.Auth.Commands.Login;
using ShopFresherz.Application.Features.Auth.Commands.Logout;
using ShopFresherz.Application.Features.Auth.Commands.RefreshToken;
using ShopFresherz.Application.Features.Auth.Commands.Register;
using ShopFresherz.Application.Features.Auth.Commands.ResetPassword;
using ShopFresherz.Application.Features.Auth.Commands.VerifyEmail;

namespace ShopFresherz.API.Controllers;

/// <summary>Handles user registration, login, token refresh, and password recovery.</summary>
[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>Initialises a new instance of <see cref="AuthController"/>.</summary>
    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Registers a new customer account and returns access + refresh tokens.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        Result<AuthResponse> result =
            await _mediator.Send(new RegisterCommand(request), cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(Register), result.Value)
            : MapError(result.Error);
    }

    /// <summary>Authenticates an existing user and returns access + refresh tokens.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        Result<AuthResponse> result =
            await _mediator.Send(new LoginCommand(request), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Rotates the access and refresh token pair using the supplied refresh token.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        Result<AuthResponse> result =
            await _mediator.Send(new RefreshTokenCommand(request), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>Verifies the user's email using a 6-digit OTP sent at registration.</summary>
    [HttpPost("verify-email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyOtpRequest request,
        CancellationToken cancellationToken)
    {
        Result<bool> result =
            await _mediator.Send(new VerifyEmailCommand(request), cancellationToken);

        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    /// <summary>Sends a password-reset OTP to the supplied email address.</summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new ForgotPasswordCommand(request), cancellationToken);
        return NoContent();
    }

    /// <summary>Completes a password reset using the OTP and new password.</summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        Result<bool> result =
            await _mediator.Send(new ResetPasswordCommand(request), cancellationToken);

        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    /// <summary>Revokes the current user's refresh token (server-side logout).</summary>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        Guid userId = GetUserId();
        await _mediator.Send(new LogoutCommand(userId), cancellationToken);
        return NoContent();
    }

    // â”€â”€ Helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private Guid GetUserId()
    {
        string? sub =
            User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub) ??
            User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out Guid id) ? id : Guid.Empty;
    }

    private IActionResult MapError(Error error) => error.Code switch
    {
        "NOT_FOUND"   => NotFound(new { error.Code, error.Message }),
        "CONFLICT"    => Conflict(new { error.Code, error.Message }),
        "UNAUTHORIZED"=> Unauthorized(new { error.Code, error.Message }),
        "FORBIDDEN"   => Forbid(),
        "VALIDATION"  => BadRequest(new { error.Code, error.Message }),
        _             => StatusCode(StatusCodes.Status500InternalServerError, new { error.Code, error.Message }),
    };
}
```

### ShopFresherz.Infrastructure/Services/TokenService.cs
Summary: Service implementation or abstraction for the related infrastructure capability.
```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces.Services;

namespace ShopFresherz.Infrastructure.Services;

/// <summary>
/// JWT RS256 token service.
/// Reads the RSA private key PEM from configuration key <c>Jwt:PrivateKeyPem</c>
/// and the public key from <c>Jwt:PublicKeyPem</c>.
/// Access tokens expire in 15 minutes; refresh tokens are 64-byte CSPRNG strings.
/// </summary>
public sealed class TokenService : ITokenService
{
    private readonly RsaSecurityKey _privateKey;
    private readonly RsaSecurityKey _publicKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly JwtSecurityTokenHandler _handler = new();

    /// <summary>Initialises a new instance of <see cref="TokenService"/>.</summary>
    public TokenService(IConfiguration configuration)
    {
        string privateKeyPem = configuration["Jwt:PrivateKeyPem"]
            ?? throw new InvalidOperationException("Jwt:PrivateKeyPem is not configured.");
        string publicKeyPem  = configuration["Jwt:PublicKeyPem"]
            ?? throw new InvalidOperationException("Jwt:PublicKeyPem is not configured.");

        // SmarterASP environment variables cannot contain literal newlines.
        // They may be provided as "\\n"; normalize to actual newlines.
        privateKeyPem = privateKeyPem.Replace("\\n", "\n");
        publicKeyPem  = publicKeyPem.Replace("\\n", "\n");


        // Environment variables may store PEM keys with escaped newlines (\n).
        // Normalize both formats to actual newlines.
        privateKeyPem = privateKeyPem.Replace("\\n", "\n").Trim();
        publicKeyPem = publicKeyPem.Replace("\\n", "\n").Trim();

        RSA privateRsa = RSA.Create();
        privateRsa.ImportFromPem(privateKeyPem);
        _privateKey = new RsaSecurityKey(privateRsa);

        RSA publicRsa = RSA.Create();
        publicRsa.ImportFromPem(publicKeyPem);
        _publicKey = new RsaSecurityKey(publicRsa);

        _issuer = configuration["Jwt:Issuer"] ?? "ShopFresherz";
        _audience = configuration["Jwt:Audience"] ?? "ShopFresherz";
    }

    /// <inheritdoc />
    public string GenerateAccessToken(User user)
    {
        Claim[] claims =
        [
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        ];

        SigningCredentials credentials = new(
            _privateKey,
            SecurityAlgorithms.RsaSha256);

        JwtSecurityToken token = new(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials);

        return _handler.WriteToken(token);
    }

    /// <inheritdoc />
    public string GenerateRefreshToken()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    /// <inheritdoc />
    public Guid? GetUserIdFromExpiredToken(string token)
    {
        TokenValidationParameters parameters = new()
        {
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _publicKey,
            ValidateLifetime = false, // Allow expired tokens for refresh
        };

        try
        {
            ClaimsPrincipal principal = _handler.ValidateToken(token, parameters, out _);
            string? sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return Guid.TryParse(sub, out Guid id) ? id : null;
        }
        catch
        {
            return null;
        }
    }
}
```

### ShopFresherz.Domain/Interfaces/Services/ITokenService.cs
Summary: Service implementation or abstraction for the related infrastructure capability.
```csharp
using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Domain.Interfaces.Services;

/// <summary>
/// Contract for JWT RS256 access token and refresh token generation/validation.
/// Access tokens expire in 15 minutes; refresh tokens expire in 7 days.
/// </summary>
public interface ITokenService
{
    /// <summary>Generates a signed RS256 JWT access token for the given user.</summary>
    string GenerateAccessToken(User user);

    /// <summary>Generates a cryptographically secure refresh token string.</summary>
    string GenerateRefreshToken();

    /// <summary>Extracts the user ID claim from an expired access token (used during refresh).</summary>
    Guid? GetUserIdFromExpiredToken(string token);
}
```

### ShopFresherz.Domain/Entities/User.cs
Summary: Domain entity definition.
```csharp
using ShopFresherz.Domain.Common;
using ShopFresherz.Domain.Enums;

namespace ShopFresherz.Domain.Entities;

/// <summary>
/// Represents a registered platform user (customer, admin, or super admin).
/// </summary>
public class User : BaseEntity
{
    /// <summary>Gets or sets the user's email address (lowercase normalised, unique).</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the user's phone number in international format (+234...).</summary>
    public string? Phone { get; set; }

    /// <summary>Gets or sets the BCrypt cost-12 hashed password.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Gets or sets the user's first name.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Gets or sets the user's last name.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Gets or sets the CDN URL to the user's avatar image.</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>Gets or sets the user's access role on the platform.</summary>
    public UserRole Role { get; set; } = UserRole.Customer;

    /// <summary>Gets or sets a value indicating whether email or phone has been verified.</summary>
    public bool IsVerified { get; set; } = false;

    /// <summary>Gets or sets the user's current loyalty points balance.</summary>
    public int LoyaltyPoints { get; set; } = 0;

    /// <summary>Gets or sets the Google OAuth subject identifier.</summary>
    public string? GoogleId { get; set; }

    /// <summary>Gets or sets the hashed refresh token (SHA-256).</summary>
    public string? RefreshTokenHash { get; set; }

    /// <summary>Gets or sets the UTC expiry of the current refresh token.</summary>
    public DateTime? RefreshTokenExpires { get; set; }

    /// <summary>Gets or sets the UTC timestamp of the user's last successful login.</summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>Gets or sets the collection of orders placed by this user.</summary>
    public ICollection<Order> Orders { get; set; } = new List<Order>();

    /// <summary>Gets or sets the collection of saved delivery addresses.</summary>
    public ICollection<Address> Addresses { get; set; } = new List<Address>();

    /// <summary>Gets or sets the collection of product reviews submitted by this user.</summary>
    public ICollection<Review> Reviews { get; set; } = new List<Review>();

    /// <summary>Gets or sets the user's wishlist items.</summary>
    public ICollection<Wishlist> Wishlist { get; set; } = new List<Wishlist>();

    /// <summary>Gets or sets the loyalty points transaction history.</summary>
    public ICollection<LoyaltyTransaction> LoyaltyTransactions { get; set; } = new List<LoyaltyTransaction>();

    /// <summary>Gets or sets the collection of saved bank cards.</summary>
    public ICollection<SavedCard> SavedCards { get; set; } = new List<SavedCard>();

    /// <summary>Gets or sets whether order update notifications are enabled.</summary>
    public bool NotificationOrderUpdates { get; set; } = true;

    /// <summary>Gets or sets whether promotional notifications are enabled.</summary>
    public bool NotificationPromotions { get; set; } = true;

    /// <summary>Gets or sets whether back-in-stock notifications are enabled.</summary>
    public bool NotificationBackInStock { get; set; } = true;

    /// <summary>Gets or sets whether wishlist reminder notifications are enabled.</summary>
    public bool NotificationWishlistReminders { get; set; } = true;

    /// <summary>Gets or sets whether review reminder notifications are enabled.</summary>
    public bool NotificationReviewReminders { get; set; } = true;
}
```

JWT section in ShopFresherz.API/appsettings.json:
NOT FOUND: JWT section - may not have been implemented yet

JWT section in ShopFresherz.API/appsettings.Development.json:
NOT FOUND: JWT section - may not have been implemented yet

JWT section in ShopFresherz.API/appsettings.Production.json:
NOT FOUND: JWT section - may not have been implemented yet

## Report 6 - Database & Entities
### ShopFresherz.Infrastructure/Persistence/ShopFresherzDbContext.cs
Summary: EF Core database context.
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ShopFresherz.Domain.Common;
using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core DbContext for the ShopFresherz platform.
/// Applies all entity configurations from the Configurations folder,
/// auto-sets UpdatedAt on save, and enforces soft-delete global query filters.
/// </summary>
public class ShopFresherzDbContext : DbContext
{
    private IDbContextTransaction? _currentTransaction;

    /// <summary>Initialises a new instance of <see cref="ShopFresherzDbContext"/>.</summary>
    public ShopFresherzDbContext(DbContextOptions<ShopFresherzDbContext> options) : base(options)
    {
    }

    // â”€â”€ DbSets â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>Gets or sets the Users table.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Gets or sets the Products table.</summary>
    public DbSet<Product> Products => Set<Product>();

    /// <summary>Gets or sets the ProductImages table.</summary>
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

    /// <summary>Gets or sets the ProductVariants table.</summary>
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();

    /// <summary>Gets or sets the Categories table.</summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <summary>Gets or sets the Brands table.</summary>
    public DbSet<Brand> Brands => Set<Brand>();

    /// <summary>Gets or sets the Orders table.</summary>
    public DbSet<Order> Orders => Set<Order>();

    /// <summary>Gets or sets the OrderItems table.</summary>
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    /// <summary>Gets or sets the Carts table.</summary>
    public DbSet<Cart> Carts => Set<Cart>();

    /// <summary>Gets or sets the CartItems table.</summary>
    public DbSet<CartItem> CartItems => Set<CartItem>();

    /// <summary>Gets or sets the Reviews table.</summary>
    public DbSet<Review> Reviews => Set<Review>();

    /// <summary>Gets or sets the FlashDeals table.</summary>
    public DbSet<FlashDeal> FlashDeals => Set<FlashDeal>();

    /// <summary>Gets or sets the Coupons table.</summary>
    public DbSet<Coupon> Coupons => Set<Coupon>();

    /// <summary>Gets or sets the Addresses table.</summary>
    public DbSet<Address> Addresses => Set<Address>();

    /// <summary>Gets or sets the Wishlists table.</summary>
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();

    /// <summary>Gets or sets the LoyaltyTransactions table.</summary>
    public DbSet<LoyaltyTransaction> LoyaltyTransactions => Set<LoyaltyTransaction>();

    /// <summary>Gets or sets the NotifyRequests table.</summary>
    public DbSet<NotifyRequest> NotifyRequests => Set<NotifyRequest>();

    /// Gets or sets the AuditLogs table.</summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    /// <summary>Gets or sets the Notifications table.</summary>
    public DbSet<Notification> Notifications => Set<Notification>();

    /// <summary>Gets or sets the HomepageBanners table.</summary>
    public DbSet<HomepageBanner> HomepageBanners => Set<HomepageBanner>();

    /// <summary>Gets or sets the AppSettings table.</summary>
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    /// <summary>Gets or sets the SavedCards (bank cards) table.</summary>
    public DbSet<SavedCard> SavedCards => Set<SavedCard>();

    /// <summary>Gets or sets the FeaturedSections table.</summary>
    public DbSet<FeaturedSection> FeaturedSections => Set<FeaturedSection>();

    /// <summary>Gets or sets the PromotionalSections table.</summary>
    public DbSet<PromotionalSection> PromotionalSections => Set<PromotionalSection>();

    // â”€â”€ Model configuration â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration<T> implementations in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShopFresherzDbContext).Assembly);
    }

    // â”€â”€ SaveChanges overrides â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <inheritdoc />
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetAuditTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override int SaveChanges()
    {
        SetAuditTimestamps();
        return base.SaveChanges();
    }

    // â”€â”€ Transaction helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>Begins an explicit database transaction.</summary>
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            return; // already in a transaction
        }

        _currentTransaction = await Database.BeginTransactionAsync(cancellationToken);
    }

    /// <summary>Commits the current explicit transaction.</summary>
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No active transaction to commit.");
        }

        try
        {
            await SaveChangesAsync(cancellationToken);
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    /// <summary>Rolls back the current explicit transaction.</summary>
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            return;
        }

        try
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    // â”€â”€ Private helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void SetAuditTimestamps()
    {
        DateTime utcNow = DateTime.UtcNow;

        foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<BaseEntity> entry in
                 ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = utcNow;
            }
        }

        foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Category> entry in
                 ChangeTracker.Entries<Category>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = utcNow;
            }
        }
    }
}
```

### ShopFresherz.Domain/Entities/Product.cs
Summary: Domain entity definition.
```csharp
using ShopFresherz.Domain.Common;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShopFresherz.Domain.Entities;
/// <summary>
/// Core product entity â€” the central aggregate in the ShopFresherz catalog.
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

    /// <summary>Gets the quantity available for new cart additions (StockQty âˆ’ ReservedQty).</summary>
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
```

### ShopFresherz.Domain/Entities/ProductImage.cs
Summary: Domain entity definition.
```csharp
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

    /// <summary>Gets or sets the CDN URL for the 80Ã—80 LQIP thumbnail.</summary>
    public string ThumbUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the CDN URL for the 540Ã—540 main display image (WebP).</summary>
    public string DisplayUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the CDN URL for the 1600Ã—1600 zoom-source image (WebP).</summary>
    public string ZoomUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the CDN URL for the original upload (up to 4000Ã—4000).</summary>
    public string OriginalUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the display sort order (0-based, drag-drop in admin).</summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>Gets or sets a value indicating whether this slot holds a product video.</summary>
    public bool IsVideo { get; set; } = false;
}
```

### ShopFresherz.Domain/Entities/ProductVariant.cs
Summary: Domain entity definition.
```csharp
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

    /// <summary>Gets the quantity available for new orders (StockQty âˆ’ ReservedQty).</summary>
    public int AvailableQty => StockQty - ReservedQty;
}
```

### ShopFresherz.Infrastructure/Persistence/Configurations/ProductConfiguration.cs
Summary: EF Core entity configuration.
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Infrastructure.Persistence.Configurations;

/// <summary>EF Core fluent configuration for the <see cref="Product"/> entity.</summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SKU)
            .IsRequired()
            .HasMaxLength(100);
        builder.HasIndex(x => x.SKU).IsUnique();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(x => x.Slug)
            .IsRequired()
            .HasMaxLength(350);
        builder.HasIndex(x => x.Slug).IsUnique();

        builder.Property(x => x.Price)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.CompareAtPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.CostPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.AverageRating)
            .HasColumnType("decimal(3,2)")
            .HasDefaultValue(0m);

        builder.Property(x => x.WeightKg)
            .HasColumnType("decimal(8,3)");

        builder.Property(x => x.MetaTitle)
            .HasMaxLength(70);

        builder.Property(x => x.MetaDescription)
            .HasMaxLength(160);

        // Ignore computed property â€” not mapped to a column
        builder.Ignore(x => x.AvailableQty);

        builder.HasOne(x => x.Brand)
            .WithMany(b => b.Products)
            .HasForeignKey(x => x.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Images)
            .WithOne(i => i.Product)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Variants)
            .WithOne(v => v.Product)
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Reviews)
            .WithOne(r => r.Product)
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // OrderItems navigation exists on Product but no matching collection exists for
        // the other FK-dependent entities â€” configure explicitly to prevent EF Core from
        // creating duplicate shadow navigations.
        builder.HasMany(x => x.OrderItems)
            .WithOne(i => i.Product)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}
```

### ShopFresherz.Infrastructure/Persistence/Configurations/ProductImageConfiguration.cs
Summary: EF Core entity configuration.
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Infrastructure.Persistence.Configurations;

/// <summary>EF Core fluent configuration for the <see cref="ProductImage"/> entity.</summary>
public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ThumbUrl).IsRequired().HasMaxLength(500);
        builder.Property(x => x.DisplayUrl).IsRequired().HasMaxLength(500);
        builder.Property(x => x.ZoomUrl).IsRequired().HasMaxLength(500);
        builder.Property(x => x.OriginalUrl).IsRequired().HasMaxLength(500);

        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}
```

### ShopFresherz.Infrastructure/Persistence/Configurations/ProductVariantConfiguration.cs
Summary: EF Core entity configuration.
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Infrastructure.Persistence.Configurations;

/// <summary>EF Core fluent configuration for the <see cref="ProductVariant"/> entity.</summary>
public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SKU)
            .IsRequired()
            .HasMaxLength(100);
        builder.HasIndex(x => x.SKU).IsUnique();

        builder.Property(x => x.Price)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Ignore(x => x.AvailableQty);

        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}
```

### ShopFresherz.Infrastructure/Persistence/Migrations/20260517142653_AddProductImageUrlsJson.cs
Summary: Source/configuration file used by the audited backend.
```csharp
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopFresherz.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductImageUrlsJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "NotificationWishlistReminders",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "NotificationReviewReminders",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "NotificationPromotions",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "NotificationOrderUpdates",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "NotificationBackInStock",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrlsJson",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LinkUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropColumn(
                name: "ImageUrlsJson",
                table: "Products");

            migrationBuilder.AlterColumn<bool>(
                name: "NotificationWishlistReminders",
                table: "Users",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "NotificationReviewReminders",
                table: "Users",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "NotificationPromotions",
                table: "Users",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "NotificationOrderUpdates",
                table: "Users",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "NotificationBackInStock",
                table: "Users",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);
        }
    }
}
```

### ShopFresherz.Infrastructure/Persistence/Migrations/20260528101923_FixSlugIdFilteredUniqueIndex.cs
Summary: Source/configuration file used by the audited backend.
```csharp
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopFresherz.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixSlugIdFilteredUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PromotionalSections_SlugId",
                table: "PromotionalSections");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionalSections_SlugId",
                table: "PromotionalSections",
                column: "SlugId",
                unique: true,
                filter: "[DeletedAt] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PromotionalSections_SlugId",
                table: "PromotionalSections");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionalSections_SlugId",
                table: "PromotionalSections",
                column: "SlugId",
                unique: true);
        }
    }
}
```

Latest migration file name/date: ShopFresherz.Infrastructure/Persistence/Migrations/20260528101923_FixSlugIdFilteredUniqueIndex.cs

## Report 7 - Dependency Injection Summary
NOT FOUND: DependencyInjection.cs - may not have been implemented yet

NOT FOUND: DependencyInjection.cs - may not have been implemented yet

### ShopFresherz.API/Program.cs
Summary: API startup and middleware configuration.
```csharp
using System.Text.Json.Serialization;
using AspNetCoreRateLimit;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using ShopFresherz.API;
using ShopFresherz.Application.Common.Extensions;
using ShopFresherz.Domain.Interfaces.Services;
using ShopFresherz.Infrastructure.Configuration;
using ShopFresherz.Infrastructure.Extensions;
using ShopFresherz.Infrastructure.Jobs;
using ShopFresherz.Infrastructure.Persistence;
using ShopFresherz.Infrastructure.Services;
using System.Security.Cryptography;

// â”€â”€ Serilog bootstrap logger (captures startup errors before DI is ready) â”€â”€â”€â”€â”€â”€
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    // â”€â”€ Serilog â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    builder.Host.UseSerilog((ctx, services, config) =>
        config.ReadFrom.Configuration(ctx.Configuration)
              .ReadFrom.Services(services));

    // â”€â”€ Application (MediatR, FluentValidation, AutoMapper) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    builder.Services.AddApplication();

    // â”€â”€ Infrastructure (EF Core, repositories, services) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.Configure<PaystackOptions>(
        builder.Configuration.GetSection("Paystack"));
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
    }
    else
    {
        builder.Services.AddScoped<IFileStorageService, AzureBlobFileStorageService>();
    }

    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            new SqlServerStorageOptions
            {
                PrepareSchemaIfNecessary = true,
            }));

    builder.Services.AddHangfireServer(options =>
    {
        options.WorkerCount = 2;
        options.Queues = ["default", "critical"];
    });

    builder.Services.AddScoped<StockReservationExpiryJob>();
    builder.Services.AddScoped<FlashDealExpiryJob>();
    builder.Services.AddScoped<BackInStockNotificationJob>();

    builder.Services.AddMemoryCache();
    builder.Services.Configure<IpRateLimitOptions>(options =>
    {
        options.EnableEndpointRateLimiting = true;
        options.StackBlockedRequests = false;
        options.HttpStatusCode = StatusCodes.Status429TooManyRequests;
        options.RealIpHeader = "X-Real-IP";
        options.GeneralRules =
        [
            new RateLimitRule { Endpoint = "*", Period = "1m", Limit = 60 },
            new RateLimitRule { Endpoint = "post:/api/v1/auth/login", Period = "5m", Limit = 5 },
            new RateLimitRule { Endpoint = "post:/api/v1/auth/register", Period = "1h", Limit = 10 },
            new RateLimitRule { Endpoint = "post:/api/v1/auth/forgot-password", Period = "15m", Limit = 3 },
            new RateLimitRule { Endpoint = "post:/api/v1/auth/resend-otp", Period = "1h", Limit = 3 },
            new RateLimitRule { Endpoint = "get:/api/v1/search/instant", Period = "1m", Limit = 120 },
        ];
    });
    builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
    builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
    builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
    builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
    builder.Services.AddInMemoryRateLimiting();

    // â”€â”€ JWT RS256 authentication â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    string? privateKeyPem = builder.Configuration["Jwt:PrivateKeyPem"];
    string? publicKeyPem = builder.Configuration["Jwt:PublicKeyPem"];

    if (string.IsNullOrWhiteSpace(privateKeyPem) && string.IsNullOrWhiteSpace(publicKeyPem))
    {
        throw new InvalidOperationException("Jwt:PrivateKeyPem or Jwt:PublicKeyPem is not configured.");
    }

    RSA publicRsa = RSA.Create();
    if (!string.IsNullOrWhiteSpace(privateKeyPem))
    {
        privateKeyPem = privateKeyPem.Replace("\\n", "\n").Trim();
        publicRsa.ImportFromPem(privateKeyPem);
    }
    else
    {
        publicKeyPem = publicKeyPem!.Replace("\\n", "\n").Trim();
        publicRsa.ImportFromPem(publicKeyPem);
    }

    RsaSecurityKey rsaPublicKey = new(publicRsa);

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidIssuer              = builder.Configuration["Jwt:Issuer"],
                ValidateAudience         = true,
                ValidAudience            = builder.Configuration["Jwt:Audience"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey         = rsaPublicKey,
                ValidateLifetime         = true,
                ClockSkew                = TimeSpan.FromSeconds(30),
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        // NOTE: TokenService issues role claim using ClaimTypes.Role (standard MS role claim type).
        // TokenService uses ClaimTypes.Role, so authorization policies must check the same claim type.
        const string roleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

        options.AddPolicy("RequireCustomer", policy =>
            policy.RequireAuthenticatedUser()
                  .RequireClaim(roleClaimType, "Customer", "Admin", "SuperAdmin"));

        options.AddPolicy("RequireAdmin", policy =>
            policy.RequireAuthenticatedUser()
                  .RequireClaim(roleClaimType, "Admin", "SuperAdmin"));

        options.AddPolicy("RequireSuperAdmin", policy =>
            policy.RequireAuthenticatedUser()
                  .RequireClaim(roleClaimType, "SuperAdmin"));
    });

    // â”€â”€ Controllers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    builder.Services
        .AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

    // â”€â”€ CORS â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("ShopFresherzCors", policy =>
        {
            string[] origins = builder.Configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? Array.Empty<string>();

            if (builder.Environment.IsDevelopment())
            {
                policy.WithOrigins(
                        "http://localhost:3000",
                        "http://localhost:3001",
                        "https://localhost:3000")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            }
            else
            {
                policy.WithOrigins(origins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            }
        });
    });


    // â”€â”€ Swagger / OpenAPI â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title       = "ShopFresherz API",
            Version     = "v1",
            Description = "Nigerian tech/gadgets e-commerce platform API",
        });

        // JWT Bearer security definition
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name         = "Authorization",
            Type         = SecuritySchemeType.Http,
            Scheme       = "bearer",
            BearerFormat = "JWT",
            In           = ParameterLocation.Header,
            Description  = "Enter your JWT access token.",
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id   = "Bearer",
                    },
                },
                []
            },
        });

        // Include XML comments for Swagger documentation
        string xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    });

    // â”€â”€ Build and configure middleware pipeline â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    WebApplication app = builder.Build();

    // Apply pending EF Core migrations automatically on startup (must run before seeding).
    using (IServiceScope scope = app.Services.CreateScope())
    {
        ShopFresherzDbContext db = scope.ServiceProvider.GetRequiredService<ShopFresherzDbContext>();
        await db.Database.MigrateAsync();
    }

    // Seed baseline data (categories + admin + products etc.) for new/empty databases.
    // Each seed method is wrapped in try/catch so one failure won't crash the app.
    using (IServiceScope scope = app.Services.CreateScope())
    {
        ShopFresherzDbContext db = scope.ServiceProvider
            .GetRequiredService<ShopFresherzDbContext>();
        await DataSeeder.SeedAsync(db);
    }

    app.UseSerilogRequestLogging();

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ShopFresherz API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "ShopFresherz API";
    });

    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    // CORS must come before HTTPS redirect to handle preflight OPTIONS requests
    app.UseCors("ShopFresherzCors");

    // Short-circuit OPTIONS preflight requests to avoid CORS redirect issues
    app.Use(async (context, next) =>
    {
        if (context.Request.Method == "OPTIONS")
        {
            context.Response.StatusCode = 204;
            await context.Response.CompleteAsync();
            return;
        }
        await next();
    });

    app.UseHttpsRedirection();

    // Security headers middleware (production hardening)
    app.Use(async (context, next) =>

    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        if (!app.Environment.IsDevelopment())
        {
            context.Response.Headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' https://js.paystack.co; " +
                "style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data: https:; " +
                "connect-src 'self' https://api.paystack.co https://api.flutterwave.com;";
        }

        await next();
    });

    app.UseIpRateLimiting();

    app.UseAuthentication();
    app.UseAuthorization();

    IDashboardAuthorizationFilter hangfireAuth = app.Environment.IsDevelopment()
        ? new HangfireDevAuthFilter()
        : new HangfireProductionAuthFilter();

    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [hangfireAuth]
    });

    try
    {
        RecurringJob.AddOrUpdate<StockReservationExpiryJob>(
            "stock-reservation-expiry",
            job => job.ExecuteAsync(),
            "*/5 * * * *");

        RecurringJob.AddOrUpdate<FlashDealExpiryJob>(
            "flash-deal-expiry",
            job => job.ExecuteAsync(),
            "* * * * *");

        RecurringJob.AddOrUpdate<BackInStockNotificationJob>(
            "back-in-stock",
            job => job.ExecuteAsync(),
            "*/10 * * * *");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Hangfire recurring jobs could not be scheduled during startup.");
    }

    app.MapControllers();

    // Health endpoint for production smoke tests
    app.MapGet("/health", () => Results.Ok(new
    {
        status      = "healthy",
        timestamp   = DateTime.UtcNow,
        environment = app.Environment.EnvironmentName,
        version     = "1.0.0",
    })).AllowAnonymous();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "ShopFresherz API failed to start.");
}
finally
{
    Log.CloseAndFlush();
}
```


## Appendix - Corrected API Contract And Registration Notes
This appendix corrects/expands the generated notes above with explicit JSON shapes and actual registration files discovered after the full-file extraction.

### Exact JSON shape returned by GET /api/v1/products/{slug}
Enums are strings globally because `Program.cs` registers `JsonStringEnumConverter`; property names are camelCase.
```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "name": "string",
  "slug": "string",
  "price": 0,
  "compareAtPrice": 0,
  "primaryImageUrl": "string or null",
  "averageRating": 0,
  "reviewCount": 0,
  "isActive": true,
  "isFeatured": false,
  "availableQty": 0,
  "brand": { "id": "00000000-0000-0000-0000-000000000000", "name": "string", "slug": "string", "logoUrl": "string or null" },
  "category": { "id": 0, "parentId": 0, "name": "string", "slug": "string", "imageUrl": "string or null" },
  "imageUrls": ["https://example.com/image.webp"],
  "description": "string or null",
  "shortDescription": "string or null",
  "images": [{ "id": "00000000-0000-0000-0000-000000000000", "thumbUrl": "string", "displayUrl": "string", "zoomUrl": "string", "originalUrl": "string", "sortOrder": 0, "isVideo": false }],
  "variants": [{ "id": "00000000-0000-0000-0000-000000000000", "sku": "string", "attributes": [{ "name": "Color", "value": "Black" }], "price": 0, "stockQty": 0, "availableQty": 0 }],
  "attributesJson": "string or null",
  "tagsJson": "string or null",
  "weightKg": 0,
  "metaTitle": "string or null",
  "metaDescription": "string or null",
  "soldCount": 0,
  "sku": "string"
}
```

### Exact JSON shape expected by POST /api/v1/products
There is no `/api/v1/admin/products` controller route in the source; admin product create is `[Authorize(Policy = "RequireAdmin")] POST /api/v1/products`.
```json
{
  "sku": "string",
  "name": "string",
  "slug": "string or null",
  "brandId": "00000000-0000-0000-0000-000000000000",
  "categoryId": 0,
  "description": "string or null",
  "shortDescription": "string or null",
  "price": 0,
  "compareAtPrice": 0,
  "costPrice": 0,
  "stockQty": 0,
  "weightKg": 0,
  "attributesJson": "string or null",
  "tagsJson": "string or null",
  "isActive": true,
  "isFeatured": false,
  "imageUrls": ["https://example.com/image.webp"]
}
```
Required/validated: `sku`, `name`, `brandId`, `categoryId`, `price > 0`, `stockQty >= 0`, `imageUrls` non-null; each image URL, when present, must be an absolute URL.

### Exact JSON/form shape expected by POST /api/v1/products/{id}/images
There is no `/api/v1/admin/products/{id}/images` route in the source; admin image upload is `[Authorize(Policy = "RequireAdmin")] POST /api/v1/products/{id}/images`. It consumes `multipart/form-data`, not JSON.
```http
Content-Type: multipart/form-data

file=<binary JPG|PNG|WEBP file, max 8MB>
```
Response shape is `ProductImageDto`:
```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "thumbUrl": "string",
  "displayUrl": "string",
  "zoomUrl": "string",
  "originalUrl": "string",
  "sortOrder": 0,
  "isVideo": false
}
```

### Exact JSON shape expected by POST /api/v1/orders
```json
{
  "addressId": "00000000-0000-0000-0000-000000000000 or null",
  "inlineAddress": {
    "label": "string",
    "line1": "string",
    "line2": "string or null",
    "city": "string",
    "state": "string",
    "postalCode": "string or null"
  },
  "deliveryMethod": "Standard | Express | Pickup",
  "paymentMethod": "Card | BankTransfer | USSD | PayOnDelivery",
  "couponCode": "string or null",
  "notes": "string or null",
  "guestEmail": "guest@example.com or null",
  "guestSessionId": "string or null"
}
```
Validation/business rules: cart must exist and contain items; either `addressId` or `inlineAddress` is required; guest checkout requires `guestEmail`; stock is reserved by incrementing `Product.ReservedQty` or `ProductVariant.ReservedQty` during order placement.

### Payment webhook routes
- `POST /api/v1/payments/webhook/paystack` verifies `x-paystack-signature` with HMAC-SHA512 and sends `HandlePaystackWebhookCommand`.
- `POST /api/v1/payments/webhook/flutterwave` verifies `verif-hash`; successful `charge.completed` events are mapped into `HandlePaystackWebhookCommand("charge.success", ...)`.

### Corrected appsettings sections
#### ShopFresherz.API/appsettings.json
Jwt:
```json
{
    "Issuer":  "ShopFresherz",
    "Audience":  "ShopFresherz",
    "PrivateKeyPem":  "REPLACE_WITH_RSA_PRIVATE_KEY_PEM",
    "PublicKeyPem":  "REPLACE_WITH_RSA_PUBLIC_KEY_PEM"
}
```
Paystack:
```json
{
    "SecretKey":  "REPLACE_WITH_PAYSTACK_SECRET_KEY",
    "PublicKey":  "***MASKED***",
    "WebhookSecret":  "***MASKED***"
}
```
Flutterwave:
```json
{
    "SecretKey":  "",
    "PublicKey":  "***MASKED***",
    "CallbackUrl":  "https://shopfresherz.com/order/confirmed",
    "WebhookSecret":  "***MASKED***"
}
```
NOT FOUND: Cloudinary section - may not have been implemented yet

#### ShopFresherz.API/appsettings.Development.json
Jwt:
```json
{
    "Issuer":  "ShopFresherz",
    "Audience":  "ShopFresherz",
    "PrivateKeyPem":  "REPLACE_WITH_RSA_PRIVATE_KEY_PEM",
    "PublicKeyPem":  "REPLACE_WITH_RSA_PUBLIC_KEY_PEM"
}
```
NOT FOUND: Paystack section - may not have been implemented yet
NOT FOUND: Flutterwave section - may not have been implemented yet
NOT FOUND: Cloudinary section - may not have been implemented yet

#### ShopFresherz.API/appsettings.Production.json
Jwt:
```json
{
    "Issuer":  "shopfresherz.com",
    "Audience":  "shopfresherz.com",
    "PrivateKeyPem":  "-----BEGIN RSA PRIVATE KEY-----\nMIIEpQIBAAKCAQEA0CgqchjRgZM3I0YRwMxpyrpp219POgwjldBR7OJqcRC12sg8\nJxZYc6Fc1A8HT/2vPWr6Qwx39jn99Ms6shqigEZrd5wWuszX9Xd3metczM4mimxY\nKAiQYEo9hm+o7cflFlDYaa3npVHSakgMSc0hJNIHQQ6E7fcm62nYTFZG1fjQpQ6Q\nTs7sMTf45hY1oohhWpcbDKqiCRRD/2SkAgj2svYMlNLgiTKGewAjUnN7Uux27DrE\n5Dj81xfuYGipOg8kt5QYNREvTvwIbeyjAG0PDJHFMW2oxXeR3GZHsvNoLk/pDst0\nVMVzNii5OBN0Tgwgn6+Oxk0iThUDQmMqM43cmQIDAQABAoIBAQC95nbawpOXv8OE\nta8KkjJNlgVu8iOoVFy+l1+2WdU7s0AFCbn++t7VFrhGajOEoW6W/Jq2QpDKnJt4\nEVRKGgx4NEZkx4zg/HJk6Ym+bT+rRMNac391fmKgxfodRI775uqzkivtNY9ofh4V\nggdb6P08xzOBAYr6QglKqbR/6BIVr5UJlPnQEPfwojox/ykmQxHNKk6kIEdsK4JD\nEzwQcnD+IdqoEOTt/Tmg1UzEa4hthx9MQuR8mv5lCOSVqzYezcbVTRxt0r1IMy1V\nuHjkubWbTd8tNgYcM00fYRBL0004cmvuWILmcBaoTUIxHUdvFF09c88eB8wMIWQs\n2IMi/Z0VAoGBAOkd8ScuaYCWtQVTl0D592jkzVqG/60S3FcH2lioarIxfQ2JHDfs\nllUfn2Q+Up4uQRD8GNtUj7U/Bq3J4dPvNAiOuYhGZFAMT8rEgmRKS/Duo//zyTIC\nknK8TKiTq9uTKr8ZcBnrE1TjGLG5JV+GlmRle0ib2mdEkU96cgeCbZrjAoGBAOSW\n/ukFfrrFIqf9hB5rq7FItJI41SWKUb4a8qxxnXk9xUsQVvZN3Ci7ub9+wldVtt2K\nxCBlJESVdn7LdU8ZTLbZcNxHr/RaO7GGkjiGBG/umdEP8VdErTSFxNGyyV70kyWd\nDHYbduXD977ZueohsrbWY3BqDbUQgTAF1emSUNdTAoGBANnEtNCrVLdhEf3F1thG\nKs98XjGhRxtBMUDD/z8vJW0NOgr4+LltdApVHisg5UELaJEbT4Uk+5KPLAgRPbsk\nC/GqkrsxNuhod+mtpTlXBpYt+FpUPLFRIcm5Ccu5F6RpWFdoxgYQSeDUvO7ZTIZu\nTfO/sWlk3qP1s+ERO5C0UPuNAoGAexxXbWhoOG6zm4YOAdQ7Q9/EYHv7T5yPKbOZ\nvwXxqbp651KFg2GrkMSJ5Xh/cknXkrmJp2jRf55/kwpAw7j8d/3jqr9xJEj0nUe8\nuHR1L1NxxAayFC4ImqDwv39qtJlEJp3OeE2SW2yPoVnhpxc/CPmt1H1JpJqe+R4r\ndwA2ScECgYEAq6O7DRSfd7JueYPumUHc1hDmzh9Z/vgPaEmosrXXWj3X620sPuTp\n0ucsYpkKMyismboW0fybDZPJZDziukSgIns6Zup0Ni798HiTwNfrzRVT+hpBW/PO\nxiUsrNx6j8uDRj61VyRHFOvDyM/2wUmP2YKkBmtvoBWX480oaf/y6D4=\n-----END RSA PRIVATE KEY-----",
    "PublicKeyPem":  "-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA0CgqchjRgZM3I0YRwMxp\nyrpp219POgwjldBR7OJqcRC12sg8JxZYc6Fc1A8HT/2vPWr6Qwx39jn99Ms6shqi\ngEZrd5wWuszX9Xd3metczM4mimxYKAiQYEo9hm+o7cflFlDYaa3npVHSakgMSc0h\nJNIHQQ6E7fcm62nYTFZG1fjQpQ6QTs7sMTf45hY1oohhWpcbDKqiCRRD/2SkAgj2\nsvYMlNLgiTKGewAjUnN7Uux27DrE5Dj81xfuYGipOg8kt5QYNREvTvwIbeyjAG0P\nDJHFMW2oxXeR3GZHsvNoLk/pDst0VMVzNii5OBN0Tgwgn6+Oxk0iThUDQmMqM43c\nmQIDAQAB\n-----END PUBLIC KEY-----",
    "AccessTokenExpiryMinutes":  15,
    "RefreshTokenExpiryDays":  7
}
```
Paystack:
```json
{
    "SecretKey":  "",
    "PublicKey":  "***MASKED***",
    "WebhookSecret":  "***MASKED***"
}
```
Flutterwave:
```json
{
    "SecretKey":  "",
    "PublicKey":  "***MASKED***",
    "CallbackUrl":  "https://shopfresherz.com/order/confirmed",
    "WebhookSecret":  "***MASKED***"
}
```
NOT FOUND: Cloudinary section - may not have been implemented yet

### Actual service registration files
#### ShopFresherz.Infrastructure/Extensions/ServiceCollectionExtensions.cs
Summary: Actual DI extension file used by this codebase instead of `DependencyInjection.cs`.
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using ShopFresherz.Domain.Interfaces;
using ShopFresherz.Domain.Interfaces.Repositories;
using ShopFresherz.Domain.Interfaces.Services;
using ShopFresherz.Infrastructure.Configuration;
using ShopFresherz.Infrastructure.Persistence;
using ShopFresherz.Infrastructure.Persistence.Repositories;
using ShopFresherz.Infrastructure.Search;
using ShopFresherz.Infrastructure.Services;

namespace ShopFresherz.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering Infrastructure layer services with the DI container.
/// Call <see cref="AddInfrastructure"/> from the API layer's Program.cs.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the ShopFresherzDbContext, repositories, Unit of Work, and all
    /// Infrastructure services (token, cache, email, SMS, file storage, image processing, payments).
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // â”€â”€ EF Core â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        services.AddDbContext<ShopFresherzDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(ShopFresherzDbContext).Assembly.FullName)),
            optionsLifetime: ServiceLifetime.Singleton);
        services.AddDbContextFactory<ShopFresherzDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(ShopFresherzDbContext).Assembly.FullName)));

        // â”€â”€ Unit of Work â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // â”€â”€ Redis / Cache â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        string? redisConnection = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redisConnection))
        {
            redisConnection = configuration["Redis:ConnectionString"];
        }

        if (string.IsNullOrWhiteSpace(redisConnection))
        {
            // No Redis configured â€” fall back to in-memory cache (already registered in Program.cs).
            services.AddSingleton<ICacheService, MemoryCacheService>();
        }
        else
        {
            try
            {
                services.AddSingleton<IConnectionMultiplexer>(
                    _ => ConnectionMultiplexer.Connect(redisConnection));
                services.AddSingleton<ICacheService, RedisCacheService>();
            }
            catch
            {
                // If Redis connection fails, fall back to in-memory cache.
                services.AddSingleton<ICacheService, MemoryCacheService>();
            }
        }

        // â”€â”€ HTTP clients for third-party services â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        services.AddHttpClient(nameof(PaystackPaymentService));
        services.Configure<FlutterwaveOptions>(configuration.GetSection("Flutterwave"));
        services.AddHttpClient<IFlutterwavePaymentService, FlutterwavePaymentService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        // â”€â”€ Token / Auth â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();

        // â”€â”€ Email â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        services.AddTransient<IEmailService, SendGridEmailService>();
        services.AddScoped<IAuditLogService, EfAuditLogService>();

        // â”€â”€ SMS â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        services.Configure<TermiiOptions>(configuration.GetSection("Termii"));
        services.AddHttpClient<ISmsService, TermiiSmsService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        // â”€â”€ Image processing â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        services.AddScoped<IImageProcessingService, ImageProcessingService>();

        // â”€â”€ Search â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        services.Configure<ElasticsearchOptions>(configuration.GetSection("Elasticsearch"));
        services.AddScoped<ISearchService, ElasticsearchService>();

        // â”€â”€ Notifications â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        services.AddScoped<INotificationRepository, EfNotificationRepository>();

        // â”€â”€ Payment gateway â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        services.AddTransient<IPaymentService, PaystackPaymentService>();

        // â”€â”€ Chatbot â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        services.AddScoped<IChatbotService, ChatbotService>();

        return services;
    }
}
```

#### ShopFresherz.Application/Common/Extensions/ServiceCollectionExtensions.cs
Summary: Actual DI extension file used by this codebase instead of `DependencyInjection.cs`.
```csharp
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ShopFresherz.Application.Common.Mappings;
using System.Reflection;

namespace ShopFresherz.Application.Common.Extensions;

/// <summary>Extension methods for registering Application layer services in the DI container.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers MediatR handlers, FluentValidation validators, AutoMapper, and the
    /// <see cref="ValidationBehavior{TRequest,TResponse}"/> pipeline behaviour.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        Assembly assembly = typeof(ServiceCollectionExtensions).Assembly;

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
        });

        services.AddValidatorsFromAssembly(assembly);

        services.AddAutoMapper(typeof(MappingProfile));

        return services;
    }
}
```


