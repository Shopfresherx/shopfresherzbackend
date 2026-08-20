using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using ShopFresherz.Domain.Interfaces.Services;

namespace ShopFresherz.Infrastructure.Services;

/// <summary>ImageSharp implementation of the product image zoom pipeline.</summary>
public sealed class ImageProcessingService : IImageProcessingService
{
    private const int TargetMinDimension = 800;
    private const int SanityFloorDimension = 200;
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

        // Anything at or above the sanity floor is valid — images below TargetMinDimension
        // are auto-upscaled during derivative generation rather than rejected here.
        return info is not null &&
               info.Width >= SanityFloorDimension &&
               info.Height >= SanityFloorDimension;
    }

    /// <inheritdoc />
    public async Task<ImageDerivatives> GenerateDerivativesAsync(
        Stream sourceStream,
        CancellationToken cancellationToken = default)
    {
        if (sourceStream.CanSeek) sourceStream.Position = 0;
        using Image source = await Image.LoadAsync(sourceStream, cancellationToken);

        EnsureAboveSanityFloor(source);
        UpscaleToMinimumDimension(source);

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

        EnsureAboveSanityFloor(sourceImage);
        UpscaleToMinimumDimension(sourceImage);

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
                    Sampler = KnownResamplers.Lanczos3,
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
                Sampler = KnownResamplers.Lanczos3,
            }));

        MemoryStream output = new();
        await resized.SaveAsWebpAsync(output, new WebpEncoder { Quality = quality }, cancellationToken);
        output.Position = 0;
        return output;
    }

    /// <summary>
    /// Rejects genuinely unusable source images below the hard sanity floor — too small to
    /// produce anything usable even after upscaling.
    /// </summary>
    private static void EnsureAboveSanityFloor(Image source)
    {
        if (source.Width < SanityFloorDimension || source.Height < SanityFloorDimension)
        {
            throw new InvalidOperationException(
                $"Image is too small to use — minimum {SanityFloorDimension}x{SanityFloorDimension}px required. " +
                $"Uploaded: {source.Width}x{source.Height}px.");
        }
    }

    /// <summary>
    /// If the image's smaller side is below <see cref="TargetMinDimension"/>, upscales it in
    /// place — proportionally, preserving aspect ratio, Lanczos3 resampling — so the smaller
    /// side becomes exactly <see cref="TargetMinDimension"/>. No-op for images already at or
    /// above the target. Never crops or distorts.
    /// </summary>
    private static void UpscaleToMinimumDimension(Image source)
    {
        int smallerSide = Math.Min(source.Width, source.Height);
        if (smallerSide >= TargetMinDimension) return;

        double scale = (double)TargetMinDimension / smallerSide;
        int newWidth = (int)Math.Round(source.Width * scale);
        int newHeight = (int)Math.Round(source.Height * scale);

        source.Mutate(ctx => ctx.Resize(newWidth, newHeight, KnownResamplers.Lanczos3));
    }
}
