using System.Net;
using System.Text.Json;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.EntityFrameworkCore;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Infrastructure.Persistence;
using HttpMethod = System.Net.Http.HttpMethod;

namespace ShopFresherz.ImageUpdater;

internal sealed record ProductImageSource(
    Guid ProductId,
    string Sku,
    string Name,
    string[] SourceUrls);

internal sealed record ProductSourceFile(ProductImageSource[] Products);

internal sealed record CategoryImageSource(
    string Slug,
    string Name,
    string[] SourceUrls);

internal sealed record CategorySourceFile(CategoryImageSource[] Categories);

internal sealed class CloudinaryUploader
{
    private readonly Cloudinary _cloudinary;
    private readonly string _folderPrefix;

    public CloudinaryUploader(string cloudName, string apiKey, string apiSecret, string folderPrefix = "shopfresherz/products")
    {
        _cloudinary = new Cloudinary(new Account(cloudName, apiKey, apiSecret))
        {
            Api = { Secure = true }
        };

        _folderPrefix = folderPrefix.Trim('/');
    }

    public async Task<string> UploadFromRemoteUrlAsync(
        string sourceUrl,
        string uploadPath,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, sourceUrl);
        request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (compatible; ShopFresherz.ImageUpdater/1.0)");

        using HttpResponseMessage response = await HttpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        ImageUploadParams uploadParams = new()
        {
            File = new FileDescription(uploadPath, stream),
            PublicId = $"{_folderPrefix}/{uploadPath}",
            Overwrite = true,
        };

        ImageUploadResult result = await _cloudinary.UploadAsync(uploadParams);

        if (result.StatusCode != HttpStatusCode.OK && result.StatusCode != HttpStatusCode.Created)
        {
            throw new InvalidOperationException(
                $"Cloudinary upload failed for '{sourceUrl}': {result.Error?.Message}");
        }

