namespace ShopFresherz.Domain.Interfaces.Services;

/// <summary>
/// Contract for cloud file storage operations (Azure Blob / AWS S3).
/// All product image derivatives are stored under /products/{productId}/images/.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Uploads a file stream and returns the public CDN URL.
    /// </summary>
    /// <param name="stream">The file content stream.</param>
    /// <param name="fileName">The target blob name including path.</param>
    /// <param name="contentType">The MIME type of the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The public CDN URL of the uploaded file.</returns>
    Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default);

    /// <summary>Deletes a file by its CDN URL or blob name.</summary>
    Task DeleteAsync(string fileUrl, CancellationToken cancellationToken = default);

    /// <summary>Checks whether a blob exists at the given path.</summary>
    Task<bool> ExistsAsync(string fileName, CancellationToken cancellationToken = default);
}
