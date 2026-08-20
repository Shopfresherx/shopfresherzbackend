using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using ShopFresherz.Domain.Interfaces.Services;

namespace ShopFresherz.Infrastructure.Services;

/// <summary>
/// Server-side image processing using SixLabors.ImageSharp.
/// Validates min 2000×2000 px, max 8 MB, then produces four WebP derivatives:
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

        // Thumb — 80px longest edge, lossy WebP
        using (Image thumbImage = source.Clone(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(80, 80),
            Mode = ResizeMode.Max,
        })))
        {
            await thumbImage.SaveAsync(thumb80, HighQualityEncoder, cancellationToken);
        }

        // Display — 540px longest edge, lossy WebP
        using (Image displayImage = source.Clone(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(540, 540),
            Mode = ResizeMode.Max,
        })))
        {
            await displayImage.SaveAsync(display540, HighQualityEncoder, cancellationToken);
        }

        // Zoom — 1600px longest edge, lossy WebP
        using (Image zoomImage = source.Clone(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(1600, 1600),
            Mode = ResizeMode.Max,
        })))
        {
            await zoomImage.SaveAsync(zoom1600, HighQualityEncoder, cancellationToken);
        }

        // Original — lossless WebP (re-encoded to strip metadata)
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