        return result.SecureUrl?.ToString() ?? result.Url?.ToString() ??
               throw new InvalidOperationException("Cloudinary returned an empty URL.");
    }

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(90),
    };
}

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static async Task<int> Main(string[] args)
    {
        Console.WriteLine("ShopFresherz Cloudinary image updater starting...");

        string cloudName = GetSetting("CLOUDINARY_CLOUD_NAME");
        string apiKey = GetSetting("CLOUDINARY_API_KEY");
        string apiSecret = GetSetting("CLOUDINARY_API_SECRET");

        // Parse CLI args: --mode [products|categories] --source <file>
        string mode = "products";
        string sourceArgFile = string.Empty;
        for (int i = 0; i < args.Length; i++)
        {
            if ((args[i] is "--mode" or "-m") && i + 1 < args.Length)
            {
                mode = args[i + 1];
                i++;
            }
            else if ((args[i] is "--source" or "-s") && i + 1 < args.Length)
            {
                sourceArgFile = args[i + 1];
                i++;
            }
        }

        string? connectionString = Environment.GetEnvironmentVariable("DEFAULT_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
            connectionString = TryLoadConnectionStringFromApiSettings();

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.Error.WriteLine("Connection string not found. Set DEFAULT_CONNECTION_STRING or configure appsettings.Development.json in ShopFresherz.API.");
            return 1;
        }

        DbContextOptionsBuilder<ShopFresherzDbContext> optionsBuilder = new();
        optionsBuilder.UseSqlServer(connectionString);
        using ShopFresherzDbContext dbContext = new(optionsBuilder.Options);

        if (mode == "categories")
        {
            string catSourcePath = ResolveSourcePath(sourceArgFile, "category-image-sources.json");
            return await RunCategoriesAsync(cloudName, apiKey, apiSecret, catSourcePath, dbContext);
        }
        else
        {
            string productSourcePath = ResolveSourcePath(sourceArgFile, "product-image-sources.json");
            return await RunProductsAsync(cloudName, apiKey, apiSecret, productSourcePath, dbContext);
        }
    }

    private static string ResolveSourcePath(string argFile, string defaultFile)
        => string.IsNullOrWhiteSpace(argFile)
            ? Path.Combine(Directory.GetCurrentDirectory(), defaultFile)
            : Path.IsPathRooted(argFile)
                ? argFile
                : Path.Combine(Directory.GetCurrentDirectory(), argFile);

    // ── Products mode ─────────────────────────────────────────────────────────

    private static async Task<int> RunProductsAsync(
        string cloudName, string apiKey, string apiSecret,
        string sourcePath, ShopFresherzDbContext dbContext)
    {
        if (!File.Exists(sourcePath))
        {
            Console.Error.WriteLine($"Product source file not found: {sourcePath}");
            return 1;
        }

        ProductSourceFile productSourceFile = JsonSerializer.Deserialize<ProductSourceFile>(
            File.ReadAllText(sourcePath), JsonOptions)
            ?? throw new InvalidOperationException("Unable to parse product image source file.");

        CloudinaryUploader uploader = new(cloudName, apiKey, apiSecret, "shopfresherz/products");

        foreach (ProductImageSource source in productSourceFile.Products)
        {
            Console.WriteLine($"Processing product {source.Sku} ({source.Name})...");

            Product? product = await dbContext.Products.FirstOrDefaultAsync(
                p => p.Id == source.ProductId, CancellationToken.None);

            if (product is null)
            {
                Console.Error.WriteLine($"Product ID not found in database: {source.ProductId}");
                continue;
            }

            List<string> cloudinaryUrls = new();

            for (int index = 0; index < source.SourceUrls.Length; index++)
            {
                string sourceUrl = source.SourceUrls[index];
                if (string.IsNullOrWhiteSpace(sourceUrl)) continue;

                string uploadId = $"{source.ProductId:N}/image-{index + 1}";
                Console.WriteLine($"  [{index + 1}/{source.SourceUrls.Length}] {sourceUrl}");

                try
                {
                    string cloudUrl = await uploader.UploadFromRemoteUrlAsync(sourceUrl, uploadId, CancellationToken.None);
                    cloudinaryUrls.Add(cloudUrl);
                    Console.WriteLine($"  → {cloudUrl}");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"  ✗ {ex.Message}");
                }
            }

            if (cloudinaryUrls.Count == 0)
            {
                Console.Error.WriteLine($"  No images uploaded for {source.Sku}; skipping DB update.");
                continue;
            }

            product.ImageUrls = cloudinaryUrls;
            dbContext.Products.Update(product);
            await dbContext.SaveChangesAsync();
            Console.WriteLine($"  ✓ Updated {source.Sku} with {cloudinaryUrls.Count} image(s).\n");
        }

        Console.WriteLine("Product image update complete.");
        return 0;
    }

    // ── Categories mode ───────────────────────────────────────────────────────

    private static async Task<int> RunCategoriesAsync(
        string cloudName, string apiKey, string apiSecret,
        string sourcePath, ShopFresherzDbContext dbContext)
    {
        if (!File.Exists(sourcePath))
        {
            Console.Error.WriteLine($"Category source file not found: {sourcePath}");
            return 1;
        }

        CategorySourceFile categorySourceFile = JsonSerializer.Deserialize<CategorySourceFile>(
            File.ReadAllText(sourcePath), JsonOptions)
            ?? throw new InvalidOperationException("Unable to parse category image source file.");

        CloudinaryUploader uploader = new(cloudName, apiKey, apiSecret, "shopfresherz/categories");

        foreach (CategoryImageSource source in categorySourceFile.Categories)
        {
            Console.WriteLine($"Processing category '{source.Name}' (slug: {source.Slug})...");

            Category? category = await dbContext.Categories.FirstOrDefaultAsync(
                c => c.Slug == source.Slug, CancellationToken.None);

            if (category is null)
            {
                Console.Error.WriteLine($"  Category slug not found in database: {source.Slug}");
                continue;
            }

            string? uploadedUrl = null;

            for (int index = 0; index < source.SourceUrls.Length; index++)
            {
                string sourceUrl = source.SourceUrls[index];
                if (string.IsNullOrWhiteSpace(sourceUrl)) continue;

                string uploadId = $"{source.Slug}/banner";
                Console.WriteLine($"  [{index + 1}/{source.SourceUrls.Length}] {sourceUrl}");

                try
                {
                    uploadedUrl = await uploader.UploadFromRemoteUrlAsync(sourceUrl, uploadId, CancellationToken.None);
                    Console.WriteLine($"  → {uploadedUrl}");
                    break; // Use first successful upload
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"  ✗ {ex.Message}");
                }
            }

            if (uploadedUrl is null)
            {
                Console.Error.WriteLine($"  No image uploaded for category {source.Slug}; skipping DB update.");
                continue;
            }

            category.ImageUrl = uploadedUrl;
            dbContext.Categories.Update(category);
            await dbContext.SaveChangesAsync();
            Console.WriteLine($"  ✓ Updated category '{source.Name}' with image.\n");
        }

        Console.WriteLine("Category image update complete.");
        return 0;
    }

    private static string GetSetting(string key)
    {
        string? value = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Required environment variable '{key}' is missing.");
        return value;
    }

    private static string? TryLoadConnectionStringFromApiSettings()
    {
        string[] candidatePaths =
        [
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "ShopFresherz.API", "appsettings.Development.json")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "ShopFresherz.API", "appsettings.Development.json")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "ShopFresherz.API", "appsettings.Development.json")),
        ];

        foreach (string path in candidatePaths)
        {
            if (!File.Exists(path)) continue;

            using FileStream fs = File.OpenRead(path);
            using JsonDocument document = JsonDocument.Parse(fs);
            if (document.RootElement.TryGetProperty("ConnectionStrings", out JsonElement connectionStrings)
                && connectionStrings.TryGetProperty("DefaultConnection", out JsonElement defaultConnection)
                && defaultConnection.ValueKind == JsonValueKind.String)
            {
                return defaultConnection.GetString();
            }
        }

        return null;
    }
}
