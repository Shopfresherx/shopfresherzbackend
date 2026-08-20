namespace ShopFresherz.Domain.Interfaces.Services;

/// <summary>
/// Contract for server-side image processing using ImageSharp.
/// On upload: rejects only images below a 200×200px hard floor (too small to be usable)
/// and files above 8MB. Images with a smaller side below 800px are proportionally
/// upscaled (Lanczos3, aspect ratio preserved, never cropped or distorted) to 800px
/// before four WebP derivatives are generated: 80px, 540px, 1600px, and original.
/// </summary>
public interface IImageProcessingService
{
    /// <summary>
    /// Validates the uploaded image meets minimum requirements.
    /// </summary>
    /// <param name="stream">The raw upload stream.</param>
    /// <returns>True if valid; false if below the 200×200px sanity floor or above 8MB.</returns>
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
