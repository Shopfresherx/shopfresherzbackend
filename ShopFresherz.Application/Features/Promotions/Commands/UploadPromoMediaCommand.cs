using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Domain.Interfaces;
using ShopFresherz.Domain.Interfaces.Services;

namespace ShopFresherz.Application.Features.Promotions.Commands;

/// <summary>Command for uploading a media file (image or video) for a promotional section.</summary>
public sealed record UploadPromoMediaCommand(
    Guid SectionId,
    Stream FileStream,
    string FileName,
    long FileSizeBytes,
    string ContentType) : IRequest<Result<string>>;

/// <summary>Handler for <see cref="UploadPromoMediaCommand"/>.</summary>
public sealed class UploadPromoMediaCommandHandler
    : IRequestHandler<UploadPromoMediaCommand, Result<string>>
{
    private const long MaxFileSizeBytes = 50 * 1024 * 1024;
    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };
    private static readonly HashSet<string> AllowedVideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".webm", ".mov", ".avi"
    };
    private static readonly HashSet<string> AllowedVideoContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "video/mp4", "video/webm", "video/quicktime", "video/x-msvideo"
    };

    private readonly IUnitOfWork _uow;
    private readonly IFileStorageService _storage;

    public UploadPromoMediaCommandHandler(IUnitOfWork uow, IFileStorageService storage)
    {
        _uow = uow;
        _storage = storage;
    }

    public async Task<Result<string>> Handle(
        UploadPromoMediaCommand cmd,
        CancellationToken cancellationToken)
    {
        if (cmd.FileSizeBytes > MaxFileSizeBytes)
        {
            return Error.Validation("Media file size must be 50MB or less.");
        }

        string extension = Path.GetExtension(cmd.FileName);
        bool isImage = AllowedImageExtensions.Contains(extension);
        bool isVideo = AllowedVideoExtensions.Contains(extension) || AllowedVideoContentTypes.Contains(cmd.ContentType);

        if (!isImage && !isVideo)
        {
            return Error.Validation("Media must be an image (jpg, png, webp, gif) or video (mp4, webm, mov, avi).");
        }

        var section = await _uow.PromotionalSections.GetByIdAsync(cmd.SectionId, cancellationToken);
        if (section is null)
        {
            return Error.NotFound("Promotional section");
        }

        string contentType = ResolveContentType(cmd.ContentType, extension);
        string blobPath = $"promo-media/{cmd.SectionId:N}/{Guid.NewGuid():N}{extension}";

        string url = await _storage.UploadAsync(cmd.FileStream, blobPath, contentType, cancellationToken);

        if (isImage)
        {
            section.ImageUrl = url;
        }
        else if (isVideo)
        {
            section.VideoUrl = url;
        }

        section.UpdatedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<string>.Success(url);
    }

    private static string ResolveContentType(string contentType, string extension)
    {
        if (!string.IsNullOrWhiteSpace(contentType) &&
            !string.Equals(contentType, "application/octet-stream", StringComparison.OrdinalIgnoreCase))
        {
            return contentType;
        }

        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png"             => "image/png",
            ".webp"            => "image/webp",
            ".gif"             => "image/gif",
            ".mp4"             => "video/mp4",
            ".webm"            => "video/webm",
            ".mov"             => "video/quicktime",
            ".avi"             => "video/x-msvideo",
            _                    => "application/octet-stream",
        };
    }
}
