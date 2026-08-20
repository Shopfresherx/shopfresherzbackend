using FluentValidation;
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
    private const long MaxRemoteImageSizeBytes = 8 * 1024 * 1024;
    private static readonly HttpClient ImageDownloadClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
    };

    private readonly IUnitOfWork _uow;
    private readonly ISearchService _search;
    private readonly IImageProcessingService _imageProcessing;

    /// <summary>Initialises the handler.</summary>
    public UpdateProductCommandHandler(
        IUnitOfWork uow,
        ISearchService search,
        IImageProcessingService imageProcessing)
    {
        _uow = uow;
        _search = search;
        _imageProcessing = imageProcessing;
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

        if (req.ImageUrls is not null)
        {
            Result<IReadOnlyList<ProductImage>> replacementImages =
                await BuildReplacementImagesAsync(product, req.ImageUrls, cancellationToken);

            if (!replacementImages.IsSuccess)
            {
                return replacementImages.Error;
            }

            product.Images.Clear();
            foreach (ProductImage image in replacementImages.Value!)
            {
                product.Images.Add(image);
            }
        }

        if (req.InitialRating > 0) product.AverageRating = req.InitialRating;
        if (req.InitialReviewCount > 0) product.ReviewCount = req.InitialReviewCount;

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

    private async Task<Result<IReadOnlyList<ProductImage>>> BuildReplacementImagesAsync(
        Product product,
        IEnumerable<string> imageUrls,
        CancellationToken cancellationToken)
    {
        List<string> normalizedUrls = NormalizeImageUrls(imageUrls);
        List<ProductImage> existingImages = product.Images.ToList();
        List<ProductImage> replacementImages = new();

        for (int i = 0; i < normalizedUrls.Count; i++)
        {
            string imageUrl = normalizedUrls[i];

            ProductImage? existingImage = existingImages
                .FirstOrDefault(image => ProductImageFactory.MatchesAnyDerivative(image, imageUrl));

            if (existingImage is not null)
            {
                replacementImages.Add(ProductImageFactory.CopyForReplacement(existingImage, i));
                continue;
            }

            if (ProductImageFactory.TryCreateFromProcessedBackendUrl(
                    product.Id,
                    imageUrl,
                    i,
                    out ProductImage? processedBackendImage)
                && processedBackendImage is not null)
            {
                replacementImages.Add(processedBackendImage);
                continue;
            }

            if (!TryCreateHttpUri(imageUrl, out Uri? uri) || uri is null)
            {
                return Error.Validation($"Image URL '{imageUrl}' must be an absolute HTTP(S) URL.");
            }

            try
            {
                await using MemoryStream imageStream =
                    await DownloadImageAsync(uri, cancellationToken);

                ProductImageUrls urls = await _imageProcessing.ProcessAndUploadAsync(
                    imageStream,
                    GetFileName(uri),
                    product.Id,
                    cancellationToken);

                replacementImages.Add(ProductImageFactory.Create(product.Id, urls, i));
            }
            catch (Exception ex)
            {
                return Error.Validation($"Unable to process image URL '{imageUrl}': {ex.Message}");
            }
        }

        return Result<IReadOnlyList<ProductImage>>.Success(replacementImages);
    }

    private static List<string> NormalizeImageUrls(IEnumerable<string> imageUrls)
    {
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        List<string> normalized = new();

        foreach (string? imageUrl in imageUrls)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                continue;
            }

            string trimmed = imageUrl.Trim();
            if (seen.Add(trimmed))
            {
                normalized.Add(trimmed);
            }
        }

        return normalized;
    }

    private static bool TryCreateHttpUri(string imageUrl, out Uri? uri)
    {
        return Uri.TryCreate(imageUrl, UriKind.Absolute, out uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static async Task<MemoryStream> DownloadImageAsync(
        Uri uri,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await ImageDownloadClient.GetAsync(
            uri,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"download failed with HTTP {(int)response.StatusCode} {response.ReasonPhrase}.");
        }

        if (response.Content.Headers.ContentLength is > MaxRemoteImageSizeBytes)
        {
            throw new InvalidOperationException("image file size must be 8MB or less.");
        }

        await using Stream responseStream =
            await response.Content.ReadAsStreamAsync(cancellationToken);

        MemoryStream memoryStream = new();
        byte[] buffer = new byte[81920];
        long totalBytes = 0;

        while (true)
        {
            int bytesRead = await responseStream.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0)
            {
                break;
            }

            totalBytes += bytesRead;
            if (totalBytes > MaxRemoteImageSizeBytes)
            {
                await memoryStream.DisposeAsync();
                throw new InvalidOperationException("image file size must be 8MB or less.");
            }

            await memoryStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
        }

        memoryStream.Position = 0;
        return memoryStream;
    }

    private static string GetFileName(Uri uri)
    {
        string fileName = Path.GetFileName(uri.AbsolutePath);
        return string.IsNullOrWhiteSpace(fileName)
            ? "product-image"
            : fileName;
    }
}

/// <summary>Validator for <see cref="UpdateProductCommand"/>.</summary>
public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Request.InitialRating).InclusiveBetween(0, 5)
            .WithMessage("InitialRating must be between 0 and 5.");

        RuleForEach(x => x.Request.ImageUrls)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Each imageUrl must be non-empty.")
            .Must(BeValidImageUrl)
            .WithMessage("Each imageUrl must be an absolute HTTP(S) URL or a /uploads/products/... URL.")
            .When(x => x.Request.ImageUrls is not null);
    }

    private static bool BeValidImageUrl(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return false;
        }

        string trimmed = imageUrl.Trim();
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out Uri? uri))
        {
            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        }

        return trimmed.StartsWith("/uploads/products/", StringComparison.OrdinalIgnoreCase);
    }
}
