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

        ProductImage image = ProductImageFactory.Create(product.Id, urls, product.Images.Count);

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
