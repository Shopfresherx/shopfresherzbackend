using System.Globalization;
using System.Text;
using Microsoft.VisualBasic.FileIO;
using MediatR;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Products.Commands;

public sealed record ImportProductsCsvCommand(byte[] Content) : IRequest<ImportProductsCsvResult>;

public sealed record ImportProductsCsvResult(
    int Created,
    int Updated,
    int Failed,
    IReadOnlyList<string> Errors);

public sealed class ImportProductsCsvCommandHandler
    : IRequestHandler<ImportProductsCsvCommand, ImportProductsCsvResult>
{
    private readonly IUnitOfWork _uow;

    public ImportProductsCsvCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ImportProductsCsvResult> Handle(
        ImportProductsCsvCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Content.Length == 0)
            throw new InvalidOperationException("The uploaded CSV file is empty.");

        int created = 0;
        int updated = 0;
        int failed = 0;
        int rowNumber = 1;
        var errors = new List<string>();
        var seenSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var stream = new MemoryStream(request.Content);
        using var parser = new TextFieldParser(stream, Encoding.UTF8, detectEncoding: true)
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = true
        };
        parser.SetDelimiters(",");

        if (parser.EndOfData)
            throw new InvalidOperationException("The uploaded CSV file has no header row.");

        string[] headers = parser.ReadFields() ?? [];
        var columns = headers
            .Select((name, index) => new { Name = name.Trim().TrimStart('\uFEFF'), Index = index })
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .ToDictionary(x => x.Name, x => x.Index, StringComparer.OrdinalIgnoreCase);

        string[] requiredHeaders = ["SKU", "Name", "BrandId", "CategoryId", "Price", "StockQty"];
        string[] missing = requiredHeaders.Where(h => !columns.ContainsKey(h)).ToArray();
        if (missing.Length > 0)
            throw new InvalidOperationException($"Missing required CSV columns: {string.Join(", ", missing)}.");

        await _uow.BeginTransactionAsync(cancellationToken);
        try
        {
            while (!parser.EndOfData)
            {
                rowNumber++;
                string[] fields;
                try
                {
                    fields = parser.ReadFields() ?? [];
                }
                catch (MalformedLineException)
                {
                    failed++;
                    errors.Add($"Row {rowNumber}: malformed CSV row.");
                    continue;
                }

                if (fields.All(string.IsNullOrWhiteSpace))
                    continue;

                string Get(string name)
                {
                    if (!columns.TryGetValue(name, out int index) || index >= fields.Length)
                        return string.Empty;
                    return fields[index].Trim();
                }

                try
                {
                    string sku = Get("SKU").ToUpperInvariant();
                    string name = Get("Name");
                    if (string.IsNullOrWhiteSpace(sku))
                        throw new FormatException("SKU is required.");
                    if (string.IsNullOrWhiteSpace(name))
                        throw new FormatException("Name is required.");
                    if (!seenSkus.Add(sku))
                        throw new FormatException($"SKU '{sku}' appears more than once in this CSV.");

                    if (!Guid.TryParse(Get("BrandId"), out Guid brandId))
                        throw new FormatException("BrandId must be a valid brand GUID.");
                    if (!int.TryParse(Get("CategoryId"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int categoryId))
                        throw new FormatException("CategoryId must be a valid integer.");
                    if (!decimal.TryParse(Get("Price"), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal price) || price <= 0)
                        throw new FormatException("Price must be a number greater than zero.");
                    if (!int.TryParse(Get("StockQty"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int stockQty) || stockQty < 0)
                        throw new FormatException("StockQty must be a whole number greater than or equal to zero.");

                    if (await _uow.Brands.GetByIdAsync(brandId, cancellationToken) is null)
                        throw new FormatException($"BrandId '{brandId}' was not found.");
                    if (await _uow.Categories.GetByIdAsync(categoryId, cancellationToken) is null)
                        throw new FormatException($"CategoryId '{categoryId}' was not found.");

                    decimal? compareAtPrice = ParseOptionalDecimal(Get("CompareAtPrice"), "CompareAtPrice");
                    decimal? costPrice = ParseOptionalDecimal(Get("CostPrice"), "CostPrice");
                    decimal? weightKg = ParseOptionalDecimal(Get("WeightKg"), "WeightKg");
                    bool? isActive = ParseOptionalBool(Get("IsActive"), "IsActive");
                    bool? isFeatured = ParseOptionalBool(Get("IsFeatured"), "IsFeatured");
                    string description = Get("Description");
                    string shortDescription = Get("ShortDescription");
                    string tagsJson = Get("TagsJson");
                    string attributesJson = Get("AttributesJson");
                    string imageUrlsValue = Get("ImageUrls");
                    List<string>? imageUrls = string.IsNullOrWhiteSpace(imageUrlsValue)
                        ? null
                        : imageUrlsValue.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();

                    Product? product = await _uow.Products.GetBySkuAsync(sku, cancellationToken);
                    if (product is null)
                    {
                        string slug = string.IsNullOrWhiteSpace(Get("Slug"))
                            ? GenerateSlug(name)
                            : GenerateSlug(Get("Slug"));

                        if (string.IsNullOrWhiteSpace(slug))
                            slug = $"product-{Guid.NewGuid():N}"[..16];

                        string baseSlug = slug;
                        int suffix = 2;
                        while (await _uow.Products.SlugExistsAsync(slug, cancellationToken))
                            slug = $"{baseSlug}-{suffix++}";

                        product = new Product
                        {
                            SKU = sku,
                            Name = name,
                            Slug = slug,
                            BrandId = brandId,
                            CategoryId = categoryId,
                            Price = price,
                            StockQty = stockQty,
                            IsActive = isActive ?? true,
                            IsFeatured = isFeatured ?? false
                        };
                        ApplyOptionalFields(product, description, shortDescription, compareAtPrice,
                            costPrice, weightKg, attributesJson, tagsJson, imageUrls);
                        await _uow.Products.AddAsync(product, cancellationToken);
                        created++;
                    }
                    else
                    {
                        product.Name = name;
                        product.BrandId = brandId;
                        product.CategoryId = categoryId;
                        product.Price = price;
                        product.StockQty = stockQty;

                        if (!string.IsNullOrWhiteSpace(description)) product.Description = description;
                        if (!string.IsNullOrWhiteSpace(shortDescription)) product.ShortDescription = shortDescription;
                        if (compareAtPrice.HasValue) product.CompareAtPrice = compareAtPrice;
                        if (costPrice.HasValue) product.CostPrice = costPrice;
                        if (weightKg.HasValue) product.WeightKg = weightKg;
                        if (!string.IsNullOrWhiteSpace(attributesJson)) product.AttributesJson = attributesJson;
                        if (!string.IsNullOrWhiteSpace(tagsJson)) product.TagsJson = tagsJson;
                        if (imageUrls is not null) product.ImageUrls = imageUrls;
                        if (isActive.HasValue) product.IsActive = isActive.Value;
                        if (isFeatured.HasValue) product.IsFeatured = isFeatured.Value;
                        _uow.Products.Update(product);
                        updated++;
                    }
                }
                catch (Exception ex) when (ex is FormatException or ArgumentException)
                {
                    failed++;
                    errors.Add($"Row {rowNumber}: {ex.Message}");
                }
            }

            await _uow.SaveChangesAsync(cancellationToken);
            await _uow.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _uow.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return new ImportProductsCsvResult(created, updated, failed, errors);
    }

    private static void ApplyOptionalFields(
        Product product,
        string description,
        string shortDescription,
        decimal? compareAtPrice,
        decimal? costPrice,
        decimal? weightKg,
        string attributesJson,
        string tagsJson,
        List<string>? imageUrls)
    {
        product.Description = NullIfBlank(description);
        product.ShortDescription = NullIfBlank(shortDescription);
        product.CompareAtPrice = compareAtPrice;
        product.CostPrice = costPrice;
        product.WeightKg = weightKg;
        product.AttributesJson = NullIfBlank(attributesJson);
        product.TagsJson = NullIfBlank(tagsJson);
        if (imageUrls is not null) product.ImageUrls = imageUrls;
    }

    private static decimal? ParseOptionalDecimal(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal result) || result < 0)
            throw new FormatException($"{field} must be a number greater than or equal to zero.");
        return result;
    }

    private static bool? ParseOptionalBool(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (bool.TryParse(value, out bool result)) return result;
        if (value == "1") return true;
        if (value == "0") return false;
        throw new FormatException($"{field} must be true or false.");
    }

    private static string? NullIfBlank(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static string GenerateSlug(string value)
    {
        var builder = new StringBuilder();
        foreach (char c in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c)) builder.Append(c);
            else if (builder.Length > 0 && builder[^1] != '-') builder.Append('-');
        }
        return builder.ToString().Trim('-');
    }
}
