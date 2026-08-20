using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ShopFresherz.Domain.Interfaces.Services;

namespace ShopFresherz.Infrastructure.Services;

/// <summary>
/// Azure Blob Storage implementation of <see cref="IFileStorageService"/>.
/// All product images are stored under the configured container.
/// Returns a public CDN URL after upload.
/// </summary>
public sealed class AzureBlobFileStorageService : IFileStorageService
{
    private readonly BlobContainerClient _container;
    private readonly string              _cdnBaseUrl;
    private readonly ILogger<AzureBlobFileStorageService> _logger;

    /// <summary>Initialises a new instance of <see cref="AzureBlobFileStorageService"/>.</summary>
    public AzureBlobFileStorageService(IConfiguration configuration, ILogger<AzureBlobFileStorageService> logger)
    {
        string? connectionString = configuration["Azure:BlobStorage:ConnectionString"]
            ?? Environment.GetEnvironmentVariable("AZURE_BLOB_STORAGE_CONNECTION_STRING")
            ?? Environment.GetEnvironmentVariable("Azure__BlobStorage__ConnectionString");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Azure:BlobStorage:ConnectionString is not configured.");
        }

        string containerName = configuration["Azure:BlobStorage:ContainerName"] ?? "shopfresherz-media";

        _cdnBaseUrl = configuration["Azure:BlobStorage:CdnBaseUrl"] ?? string.Empty;
        _container  = new BlobContainerClient(connectionString, containerName);
        _logger     = logger;
    }

    /// <inheritdoc />
    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        await _container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        BlobClient blob = _container.GetBlobClient(fileName);

        BlobUploadOptions options = new()
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
        };

        await blob.UploadAsync(stream, options, cancellationToken);

        string blobUrl = blob.Uri.ToString();

        // If a CDN base URL is configured, substitute the storage host with it.
        if (!string.IsNullOrWhiteSpace(_cdnBaseUrl))
        {
            blobUrl = $"{_cdnBaseUrl.TrimEnd('/')}/{fileName}";
        }

        return blobUrl;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        // Extract the blob name from the full URL or use as-is if it's already a path.
        string blobName = ExtractBlobName(fileUrl);

        BlobClient blob = _container.GetBlobClient(blobName);
        await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string fileName, CancellationToken cancellationToken = default)
    {
        BlobClient blob = _container.GetBlobClient(fileName);
        Azure.Response<bool> response = await blob.ExistsAsync(cancellationToken);
        return response.Value;
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private string ExtractBlobName(string fileUrl)
    {
        if (Uri.TryCreate(fileUrl, UriKind.Absolute, out Uri? uri))
        {
            // Path format: /container-name/blob-name — skip the container segment.
            string[] segments = uri.AbsolutePath.TrimStart('/').Split('/', 2);
            return segments.Length == 2 ? segments[1] : uri.AbsolutePath.TrimStart('/');
        }

        return fileUrl;
    }
}
