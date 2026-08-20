using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces.Services;

namespace ShopFresherz.Application.Features.Products.Commands;

internal static class ProductImageFactory
{
    private static readonly string[] ProcessedDerivativeFileNames =
    [
        "thumb.webp",
        "display.webp",
        "zoom.webp",
        "original.webp",
    ];

    public static ProductImage Create(Guid productId, ProductImageUrls urls, int sortOrder) =>
        new()
        {
            ProductId = productId,
            ThumbUrl = urls.ThumbUrl,
            DisplayUrl = urls.DisplayUrl,
            ZoomUrl = urls.ZoomUrl,
            OriginalUrl = urls.OriginalUrl,
            SortOrder = sortOrder,
            IsVideo = false,
        };

    public static ProductImage CopyForReplacement(ProductImage source, int sortOrder) =>
        new()
        {
            ProductId = source.ProductId,
            ThumbUrl = source.ThumbUrl,
            DisplayUrl = source.DisplayUrl,
            ZoomUrl = source.ZoomUrl,
            OriginalUrl = source.OriginalUrl,
            SortOrder = sortOrder,
            IsVideo = source.IsVideo,
        };

    public static bool MatchesAnyDerivative(ProductImage image, string imageUrl)
    {
        return UrlEquals(image.ThumbUrl, imageUrl)
            || UrlEquals(image.DisplayUrl, imageUrl)
            || UrlEquals(image.ZoomUrl, imageUrl)
            || UrlEquals(image.OriginalUrl, imageUrl);
    }

    public static bool TryCreateFromProcessedBackendUrl(
        Guid productId,
        string imageUrl,
        int sortOrder,
        out ProductImage? image)
    {
        image = null;

        string urlWithoutQuery = StripQueryAndFragment(imageUrl.Trim());
        string path = ExtractPath(urlWithoutQuery);
        string marker = $"/products/{productId:D}/images/";

        if (!path.Contains(marker, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string fileName = LastPathSegment(path);
        if (!ProcessedDerivativeFileNames.Contains(fileName, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        int lastSlashIndex = urlWithoutQuery.LastIndexOf('/');
        if (lastSlashIndex < 0)
        {
            return false;
        }

        string baseUrl = urlWithoutQuery[..(lastSlashIndex + 1)];
        image = Create(
            productId,
            new ProductImageUrls(
                ThumbUrl: $"{baseUrl}thumb.webp",
                DisplayUrl: $"{baseUrl}display.webp",
                ZoomUrl: $"{baseUrl}zoom.webp",
                OriginalUrl: $"{baseUrl}original.webp"),
            sortOrder);

        return true;
    }

    private static bool UrlEquals(string left, string right)
    {
        return string.Equals(
            StripQueryAndFragment(left).Trim(),
            StripQueryAndFragment(right).Trim(),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtractPath(string value)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out Uri? uri))
        {
            return uri.AbsolutePath;
        }

        return value;
    }

    private static string LastPathSegment(string value)
    {
        int lastSlashIndex = value.LastIndexOf('/');
        return lastSlashIndex >= 0 ? value[(lastSlashIndex + 1)..] : value;
    }

    private static string StripQueryAndFragment(string value)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out Uri? uri))
        {
            UriBuilder builder = new(uri)
            {
                Query = string.Empty,
                Fragment = string.Empty,
            };

            return builder.Uri.ToString();
        }

        int queryIndex = value.IndexOf('?');
        int fragmentIndex = value.IndexOf('#');

        int cutIndex = queryIndex >= 0 && fragmentIndex >= 0
            ? Math.Min(queryIndex, fragmentIndex)
            : Math.Max(queryIndex, fragmentIndex);

        return cutIndex >= 0 ? value[..cutIndex] : value;
    }
}
