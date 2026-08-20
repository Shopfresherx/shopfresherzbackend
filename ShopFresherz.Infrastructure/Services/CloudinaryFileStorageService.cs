using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ShopFresherz.Domain.Interfaces.Services;

namespace ShopFresherz.Infrastructure.Services;

/// <summary>
/// Cloudinary implementation of <see cref="IFileStorageService"/>.
/// Used in production when Cloudinary environment variables are configured.
/// </summary>
public sealed class CloudinaryFileStorageService : IFileStorageService
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryFileStorageService> _logger;

    /// <summary>Initialises a new instance of <see cref="CloudinaryFileStorageService"/>.</summary>
    public CloudinaryFileStorageService(IConfiguration configuration, ILogger<CloudinaryFileStorageService> logger)
    {
        string cloudName = configuration["Cloudinary:CloudName"]
            ?? Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME")
            ?? throw new InvalidOperationException("Cloudinary CloudName is not configured.");

        string apiKey = configuration["Cloudinary:ApiKey"]
            ?? Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY")
            ?? throw new InvalidOperationException("Cloudinary ApiKey is not configured.");

        string apiSecret = configuration["Cloudinary:ApiSecret"]
            ?? Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET")
            ?? throw new InvalidOperationException("Cloudinary ApiSecret is not configured.");

        _cloudinary = new Cloudinary(new Account(cloudName, apiKey, apiSecret))
        {
            Api = { Secure = true }
        };

        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        string publicId = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrWhiteSpace(publicId))
        {
            publicId = Guid.NewGuid().ToString("N");
        }

        if (IsVideo(contentType, fileName))
        {
            VideoUploadParams videoUploadParams = new()
            {
                File = new FileDescription(publicId, stream),
                PublicId = publicId,
                Overwrite = true,
                UseFilename = true,
                UniqueFilename = false,
            };

            VideoUploadResult videoResult = await _cloudinary.UploadAsync(videoUploadParams, cancellationToken);

            if (videoResult.StatusCode != System.Net.HttpStatusCode.OK &&
                videoResult.StatusCode != System.Net.HttpStatusCode.Created)
            {
                string error = videoResult.Error?.Message ?? "Unknown Cloudinary error";
                _logger.LogError("Cloudinary video upload failed: {Error}", error);
                throw new InvalidOperationException($"Cloudinary video upload failed: {error}");
            }

            return videoResult.SecureUrl?.ToString() ?? videoResult.Url?.ToString()
                ?? throw new InvalidOperationException("Cloudinary returned an empty URL.");
        }

        ImageUploadParams uploadParams = new()
        {
            File = new FileDescription(publicId, stream),
            PublicId = publicId,
            Overwrite = true,
            UseFilename = true,
            UniqueFilename = false,
        };

        ImageUploadResult result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

        if (result.StatusCode != System.Net.HttpStatusCode.OK &&
            result.StatusCode != System.Net.HttpStatusCode.Created)
        {
            string error = result.Error?.Message ?? "Unknown Cloudinary error";
            _logger.LogError("Cloudinary upload failed: {Error}", error);
            throw new InvalidOperationException($"Cloudinary upload failed: {error}");
        }

        return result.SecureUrl?.ToString() ?? result.Url?.ToString()
            ?? throw new InvalidOperationException("Cloudinary returned an empty URL.");
    }

    private static bool IsVideo(string contentType, string fileName) =>
        contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase) ||
        Path.GetExtension(fileName) is ".mp4" or ".webm" or ".mov" or ".avi";

    /// <inheritdoc />
    public async Task DeleteAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            string publicId = ExtractPublicId(fileUrl);
            if (string.IsNullOrEmpty(publicId))
            {
                return;
            }

            DeletionParams deletionParams = new(publicId)
            {
                Invalidate = true
            };

            DeletionResult result = await _cloudinary.DestroyAsync(deletionParams);
            if (result.Result != "ok" && result.Result != "not found")
            {
                _logger.LogWarning("Cloudinary delete failed for {PublicId}: {Result}", publicId, result.Result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cloudinary delete failed for {FileUrl}", fileUrl);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            string publicId = Path.GetFileNameWithoutExtension(fileName);
            if (string.IsNullOrWhiteSpace(publicId))
            {
                return false;
            }

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cloudinary exists check failed for {FileName}", fileName);
            return false;
        }
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private static string? ExtractPublicId(string fileUrl)
    {
        if (Uri.TryCreate(fileUrl, UriKind.Absolute, out Uri? uri))
        {
            string[] segments = uri.AbsolutePath.TrimStart('/').Split('/', 2);
            if (segments.Length >= 2)
            {
                string pathWithoutCloudName = segments[1];
                string withoutVersion = System.Text.RegularExpressions.Regex.Replace(
                    pathWithoutCloudName, @"^v\d+/", string.Empty);
                string withoutExtension = Path.Combine(
                    Path.GetDirectoryName(withoutVersion) ?? string.Empty,
                    Path.GetFileNameWithoutExtension(withoutVersion));
                return withoutExtension.Replace('\\', '/').Replace('/', '/');
            }
        }

        return fileUrl;
    }
}
