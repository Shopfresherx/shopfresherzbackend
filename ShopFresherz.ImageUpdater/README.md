# ShopFresherz Image Updater

A small console utility to upload product images to Cloudinary and update `ImageUrlsJson` in the ShopFresherz product catalog.

## Setup

1. Set the required environment variables:
   - `CLOUDINARY_CLOUD_NAME`
   - `CLOUDINARY_API_KEY`
   - `CLOUDINARY_API_SECRET`
   - `DEFAULT_CONNECTION_STRING` (optional, if not using `ShopFresherz.API/appsettings.Development.json`)

2. Ensure the `product-image-sources.json` file contains the source image URLs for each product.

## Run

From the `ShopFresherz.ImageUpdater` folder:

```powershell
dotnet run --project ShopFresherz.ImageUpdater.csproj
```

The utility downloads each source image, uploads it to Cloudinary, and updates the `ImageUrlsJson` field for the matching product ID.
