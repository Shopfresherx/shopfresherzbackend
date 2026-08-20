using Microsoft.EntityFrameworkCore;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Enums;

namespace ShopFresherz.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(ShopFresherzDbContext context)
    {
        try { await SeedCategoriesAsync(context); } catch { }
        try { await SeedAdminUserAsync(context); } catch { }
        try { await SeedBrandsAsync(context); } catch { }
        try { await SeedProductsAsync(context); } catch { }
        try { await SeedCatalog2026Async(context); } catch { }
        try { await SeedFlashDealAsync(context); } catch { }
        try { await SeedBannersAsync(context); } catch { }
        try { await SeedCouponsAsync(context); } catch { }
        try { await SeedReviewsAsync(context); } catch { }
        try { await SeedPromotionalSectionsAsync(context); } catch { }
    }

    // ── Categories ────────────────────────────────────────────────────────────

    private static readonly List<Category> CategoriesToSeed = new()
    {
        new() { Id = 1,  Name = "Smartphones",          Slug = "smartphones",        ParentId = null, SortOrder = 1, IsActive = true },
        new() { Id = 2,  Name = "Smartphones",          Slug = "phones",             ParentId = null, SortOrder = 1, IsActive = true },
        new() { Id = 3,  Name = "Laptops & Computers",  Slug = "laptops",            ParentId = null, SortOrder = 2, IsActive = true },
        new() { Id = 4,  Name = "Laptops",              Slug = "laptops-sub",        ParentId = 3,    SortOrder = 1, IsActive = true },
        new() { Id = 5,  Name = "Smartwatches",         Slug = "smartwatches",       ParentId = null, SortOrder = 3, IsActive = true },
        new() { Id = 6,  Name = "Accessories",          Slug = "accessories",        ParentId = null, SortOrder = 4, IsActive = true },
        new() { Id = 7,  Name = "Gaming",               Slug = "gaming",             ParentId = null, SortOrder = 5, IsActive = true },
        new() { Id = 8,  Name = "Audio",                Slug = "audio",              ParentId = null, SortOrder = 6, IsActive = true },
        new() { Id = 9,  Name = "Computer Peripherals", Slug = "peripherals",        ParentId = null, SortOrder = 7, IsActive = true },
        new() { Id = 11, Name = "Cables",               Slug = "cables",             ParentId = 6,    SortOrder = 1, IsActive = true },
        new() { Id = 15, Name = "Gaming Accessories",   Slug = "gaming-accessories", ParentId = 7,    SortOrder = 1, IsActive = true },
        new() { Id = 16, Name = "Chargers",             Slug = "chargers",           ParentId = 6,    SortOrder = 2, IsActive = true },
        new() { Id = 17, Name = "Power Banks",          Slug = "power-banks",        ParentId = 6,    SortOrder = 3, IsActive = true },
        new() { Id = 18, Name = "Earbuds & Headphones", Slug = "earbuds",            ParentId = 8,    SortOrder = 1, IsActive = true },

        // Legacy storefront slugs kept for backward-compatible links and older seed lookups.
        new() { Id = 101, Name = "New Arrivals",         Slug = "new-arrivals",          SortOrder = 101, IsActive = true },
        new() { Id = 102, Name = "Mobile Phones",        Slug = "mobile-phones",         SortOrder = 102, IsActive = true },
        new() { Id = 103, Name = "Tablets",              Slug = "tablets",               SortOrder = 103, IsActive = true },
        new() { Id = 104, Name = "Laptops & Computers",  Slug = "laptops-computers",     SortOrder = 104, IsActive = true },
        new() { Id = 105, Name = "Smart Watches",        Slug = "smart-watches",         SortOrder = 105, IsActive = true },
        new() { Id = 106, Name = "Games & Consoles",     Slug = "games-consoles",        SortOrder = 106, IsActive = true },
        new() { Id = 107, Name = "Electronics",          Slug = "electronics",           SortOrder = 107, IsActive = true },
        new() { Id = 108, Name = "Computing Accessories",Slug = "computing-accessories", SortOrder = 108, IsActive = true },
        new() { Id = 109, Name = "Home & Kitchen Tech",  Slug = "home-kitchen-tech",     SortOrder = 109, IsActive = true },
        new() { Id = 110, Name = "Musical Equipment",    Slug = "musical-equipment",     SortOrder = 110, IsActive = true },
        new() { Id = 111, Name = "Romoss",               Slug = "romoss",                SortOrder = 111, IsActive = true },
        new() { Id = 112, Name = "Computer Mouse",       Slug = "computer-mouse",        SortOrder = 112, IsActive = true },
        new() { Id = 113, Name = "Other Categories",     Slug = "other-categories",      SortOrder = 113, IsActive = true },

        // ── Catalog 2026 category addition ──
        new() { Name = "Content Creation", Slug = "content-creation", ParentId = null, IsActive = true },
    };

    private static async Task SeedCategoriesAsync(ShopFresherzDbContext context)
    {
        HashSet<string> existing = (await context.Categories
            .Select(c => c.Slug)
            .ToListAsync()).ToHashSet();

        HashSet<int> existingIds = (await context.Categories
            .Select(c => c.Id)
            .ToListAsync()).ToHashSet();

        List<Category> missing = CategoriesToSeed
            .Where(c => !existing.Contains(c.Slug))
            .ToList();

        if (missing.Count == 0) return;

        List<Category> withExplicitIds = missing
            .Where(c => c.Id > 0 && !existingIds.Contains(c.Id))
            .ToList();

        List<Category> withoutExplicitIds = missing
            .Except(withExplicitIds)
            .Select(c => new Category
            {
                Name = c.Name,
                Slug = c.Slug,
                ParentId = c.ParentId.HasValue && existingIds.Contains(c.ParentId.Value) ? c.ParentId : null,
                SortOrder = c.SortOrder,
                IsActive = c.IsActive,
                ImageUrl = c.ImageUrl,
                MetaTitle = c.MetaTitle,
                MetaDescription = c.MetaDescription,
            })
            .ToList();

        if (withExplicitIds.Count > 0)
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Categories ON");
            await context.Categories.AddRangeAsync(withExplicitIds);
            await context.SaveChangesAsync();
            await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Categories OFF");
            await transaction.CommitAsync();
        }

        if (withoutExplicitIds.Count > 0)
        {
            await context.Categories.AddRangeAsync(withoutExplicitIds);
            await context.SaveChangesAsync();
        }
    }

    // ── Admin user ────────────────────────────────────────────────────────────

    private static async Task SeedAdminUserAsync(ShopFresherzDbContext context)
    {
        if (await context.Users.AnyAsync(u => u.Role == UserRole.SuperAdmin)) return;

        User admin = new()
        {
            Id           = Guid.NewGuid(),
            Email        = "admin@shopfresherz.com",
            Phone        = "+2349075308722",
            FirstName    = "Shop",
            LastName     = "Admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@ShopFresherz2026!"),
            Role         = UserRole.SuperAdmin,
            IsVerified   = true,
            CreatedAt    = DateTime.UtcNow,
        };

        await context.Users.AddAsync(admin);
        await context.SaveChangesAsync();
    }

    // ── Brands ────────────────────────────────────────────────────────────────

    private static readonly List<Brand> BrandsToSeed = new()
    {
        new() { Id = new("5C9C601D-9FE5-4718-B47C-ABB836C04A53"), Name = "Apple",        Slug = "apple",        IsActive = true },
        new() { Id = new("CF66C378-DE44-42F9-A9F4-F73D014698C1"), Name = "Samsung",      Slug = "samsung",      IsActive = true },
        new() { Id = new("8BCA4BB9-1C01-4E5C-9B19-745294994627"), Name = "Tecno",        Slug = "tecno",        IsActive = true },
        new() { Id = new("81E9A3B8-2DE4-46DA-9EBF-99D968769ACE"), Name = "Infinix",      Slug = "infinix",      IsActive = true },
        new() { Name = "HP",           Slug = "hp",           IsActive = true },
        new() { Name = "Dell",         Slug = "dell",         IsActive = true },
        new() { Id = new("FF3005B0-337B-4C8D-B08C-68BD558FBDBF"), Name = "Sony",         Slug = "sony",         IsActive = true },
        new() { Id = new("5FA3BB47-91D3-4635-9F2C-986C709AB99A"), Name = "JBL",          Slug = "jbl",          IsActive = true },
        new() { Name = "Romoss",       Slug = "romoss",       IsActive = true },
        new() { Name = "Logitech",     Slug = "logitech",     IsActive = true },
        new() { Id = new("D6F19BCB-D59F-4694-9CB2-3773F0DAD156"), Name = "Xiaomi",       Slug = "xiaomi",       IsActive = true },
        new() { Id = new("637267AA-C77B-450D-AC55-BC2143FED949"), Name = "Anker",        Slug = "anker",        IsActive = true },
        new() { Id = new("8BA95021-0275-4FEC-B32F-6CB1649BF624"), Name = "Oraimo",       Slug = "oraimo",       IsActive = true },
        new() { Id = new("AFE952D2-E420-46F6-9C68-2C22AFBFC627"), Name = "Microsoft",    Slug = "microsoft",    IsActive = true },
        new() { Id = new("BCE18E32-CD7D-493B-8D84-7E984BA60D43"), Name = "Nintendo",     Slug = "nintendo",     IsActive = true },
        new() { Id = new("0E0545DA-55F3-4AC6-A9EB-884E6474AB76"), Name = "Razer",        Slug = "razer",        IsActive = true },
        new() { Id = new("8EF4C97F-B1E1-408D-981D-439EFD576076"), Name = "Redragon",     Slug = "redragon",     IsActive = true },
        new() { Id = new("5EB42993-9AA4-40D8-9BEA-D69522772E88"), Name = "HyperX",       Slug = "hyperx",       IsActive = true },
        new() { Id = new("85D7B77D-5483-4CDB-B962-899D19F00F2C"), Name = "Spigen",       Slug = "spigen",       IsActive = true },
        new() { Id = new("BEC93700-9990-4296-8E6D-C329C4AC870B"), Name = "Neewer",       Slug = "neewer",       IsActive = true },
        new() { Id = new("7D2F9979-8BC4-41C4-B847-1FF3130B6230"), Name = "UGREEN",       Slug = "ugreen",       IsActive = true },
        new() { Id = new("4B86C0FD-BD02-463D-9F1F-D47982528AA3"), Name = "Cooler Master",Slug = "cooler-master",IsActive = true },
        new() { Id = new("D0EF5DF2-B0AA-4984-8227-47C4E3D3214F"), Name = "GTRacing",     Slug = "gtracing",     IsActive = true },

        // ── Full brand catalogue expansion (accessories, gaming, cameras, storage) ──
        new() { Name = "Airsky",              Slug = "airsky",              IsActive = true },
        new() { Name = "Airtel",              Slug = "airtel",              IsActive = true },
        new() { Name = "AKZ",                 Slug = "akz",                 IsActive = true },
        new() { Name = "Amazon",              Slug = "amazon",              IsActive = true },
        new() { Name = "Anspo",               Slug = "anspo",               IsActive = true },
        new() { Name = "Beats",               Slug = "beats",               IsActive = true },
        new() { Name = "Boya",                Slug = "boya",                IsActive = true },
        new() { Name = "Crossfire",           Slug = "crossfire",           IsActive = true },
        new() { Name = "Digirich",            Slug = "digirich",            IsActive = true },
        new() { Name = "DM",                  Slug = "dm",                  IsActive = true },
        new() { Name = "Earldom",             Slug = "earldom",             IsActive = true },
        new() { Name = "FeiyuTech",           Slug = "feiyutech",           IsActive = true },
        new() { Name = "Hiksemi",             Slug = "hiksemi",             IsActive = true },
        new() { Name = "Jabra",               Slug = "jabra",               IsActive = true },
        new() { Name = "Koleer",              Slug = "koleer",              IsActive = true },
        new() { Name = "LDNIO",               Slug = "ldnio",               IsActive = true },
        new() { Name = "Maono",               Slug = "maono",               IsActive = true },
        new() { Name = "Merak One",           Slug = "merak-one",           IsActive = true },
        new() { Name = "MTN",                 Slug = "mtn",                 IsActive = true },
        new() { Name = "New Age",             Slug = "new-age",             IsActive = true },
        new() { Name = "Norton",              Slug = "norton",              IsActive = true },
        new() { Name = "Onten",               Slug = "onten",               IsActive = true },
        new() { Name = "Plantronics (Poly)",  Slug = "plantronics-poly",    IsActive = true },
        new() { Name = "PlayStation",         Slug = "playstation",         IsActive = true },
        new() { Name = "QLT",                 Slug = "qlt",                 IsActive = true },
        new() { Name = "Quick Heal",          Slug = "quick-heal",          IsActive = true },
        new() { Name = "Redmi",               Slug = "redmi",               IsActive = true },
        new() { Name = "REMAX",               Slug = "remax",               IsActive = true },
        new() { Name = "Roku",                Slug = "roku",                IsActive = true },
        new() { Name = "SanDisk",             Slug = "sandisk",             IsActive = true },
        new() { Name = "Seagate",             Slug = "seagate",             IsActive = true },
        new() { Name = "Soundcore (Anker)",   Slug = "soundcore-anker",     IsActive = true },
        new() { Name = "TP-Link",             Slug = "tp-link",             IsActive = true },
        new() { Name = "Verbatim",            Slug = "verbatim",            IsActive = true },
        new() { Name = "WD (Western Digital)",Slug = "wd-western-digital",  IsActive = true },
        new() { Name = "Winpossee",           Slug = "winpossee",           IsActive = true },
        new() { Name = "Wmark",               Slug = "wmark",               IsActive = true },
        new() { Name = "Yoobao",              Slug = "yoobao",              IsActive = true },
        new() { Name = "Zealot",              Slug = "zealot",              IsActive = true },
        new() { Name = "Zoom",                Slug = "zoom",                IsActive = true },
        new() { Name = "Baseus",              Slug = "baseus",              IsActive = true },
        new() { Name = "Belkin",              Slug = "belkin",              IsActive = true },
        new() { Name = "Mcdodo",              Slug = "mcdodo",              IsActive = true },
        new() { Name = "JOYROOM",             Slug = "joyroom",             IsActive = true },
        new() { Name = "Hoco",                Slug = "hoco",                IsActive = true },
        new() { Name = "Green Lion",          Slug = "green-lion",          IsActive = true },
        new() { Name = "WIWU",                Slug = "wiwu",                IsActive = true },
        new() { Name = "ESR",                 Slug = "esr",                 IsActive = true },
        new() { Name = "Ringke",              Slug = "ringke",              IsActive = true },
        new() { Name = "Torras",              Slug = "torras",              IsActive = true },
        new() { Name = "Caseology",           Slug = "caseology",           IsActive = true },
        new() { Name = "Xbox",                Slug = "xbox",                IsActive = true },
        new() { Name = "SteelSeries",         Slug = "steelseries",         IsActive = true },
        new() { Name = "GameSir",             Slug = "gamesir",             IsActive = true },
        new() { Name = "8BitDo",              Slug = "8bitdo",              IsActive = true },
        new() { Name = "Turtle Beach",        Slug = "turtle-beach",        IsActive = true },
        new() { Name = "Huawei",              Slug = "huawei",              IsActive = true },
        new() { Name = "Nothing",             Slug = "nothing",             IsActive = true },
        new() { Name = "CMF by Nothing",      Slug = "cmf-by-nothing",      IsActive = true },
        new() { Name = "Amazfit",             Slug = "amazfit",             IsActive = true },
        new() { Name = "Google",              Slug = "google",              IsActive = true },
        new() { Name = "DJI",                 Slug = "dji",                 IsActive = true },
        new() { Name = "Insta360",            Slug = "insta360",            IsActive = true },
        new() { Name = "GoPro",               Slug = "gopro",               IsActive = true },
        new() { Name = "Ulanzi",              Slug = "ulanzi",              IsActive = true },
        new() { Name = "Rode",                Slug = "rode",                IsActive = true },
        new() { Name = "Elgato",              Slug = "elgato",              IsActive = true },
        new() { Name = "Kingston",            Slug = "kingston",            IsActive = true },
        new() { Name = "Lexar",               Slug = "lexar",               IsActive = true },
        new() { Name = "Crucial",             Slug = "crucial",             IsActive = true },
        new() { Name = "Samsung Storage",     Slug = "samsung-storage",     IsActive = true },

        // ── Catalog 2026 brand additions ──
        // Zealot, Maono, Boya, New Age already exist above — not duplicated here.
        new() { Name = "Godox",               Slug = "godox",               IsActive = true },
        new() { Name = "Generic",             Slug = "generic",             IsActive = true },
    };

    private static async Task SeedBrandsAsync(ShopFresherzDbContext context)
    {
        HashSet<string> existing = (await context.Brands
            .Select(b => b.Name)
            .ToListAsync())
            .Select(n => n.Trim().ToLowerInvariant())
            .ToHashSet();

        List<Brand> missing = BrandsToSeed
            .Where(b => !existing.Contains(b.Name.Trim().ToLowerInvariant()))
            .ToList();

        if (missing.Count == 0) return;

        await context.Brands.AddRangeAsync(missing);
        await context.SaveChangesAsync();
    }

    // ── Products ──────────────────────────────────────────────────────────────

    private static async Task SeedProductsAsync(ShopFresherzDbContext context)
    {
        // ── Load lookup dictionaries ─────────────────────────────────────────
        Dictionary<string, Guid> brandIds = await context.Brands
            .ToDictionaryAsync(b => b.Slug, b => b.Id);
        Dictionary<string, int> categoryIds = await context.Categories
            .ToDictionaryAsync(c => c.Slug, c => c.Id);

        Guid apple    = brandIds["apple"];
        Guid samsung  = brandIds["samsung"];
        Guid tecno    = brandIds["tecno"];
        Guid infinix  = brandIds["infinix"];
        Guid sony     = brandIds["sony"];
        Guid jbl      = brandIds["jbl"];
        Guid xiaomi   = brandIds["xiaomi"];
        Guid anker    = brandIds["anker"];
        Guid oraimo   = brandIds["oraimo"];
        Guid microsoft = brandIds["microsoft"];
        Guid nintendo = brandIds["nintendo"];
        Guid razer    = brandIds["razer"];
        Guid redragon = brandIds["redragon"];
        Guid hyperx   = brandIds["hyperx"];
        Guid spigen   = brandIds["spigen"];
        Guid neewer   = brandIds["neewer"];
        Guid ugreen   = brandIds["ugreen"];
        Guid coolerMaster = brandIds["cooler-master"];
        Guid gtracing = brandIds["gtracing"];
        Guid logitech = brandIds["logitech"];

        int phones      = categoryIds.ContainsKey("phones") ? categoryIds["phones"] : categoryIds["mobile-phones"];
        int laptops     = categoryIds.ContainsKey("laptops-sub") ? categoryIds["laptops-sub"] : categoryIds["laptops-computers"];
        int consoles    = categoryIds.ContainsKey("gaming") ? categoryIds["gaming"] : categoryIds["games-consoles"];
        int accessories = categoryIds["accessories"];
        int smartWatches = categoryIds.ContainsKey("smartwatches") ? categoryIds["smartwatches"] : categoryIds["smart-watches"];
        int cables      = categoryIds["cables"];
        int compAcc     = categoryIds.ContainsKey("peripherals") ? categoryIds["peripherals"] : categoryIds["computing-accessories"];
        int gamingAcc   = categoryIds.ContainsKey("gaming-accessories") ? categoryIds["gaming-accessories"] : consoles;
        int chargers    = categoryIds["chargers"];
        int powerBanks  = categoryIds["power-banks"];
        int audio       = categoryIds.ContainsKey("earbuds") ? categoryIds["earbuds"] : categoryIds["audio"];
        int otherCats   = categoryIds["other-categories"];

        List<Product> products = new()
        {
            // ════════════════════════════════════════════════════════════════
            // SMARTPHONES
            // ════════════════════════════════════════════════════════════════

            new Product
            {
                Id = new Guid("11111111-1001-0000-0000-000000000001"),
                SKU = "IP16PMAX-256",
                Name = "Apple iPhone 16 Pro Max 256GB",
                Slug = "apple-iphone-16-pro-max-256gb",
                BrandId = apple, CategoryId = phones,
                Description = "The most powerful iPhone ever. A18 Pro chip delivers console-level gaming and groundbreaking camera capabilities. Titanium design, 48MP Fusion camera system, and 4K 120fps ProRes video.",
                ShortDescription = "A18 Pro · 48MP · Titanium · 5G",
                Price = 1850000, CompareAtPrice = 2050000,
                StockQty = 15, IsActive = true, IsFeatured = true,
                AverageRating = 4.9m, ReviewCount = 148, SoldCount = 93,
                WeightKg = 0.227m,
                AttributesJson = "{\"display\":\"6.9\\\" Super Retina XDR\",\"chip\":\"A18 Pro\",\"ram\":\"8GB\",\"storage\":\"256GB\",\"camera\":\"48MP main + 48MP ultrawide + 12MP telephoto\",\"battery\":\"4685mAh\",\"network\":\"5G\",\"os\":\"iOS 18\",\"color\":\"Black Titanium\"}"
            },

            new Product
            {
                Id = new Guid("11111111-1002-0000-0000-000000000001"),
                SKU = "IP16PRO-256",
                Name = "Apple iPhone 16 Pro 256GB",
                Slug = "apple-iphone-16-pro-256gb",
                BrandId = apple, CategoryId = phones,
                Description = "A18 Pro chip, all-new 48MP Fusion camera, and the innovative Camera Control button. 6.3-inch Super Retina XDR display with ProMotion. Titanium design.",
                ShortDescription = "A18 Pro · 48MP · 6.3\\\" OLED · Titanium",
                Price = 1550000, CompareAtPrice = 1750000,
                StockQty = 18, IsActive = true, IsFeatured = true,
                AverageRating = 4.8m, ReviewCount = 112, SoldCount = 74,
                WeightKg = 0.199m,
                AttributesJson = "{\"display\":\"6.3\\\" Super Retina XDR\",\"chip\":\"A18 Pro\",\"storage\":\"256GB\",\"camera\":\"48MP system\",\"battery\":\"3582mAh\",\"network\":\"5G\",\"os\":\"iOS 18\",\"color\":\"Desert Titanium\"}"
            },

            new Product
            {
                Id = new Guid("11111111-1003-0000-0000-000000000001"),
                SKU = "IP15PMAX-256",
                Name = "Apple iPhone 15 Pro Max 256GB",
                Slug = "apple-iphone-15-pro-max-256gb",
                BrandId = apple, CategoryId = phones,
                Description = "A17 Pro chip with hardware ray tracing. 5x optical zoom on a 12MP telephoto camera. Titanium frame. USB 3 speeds on USB-C. Dynamic Island.",
                ShortDescription = "A17 Pro · 5x Zoom · Titanium · USB-C",
                Price = 1350000, CompareAtPrice = 1550000,
                StockQty = 12, IsActive = true, IsFeatured = false,
                AverageRating = 4.7m, ReviewCount = 89, SoldCount = 61,
                WeightKg = 0.221m,
                AttributesJson = "{\"display\":\"6.7\\\" Super Retina XDR ProMotion\",\"chip\":\"A17 Pro\",\"storage\":\"256GB\",\"camera\":\"48MP + 12MP 5x zoom\",\"battery\":\"4422mAh\",\"network\":\"5G\",\"os\":\"iOS 17\"}"
            },

            new Product
            {
                Id = new Guid("11111111-1004-0000-0000-000000000001"),
                SKU = "IP15-128",
                Name = "Apple iPhone 15 128GB",
                Slug = "apple-iphone-15-128gb",
                BrandId = apple, CategoryId = phones,
                Description = "A16 Bionic chip. USB-C connector. Dynamic Island. 48MP main camera with 2x optical quality telephoto. Durable colour-infused glass back.",
                ShortDescription = "A16 Bionic · Dynamic Island · USB-C",
                Price = 850000, CompareAtPrice = 980000,
                StockQty = 20, IsActive = true, IsFeatured = false,
                AverageRating = 4.6m, ReviewCount = 76, SoldCount = 88,
                WeightKg = 0.171m,
                AttributesJson = "{\"display\":\"6.1\\\" Super Retina XDR\",\"chip\":\"A16 Bionic\",\"storage\":\"128GB\",\"camera\":\"48MP main\",\"battery\":\"3349mAh\",\"network\":\"5G\",\"os\":\"iOS 17\"}"
            },

            new Product
            {
                Id = new Guid("11111111-1005-0000-0000-000000000001"),
                SKU = "IP14PRO-256",
                Name = "Apple iPhone 14 Pro 256GB",
                Slug = "apple-iphone-14-pro-256gb",
                BrandId = apple, CategoryId = phones,
                Description = "A16 Bionic chip. Dynamic Island replaces the notch. Always-On display. 48MP Pro camera system. Emergency SOS via satellite.",
                ShortDescription = "A16 Bionic · Dynamic Island · 48MP",
                Price = 950000, CompareAtPrice = 1100000,
                StockQty = 10, IsActive = true, IsFeatured = false,
                AverageRating = 4.6m, ReviewCount = 64, SoldCount = 52,
                WeightKg = 0.206m,
                AttributesJson = "{\"display\":\"6.1\\\" Super Retina XDR ProMotion\",\"chip\":\"A16 Bionic\",\"storage\":\"256GB\",\"camera\":\"48MP Pro system\",\"battery\":\"3200mAh\",\"network\":\"5G\"}"
            },

            new Product
            {
                Id = new Guid("11111111-1006-0000-0000-000000000001"),
                SKU = "SGS25U-512",
                Name = "Samsung Galaxy S25 Ultra 512GB",
                Slug = "samsung-galaxy-s25-ultra-512gb",
                BrandId = samsung, CategoryId = phones,
                Description = "Snapdragon 8 Elite for Galaxy. Built-in S Pen. 200MP ProVisual Engine camera. Galaxy AI on-device intelligence. 12GB RAM, 5000mAh battery.",
                ShortDescription = "Snapdragon 8 Elite · 200MP · S Pen",
                Price = 1650000, CompareAtPrice = 1850000,
                StockQty = 14, IsActive = true, IsFeatured = true,
                AverageRating = 4.8m, ReviewCount = 103, SoldCount = 71,
                WeightKg = 0.218m,
                AttributesJson = "{\"display\":\"6.9\\\" QHD+ Dynamic AMOLED\",\"chip\":\"Snapdragon 8 Elite\",\"ram\":\"12GB\",\"storage\":\"512GB\",\"camera\":\"200MP main\",\"battery\":\"5000mAh 45W\",\"network\":\"5G\"}"
            },

            new Product
            {
                Id = new Guid("11111111-1007-0000-0000-000000000001"),
                SKU = "SGS24FE-128",
                Name = "Samsung Galaxy S24 FE 128GB",
                Slug = "samsung-galaxy-s24-fe-128gb",
                BrandId = samsung, CategoryId = phones,
                Description = "Exynos 2500, 6.7-inch FHD+ 120Hz AMOLED, 50MP OIS camera, 4700mAh battery with 25W charging. Galaxy AI features included.",
                ShortDescription = "Exynos 2500 · 50MP · 120Hz AMOLED",
                Price = 480000, CompareAtPrice = 540000,
                StockQty = 25, IsActive = true, IsFeatured = false,
                AverageRating = 4.4m, ReviewCount = 58, SoldCount = 67,
                WeightKg = 0.213m,
                AttributesJson = "{\"display\":\"6.7\\\" FHD+ 120Hz AMOLED\",\"chip\":\"Exynos 2500\",\"ram\":\"8GB\",\"storage\":\"128GB\",\"camera\":\"50MP OIS\",\"battery\":\"4700mAh 25W\",\"network\":\"5G\"}"
            },

            new Product
            {
                Id = new Guid("11111111-1008-0000-0000-000000000001"),
                SKU = "SGA56-128",
                Name = "Samsung Galaxy A56 5G 128GB",
                Slug = "samsung-galaxy-a56-5g-128gb",
                BrandId = samsung, CategoryId = phones,
                Description = "Exynos 1580, 6.7-inch FHD+ 120Hz Super AMOLED, 50MP triple camera, 5000mAh battery. Gorilla Glass Victus+ protection.",
                ShortDescription = "6.7\\\" AMOLED · 50MP · 5000mAh",
                Price = 380000, CompareAtPrice = 430000,
                StockQty = 30, IsActive = true, IsFeatured = false,
                AverageRating = 4.3m, ReviewCount = 44, SoldCount = 56,
                WeightKg = 0.198m,
                AttributesJson = "{\"display\":\"6.7\\\" FHD+ 120Hz Super AMOLED\",\"chip\":\"Exynos 1580\",\"ram\":\"8GB\",\"storage\":\"128GB\",\"camera\":\"50MP + 12MP + 5MP\",\"battery\":\"5000mAh\",\"network\":\"5G\"}"
            },

            new Product
            {
                Id = new Guid("11111111-1009-0000-0000-000000000001"),
                SKU = "SGA36-128",
                Name = "Samsung Galaxy A36 5G 128GB",
                Slug = "samsung-galaxy-a36-5g-128gb",
                BrandId = samsung, CategoryId = phones,
                Description = "Snapdragon 6 Gen 3, 6.66-inch FHD+ 120Hz Super AMOLED, 50MP camera, 5000mAh with 45W fast charging.",
                ShortDescription = "Snapdragon 6 Gen 3 · 50MP · 45W charging",
                Price = 280000, CompareAtPrice = 320000,
                StockQty = 35, IsActive = true, IsFeatured = false,
                AverageRating = 4.2m, ReviewCount = 37, SoldCount = 48,
                WeightKg = 0.193m,
                AttributesJson = "{\"display\":\"6.66\\\" FHD+ 120Hz Super AMOLED\",\"chip\":\"Snapdragon 6 Gen 3\",\"ram\":\"8GB\",\"storage\":\"128GB\",\"camera\":\"50MP\",\"battery\":\"5000mAh 45W\",\"network\":\"5G\"}"
            },

            new Product
            {
                Id = new Guid("11111111-1010-0000-0000-000000000001"),
                SKU = "SGZFLIP6-256",
                Name = "Samsung Galaxy Z Flip6 256GB",
                Slug = "samsung-galaxy-z-flip6-256gb",
                BrandId = samsung, CategoryId = phones,
                Description = "Snapdragon 8 Gen 3. Foldable design with 3.4-inch cover screen. Galaxy AI features. 50MP camera. 4000mAh battery.",
                ShortDescription = "Foldable · 3.4\\\" Cover Screen · Snapdragon 8 Gen 3",
                Price = 1100000, CompareAtPrice = 1250000,
                StockQty = 8, IsActive = true, IsFeatured = true,
                AverageRating = 4.5m, ReviewCount = 42, SoldCount = 29,
                WeightKg = 0.187m,
                AttributesJson = "{\"form_factor\":\"Foldable\",\"cover_display\":\"3.4\\\" Super AMOLED\",\"main_display\":\"6.7\\\" FHD+ 120Hz\",\"chip\":\"Snapdragon 8 Gen 3\",\"storage\":\"256GB\",\"camera\":\"50MP\",\"battery\":\"4000mAh\"}"
            },

            new Product
            {
                Id = new Guid("11111111-1011-0000-0000-000000000001"),
                SKU = "RN13PP-256",
                Name = "Xiaomi Redmi Note 13 Pro+ 5G 256GB",
                Slug = "xiaomi-redmi-note-13-pro-plus-5g-256gb",
                BrandId = xiaomi, CategoryId = phones,
                Description = "Dimensity 7200 Ultra, 200MP OIS camera, 6.67-inch 1.5K CurvedAMOLED, 120W HyperCharge. IP68 rated.",
                ShortDescription = "200MP · 120W HyperCharge · IP68",
                Price = 260000, CompareAtPrice = 300000,
                StockQty = 28, IsActive = true, IsFeatured = false,
                AverageRating = 4.4m, ReviewCount = 72, SoldCount = 84,
                WeightKg = 0.204m,
                AttributesJson = "{\"display\":\"6.67\\\" 1.5K CurvedAMOLED 120Hz\",\"chip\":\"Dimensity 7200 Ultra\",\"ram\":\"8GB\",\"storage\":\"256GB\",\"camera\":\"200MP OIS\",\"battery\":\"5000mAh 120W\",\"network\":\"5G\",\"ip\":\"IP68\"}"
            },

            new Product
            {
                Id = new Guid("11111111-1012-0000-0000-000000000001"),
                SKU = "RN13-128",
                Name = "Xiaomi Redmi Note 13 5G 128GB",
                Slug = "xiaomi-redmi-note-13-5g-128gb",
                BrandId = xiaomi, CategoryId = phones,
                Description = "Snapdragon 695 5G, 108MP camera, 6.67-inch 120Hz AMOLED display, 5000mAh battery with 33W fast charging.",
                ShortDescription = "108MP · 120Hz AMOLED · 5G",
                Price = 185000, CompareAtPrice = 215000,
                StockQty = 40, IsActive = true, IsFeatured = false,
                AverageRating = 4.2m, ReviewCount = 55, SoldCount = 97,
                WeightKg = 0.188m,
                AttributesJson = "{\"display\":\"6.67\\\" 120Hz AMOLED\",\"chip\":\"Snapdragon 695 5G\",\"ram\":\"6GB\",\"storage\":\"128GB\",\"camera\":\"108MP\",\"battery\":\"5000mAh 33W\",\"network\":\"5G\"}"
            },

            new Product
            {
                Id = new Guid("11111111-1013-0000-0000-000000000001"),
                SKU = "INX-GT20P-256",
                Name = "Infinix GT 20 Pro 256GB",
                Slug = "infinix-gt-20-pro-256gb",
                BrandId = infinix, CategoryId = phones,
                Description = "Dimensity 8200 Ultimate gaming chip. 144Hz AMOLED display. RGB CORTEX-X lighting system. 45W fast charging. Dedicated gaming triggers.",
                ShortDescription = "144Hz AMOLED · Dimensity 8200 · RGB",
                Price = 235000, CompareAtPrice = 270000,
                StockQty = 22, IsActive = true, IsFeatured = true,
                AverageRating = 4.3m, ReviewCount = 31, SoldCount = 43,
                WeightKg = 0.215m,
                AttributesJson = "{\"display\":\"6.78\\\" 144Hz AMOLED\",\"chip\":\"Dimensity 8200 Ultimate\",\"ram\":\"12GB\",\"storage\":\"256GB\",\"camera\":\"108MP\",\"battery\":\"5000mAh 45W\",\"network\":\"5G\",\"special\":\"RGB lighting + gaming triggers\"}"
            },

            new Product
            {
                Id = new Guid("11111111-1014-0000-0000-000000000001"),
                SKU = "INX-GT10P-256",
                Name = "Infinix GT 10 Pro 256GB",
                Slug = "infinix-gt-10-pro-256gb",
                BrandId = infinix, CategoryId = phones,
                Description = "Dimensity 8050, 6.67-inch 144Hz AMOLED display, RGB CORTEX-X lighting, 108MP camera, 4600mAh battery 45W.",
                ShortDescription = "144Hz AMOLED · Dimensity 8050 · RGB",
                Price = 185000, CompareAtPrice = 210000,
                StockQty = 25, IsActive = true, IsFeatured = false,
                AverageRating = 4.1m, ReviewCount = 28, SoldCount = 39,
                WeightKg = 0.210m,
                AttributesJson = "{\"display\":\"6.67\\\" 144Hz AMOLED\",\"chip\":\"Dimensity 8050\",\"ram\":\"8GB\",\"storage\":\"256GB\",\"camera\":\"108MP\",\"battery\":\"4600mAh 45W\",\"network\":\"5G\"}"
            },

            new Product
            {
                Id = new Guid("11111111-1015-0000-0000-000000000001"),
                SKU = "TECNO-C30P-256",
                Name = "Tecno Camon 30 Pro 5G 256GB",
                Slug = "tecno-camon-30-pro-5g-256gb",
                BrandId = tecno, CategoryId = phones,
                Description = "Dimensity 7020, 6.77-inch curved AMOLED 144Hz, 50MP rear camera with OIS, 5000mAh 45W fast charging. Stylish design.",
                ShortDescription = "50MP OIS · 144Hz · 5G",
                Price = 195000, CompareAtPrice = 230000,
                StockQty = 30, IsActive = true, IsFeatured = false,
                AverageRating = 4.2m, ReviewCount = 39, SoldCount = 52,
                WeightKg = 0.195m,
                AttributesJson = "{\"display\":\"6.77\\\" 144Hz curved AMOLED\",\"chip\":\"Dimensity 7020\",\"ram\":\"8GB\",\"storage\":\"256GB\",\"camera\":\"50MP OIS\",\"battery\":\"5000mAh 45W\",\"network\":\"5G\"}"
            },

            new Product
            {
                Id = new Guid("11111111-1016-0000-0000-000000000001"),
                SKU = "TECNO-PV6P-256",
                Name = "Tecno Pova 6 Pro 5G 256GB",
                Slug = "tecno-pova-6-pro-5g-256gb",
                BrandId = tecno, CategoryId = phones,
                Description = "Dimensity 6080, 6.78-inch FHD+ 120Hz IPS, 50MP camera, 6000mAh battery with 45W fast charging. Gaming-tuned design.",
                ShortDescription = "6000mAh · 45W · 5G · 120Hz",
                Price = 145000, CompareAtPrice = 175000,
                StockQty = 40, IsActive = true, IsFeatured = false,
                AverageRating = 4.0m, ReviewCount = 33, SoldCount = 61,
                WeightKg = 0.217m,
                AttributesJson = "{\"display\":\"6.78\\\" 120Hz IPS\",\"chip\":\"Dimensity 6080\",\"ram\":\"8GB\",\"storage\":\"256GB\",\"camera\":\"50MP\",\"battery\":\"6000mAh 45W\",\"network\":\"5G\"}"
            },

            new Product
            {
                Id = new Guid("11111111-1017-0000-0000-000000000001"),
                SKU = "REDMI-A3-64",
                Name = "Xiaomi Redmi A3 64GB",
                Slug = "xiaomi-redmi-a3-64gb",
                BrandId = xiaomi, CategoryId = phones,
                Description = "MediaTek Helio G36, 6.71-inch HD+ 90Hz display, 8MP camera, 5000mAh battery with 10W charging. Reliable everyday phone.",
                ShortDescription = "Helio G36 · 90Hz · 5000mAh",
                Price = 95000, CompareAtPrice = 115000,
                StockQty = 50, IsActive = true, IsFeatured = false,
                AverageRating = 3.9m, ReviewCount = 47, SoldCount = 112,
                WeightKg = 0.193m,
                AttributesJson = "{\"display\":\"6.71\\\" HD+ 90Hz IPS\",\"chip\":\"MediaTek Helio G36\",\"ram\":\"3GB\",\"storage\":\"64GB\",\"camera\":\"8MP\",\"battery\":\"5000mAh 10W\",\"network\":\"4G\"}"
            },

            new Product
            {
                Id = new Guid("11111111-1018-0000-0000-000000000001"),
                SKU = "INX-NOTE40P-256",
                Name = "Infinix Note 40 Pro+ 5G 256GB",
                Slug = "infinix-note-40-pro-plus-5g-256gb",
                BrandId = infinix, CategoryId = phones,
                Description = "Dimensity 6080 5G, 6.78-inch AMOLED 120Hz, 108MP OIS camera, 4600mAh with 100W wired + 20W wireless charging.",
                ShortDescription = "108MP OIS · 100W Charge · Wireless",
                Price = 210000, CompareAtPrice = 245000,
                StockQty = 25, IsActive = true, IsFeatured = false,
                AverageRating = 4.2m, ReviewCount = 26, SoldCount = 37,
                WeightKg = 0.207m,
                AttributesJson = "{\"display\":\"6.78\\\" 120Hz AMOLED\",\"chip\":\"Dimensity 6080\",\"ram\":\"8GB\",\"storage\":\"256GB\",\"camera\":\"108MP OIS\",\"battery\":\"4600mAh 100W wired + 20W wireless\",\"network\":\"5G\"}"
            },

            // ════════════════════════════════════════════════════════════════
            // LAPTOPS
            // ════════════════════════════════════════════════════════════════

            new Product
            {
                Id = new Guid("22222222-2001-0000-0000-000000000001"),
                SKU = "MBA-M4-13-16-256",
                Name = "Apple MacBook Air M4 13-inch 16GB 256GB",
                Slug = "apple-macbook-air-m4-13-inch-16gb-256gb",
                BrandId = apple, CategoryId = laptops,
                Description = "Apple M4 chip with 10-core CPU and 10-core GPU. 13.6-inch Liquid Retina display. Up to 18 hours battery. MagSafe charging. Two Thunderbolt 4 ports.",
                ShortDescription = "M4 chip · 18hr battery · 13.6\\\" Retina",
                Price = 1750000, CompareAtPrice = 1950000,
                StockQty = 8, IsActive = true, IsFeatured = true,
                AverageRating = 4.9m, ReviewCount = 57, SoldCount = 34,
                WeightKg = 1.24m,
                AttributesJson = "{\"display\":\"13.6\\\" Liquid Retina\",\"chip\":\"Apple M4\",\"ram\":\"16GB\",\"storage\":\"256GB SSD\",\"battery\":\"18 hours\",\"ports\":\"2x Thunderbolt 4 + MagSafe 3 + 3.5mm\",\"os\":\"macOS Sequoia\"}"
            },

            new Product
            {
                Id = new Guid("22222222-2002-0000-0000-000000000001"),
                SKU = "MBA-M4-15-16-256",
                Name = "Apple MacBook Air M4 15-inch 16GB 256GB",
                Slug = "apple-macbook-air-m4-15-inch-16gb-256gb",
                BrandId = apple, CategoryId = laptops,
                Description = "Apple M4 chip. Massive 15.3-inch Liquid Retina display. 18-hour battery life. 12MP Center Stage camera. MagSafe 3 charging.",
                ShortDescription = "M4 chip · 18hr battery · 15.3\\\" Retina",
                Price = 2100000, CompareAtPrice = 2350000,
                StockQty = 6, IsActive = true, IsFeatured = true,
                AverageRating = 4.9m, ReviewCount = 43, SoldCount = 27,
                WeightKg = 1.51m,
                AttributesJson = "{\"display\":\"15.3\\\" Liquid Retina\",\"chip\":\"Apple M4\",\"ram\":\"16GB\",\"storage\":\"256GB SSD\",\"battery\":\"18 hours\",\"ports\":\"2x Thunderbolt 4 + MagSafe 3 + 3.5mm\",\"os\":\"macOS Sequoia\"}"
            },

            new Product
            {
                Id = new Guid("22222222-2003-0000-0000-000000000001"),
                SKU = "MBP-M4-14-16-512",
                Name = "Apple MacBook Pro M4 14-inch 16GB 512GB",
                Slug = "apple-macbook-pro-m4-14-inch-16gb-512gb",
                BrandId = apple, CategoryId = laptops,
                Description = "M4 chip with hardware ray tracing. Stunning 14.2-inch Liquid Retina XDR ProMotion display at 120Hz. 24 hours battery. Three Thunderbolt 4 ports, HDMI, SD card.",
                ShortDescription = "M4 Pro · 24hr battery · ProMotion XDR",
                Price = 2750000, CompareAtPrice = 3050000,
                StockQty = 4, IsActive = true, IsFeatured = true,
                AverageRating = 4.9m, ReviewCount = 34, SoldCount = 18,
                WeightKg = 1.55m,
                AttributesJson = "{\"display\":\"14.2\\\" Liquid Retina XDR ProMotion 120Hz\",\"chip\":\"Apple M4\",\"ram\":\"16GB\",\"storage\":\"512GB SSD\",\"battery\":\"24 hours\",\"ports\":\"3x Thunderbolt 4 + HDMI + SD + MagSafe 3\",\"os\":\"macOS Sequoia\"}"
            },

            // ════════════════════════════════════════════════════════════════
            // CONSOLES
            // ════════════════════════════════════════════════════════════════

            new Product
            {
                Id = new Guid("33333333-3001-0000-0000-000000000001"),
                SKU = "PS5-SLIM-DISC",
                Name = "Sony PlayStation 5 Slim Disc Edition",
                Slug = "sony-playstation-5-slim-disc-edition",
                BrandId = sony, CategoryId = consoles,
                Description = "PlayStation 5 Slim with Ultra HD Blu-ray disc drive. 1TB custom SSD. DualSense wireless controller included. 4K gaming at up to 120fps. Haptic feedback and adaptive triggers.",
                ShortDescription = "PS5 Slim · 1TB SSD · 4K 120fps · Disc Drive",
                Price = 680000, CompareAtPrice = 750000,
                StockQty = 3, IsActive = true, IsFeatured = true,
                AverageRating = 4.9m, ReviewCount = 218, SoldCount = 184,
                WeightKg = 3.2m,
                AttributesJson = "{\"storage\":\"1TB custom SSD\",\"optical\":\"Ultra HD Blu-ray\",\"resolution\":\"4K 120fps\",\"network\":\"WiFi 6\",\"usb\":\"USB-A + USB-C\",\"controller\":\"1x DualSense\"}"
            },

            new Product
            {
                Id = new Guid("33333333-3002-0000-0000-000000000001"),
                SKU = "XBOX-SX-1TB",
                Name = "Microsoft Xbox Series X 1TB",
                Slug = "microsoft-xbox-series-x-1tb",
                BrandId = microsoft, CategoryId = consoles,
                Description = "The fastest, most powerful Xbox ever. Custom AMD Zen 2 CPU and RDNA 2 GPU. 12 teraflops of gaming performance. 4K gaming at 60fps, up to 120fps. Quick Resume.",
                ShortDescription = "12 Teraflops · 4K 120fps · Quick Resume",
                Price = 650000, CompareAtPrice = 720000,
                StockQty = 2, IsActive = true, IsFeatured = true,
                AverageRating = 4.8m, ReviewCount = 142, SoldCount = 97,
                WeightKg = 4.45m,
                AttributesJson = "{\"cpu\":\"Custom AMD Zen 2 8-core\",\"gpu\":\"Custom RDNA 2 12TF\",\"storage\":\"1TB custom NVMe SSD\",\"optical\":\"4K Blu-ray\",\"resolution\":\"4K 120fps\",\"network\":\"WiFi 5\"}"
            },

            new Product
            {
                Id = new Guid("33333333-3003-0000-0000-000000000001"),
                SKU = "NSW-OLED-WHT",
                Name = "Nintendo Switch OLED White",
                Slug = "nintendo-switch-oled-white",
                BrandId = nintendo, CategoryId = consoles,
                Description = "Vibrant 7-inch OLED screen. Enhanced audio. Wide adjustable stand. 64GB internal storage. Dock for TV play. Play at home or on the go.",
                ShortDescription = "7\\\" OLED · 64GB · TV & Handheld modes",
                Price = 320000, CompareAtPrice = 370000,
                StockQty = 6, IsActive = true, IsFeatured = false,
                AverageRating = 4.7m, ReviewCount = 167, SoldCount = 142,
                WeightKg = 0.42m,
                AttributesJson = "{\"display\":\"7\\\" OLED\",\"storage\":\"64GB\",\"battery\":\"4.5-9 hours\",\"dock\":\"Included\",\"modes\":\"TV / Tabletop / Handheld\"}"
            },

            // ════════════════════════════════════════════════════════════════
            // GAMING ACCESSORIES
            // ════════════════════════════════════════════════════════════════

            new Product
            {
                Id = new Guid("44444444-4001-0000-0000-000000000001"),
                SKU = "PS5-DS-WHT",
                Name = "Sony PS5 DualSense Wireless Controller White",
                Slug = "sony-ps5-dualsense-controller-white",
                BrandId = sony, CategoryId = gamingAcc,
                Description = "Immersive haptic feedback and adaptive triggers. Built-in microphone. USB-C charging. Integrated rechargeable battery. Textured grips.",
                ShortDescription = "Haptic Feedback · Adaptive Triggers · USB-C",
                Price = 65000, CompareAtPrice = 75000,
                StockQty = 15, IsActive = true, IsFeatured = false,
                AverageRating = 4.7m, ReviewCount = 98, SoldCount = 134,
                WeightKg = 0.28m,
                AttributesJson = "{\"compatibility\":\"PS5\",\"connectivity\":\"Wireless + USB-C\",\"battery\":\"Rechargeable\",\"features\":\"Haptic feedback, Adaptive triggers, Built-in mic\",\"color\":\"White\"}"
            },

            new Product
            {
                Id = new Guid("44444444-4002-0000-0000-000000000001"),
                SKU = "PS5-DS-MID-BLK",
                Name = "Sony PS5 DualSense Midnight Black Controller",
                Slug = "sony-ps5-dualsense-midnight-black-controller",
                BrandId = sony, CategoryId = gamingAcc,
                Description = "All the immersive DualSense features in a sleek Midnight Black colourway. Haptic feedback, adaptive triggers, USB-C charging.",
                ShortDescription = "Haptic Feedback · Adaptive Triggers · Black",
                Price = 65000, CompareAtPrice = 75000,
                StockQty = 12, IsActive = true, IsFeatured = false,
                AverageRating = 4.7m, ReviewCount = 76, SoldCount = 98,
                WeightKg = 0.28m,
                AttributesJson = "{\"compatibility\":\"PS5\",\"connectivity\":\"Wireless + USB-C\",\"battery\":\"Rechargeable\",\"features\":\"Haptic feedback, Adaptive triggers, Built-in mic\",\"color\":\"Midnight Black\"}"
            },

            new Product
            {
                Id = new Guid("44444444-4003-0000-0000-000000000001"),
                SKU = "HXCLD2-BLK",
                Name = "HyperX Cloud II Gaming Headset Black",
                Slug = "hyperx-cloud-ii-gaming-headset-black",
                BrandId = hyperx, CategoryId = compAcc,
                Description = "7.1 virtual surround sound. 53mm drivers. Memory foam leatherette ear cushions. Detachable noise-cancelling microphone. Compatible with PS5, Xbox, PC, Switch.",
                ShortDescription = "7.1 Surround · 53mm Drivers · Noise-Cancelling Mic",
                Price = 55000, CompareAtPrice = 68000,
                StockQty = 20, IsActive = true, IsFeatured = false,
                AverageRating = 4.6m, ReviewCount = 87, SoldCount = 113,
                WeightKg = 0.31m,
                AttributesJson = "{\"sound\":\"7.1 Virtual Surround\",\"drivers\":\"53mm\",\"mic\":\"Detachable noise-cancelling\",\"connectivity\":\"3.5mm + USB adapter\",\"compatibility\":\"PS5, Xbox, PC, Switch, Mobile\"}"
            },

            new Product
            {
                Id = new Guid("44444444-4004-0000-0000-000000000001"),
                SKU = "REDR-K552-RGB",
                Name = "Redragon K552 Kumara Mechanical RGB Keyboard",
                Slug = "redragon-k552-kumara-mechanical-rgb-keyboard",
                BrandId = redragon, CategoryId = compAcc,
                Description = "Compact TKL 87-key layout with tactile mechanical switches. Per-key RGB backlighting with 9 preset modes. Durable aluminium plate. Braided USB cable.",
                ShortDescription = "87-Key TKL · RGB · Mechanical · Aluminium",
                Price = 25000, CompareAtPrice = 32000,
                StockQty = 30, IsActive = true, IsFeatured = false,
                AverageRating = 4.4m, ReviewCount = 63, SoldCount = 89,
                WeightKg = 0.87m,
                AttributesJson = "{\"layout\":\"87-key TKL compact\",\"switches\":\"Blue mechanical (tactile clicky)\",\"backlight\":\"Per-key RGB 9 modes\",\"frame\":\"Aluminium plate\",\"cable\":\"Braided USB\",\"dimensions\":\"360 x 124 x 35mm\"}"
            },

            new Product
            {
                Id = new Guid("44444444-4005-0000-0000-000000000001"),
                SKU = "RAZER-DAV3-HS",
                Name = "Razer DeathAdder V3 HyperSpeed Gaming Mouse",
                Slug = "razer-deathadder-v3-hyperspeed-gaming-mouse",
                BrandId = razer, CategoryId = compAcc,
                Description = "Ultra-lightweight 63g design. Focus X 26K optical sensor. 300-hour battery life on a single AA battery. Iconic ergonomic shape. HyperSpeed 2.4GHz wireless.",
                ShortDescription = "63g · 26K DPI · 300hr battery · Wireless",
                Price = 38000, CompareAtPrice = 48000,
                StockQty = 22, IsActive = true, IsFeatured = false,
                AverageRating = 4.6m, ReviewCount = 79, SoldCount = 115,
                WeightKg = 0.063m,
                AttributesJson = "{\"weight\":\"63g\",\"sensor\":\"Focus X 26K optical\",\"dpi\":\"100-26,000 DPI\",\"connectivity\":\"HyperSpeed 2.4GHz wireless\",\"battery\":\"300 hours (AA)\",\"buttons\":\"6 programmable\"}"
            },

            new Product
            {
                Id = new Guid("44444444-4006-0000-0000-000000000001"),
                SKU = "GTR-PRO-CHAIR",
                Name = "GTRacing Gaming Chair Pro Series Black",
                Slug = "gtracing-gaming-chair-pro-series-black",
                BrandId = gtracing, CategoryId = otherCats,
                Description = "Ergonomic gaming chair with lumbar support pillow, adjustable armrests, recline 90-170°, and breathable PU leather. Supports up to 150kg.",
                ShortDescription = "Ergonomic · Lumbar Support · 170° Recline",
                Price = 85000, CompareAtPrice = 110000,
                StockQty = 1, IsActive = true, IsFeatured = false,
                AverageRating = 4.3m, ReviewCount = 44, SoldCount = 28,
                WeightKg = 21m,
                AttributesJson = "{\"material\":\"PU Leather + Mesh\",\"recline\":\"90-170 degrees\",\"armrests\":\"4D adjustable\",\"lumbar\":\"Adjustable lumbar + headrest pillow\",\"max_weight\":\"150kg\",\"color\":\"Black\"}"
            },

            new Product
            {
                Id = new Guid("44444444-4007-0000-0000-000000000001"),
                SKU = "CM-NOTEPAL-X3",
                Name = "Cooler Master NotePal X3 Laptop Cooling Pad",
                Slug = "cooler-master-notepal-x3-cooling-pad",
                BrandId = coolerMaster, CategoryId = compAcc,
                Description = "200mm silent fan, USB hub with 3 ports, ergonomic adjustable height stand, blue LED fan. Compatible with laptops up to 17 inches.",
                ShortDescription = "200mm Fan · USB Hub · 17\\\" Compatible",
                Price = 18000, CompareAtPrice = 24000,
                StockQty = 25, IsActive = true, IsFeatured = false,
                AverageRating = 4.3m, ReviewCount = 52, SoldCount = 76,
                WeightKg = 0.9m,
                AttributesJson = "{\"fan\":\"200mm silent\",\"usb_hub\":\"3-port USB hub\",\"compatibility\":\"Up to 17-inch laptops\",\"led\":\"Blue LED\",\"height_adjust\":\"Yes\"}"
            },

            new Product
            {
                Id = new Guid("44444444-4008-0000-0000-000000000001"),
                SKU = "UGREEN-HDMI21-2M",
                Name = "UGREEN HDMI 2.1 Cable 8K 2 Metres",
                Slug = "ugreen-hdmi-2-1-cable-8k-2-metres",
                BrandId = ugreen, CategoryId = cables,
                Description = "Supports 8K 60Hz, 4K 144Hz, 4K 120fps gaming. Supports eARC, VRR, ALLM. Compatible with PS5, Xbox Series X, PC, TV, projectors.",
                ShortDescription = "8K 60Hz · 4K 144Hz · VRR · eARC",
                Price = 5500, CompareAtPrice = 8000,
                StockQty = 60, IsActive = true, IsFeatured = false,
                AverageRating = 4.5m, ReviewCount = 88, SoldCount = 167,
                WeightKg = 0.12m,
                AttributesJson = "{\"version\":\"HDMI 2.1\",\"max_resolution\":\"8K 60Hz / 4K 144Hz\",\"length\":\"2 metres\",\"features\":\"eARC, VRR, ALLM, HDR\",\"compatibility\":\"PS5, Xbox, PC, TV\"}"
            },

            // ════════════════════════════════════════════════════════════════
            // CHARGERS
            // ════════════════════════════════════════════════════════════════

            new Product
            {
                Id = new Guid("55555555-5001-0000-0000-000000000001"),
                SKU = "APPLE-20W-USBC",
                Name = "Apple 20W USB-C Power Adapter",
                Slug = "apple-20w-usbc-power-adapter",
                BrandId = apple, CategoryId = chargers,
                Description = "Fast-charge iPhone 8 or later up to 50% in around 30 minutes. Compatible with all USB-C devices. Compact fold-prong design. Original Apple accessory.",
                ShortDescription = "20W · Fast Charge · Original Apple",
                Price = 18000, CompareAtPrice = 22000,
                StockQty = 50, IsActive = true, IsFeatured = false,
                AverageRating = 4.6m, ReviewCount = 124, SoldCount = 287,
                WeightKg = 0.045m,
                AttributesJson = "{\"wattage\":\"20W\",\"connector\":\"USB-C\",\"fast_charge\":\"iPhone 50% in 30min\",\"compatibility\":\"All USB-C devices\",\"color\":\"White\"}"
            },

            new Product
            {
                Id = new Guid("55555555-5002-0000-0000-000000000001"),
                SKU = "ANKER-65W-GAN",
                Name = "Anker 65W GaN USB-C Wall Charger",
                Slug = "anker-65w-gan-usbc-wall-charger",
                BrandId = anker, CategoryId = chargers,
                Description = "65W GaN technology in a compact body. Charges MacBook, iPad, and iPhone simultaneously via 2 USB-C + 1 USB-A ports. PIQ 3.0 fast charging.",
                ShortDescription = "65W GaN · 3 Ports · MacBook Compatible",
                Price = 22000, CompareAtPrice = 28000,
                StockQty = 40, IsActive = true, IsFeatured = false,
                AverageRating = 4.7m, ReviewCount = 98, SoldCount = 178,
                WeightKg = 0.073m,
                AttributesJson = "{\"wattage\":\"65W total\",\"technology\":\"GaN\",\"ports\":\"2x USB-C + 1x USB-A\",\"fast_charge\":\"PIQ 3.0\",\"compatibility\":\"MacBook, iPad, iPhone, Android\"}"
            },

            new Product
            {
                Id = new Guid("55555555-5003-0000-0000-000000000001"),
                SKU = "ORAIMO-33W-CHGR",
                Name = "Oraimo 33W Super Fast Charger",
                Slug = "oraimo-33w-super-fast-charger",
                BrandId = oraimo, CategoryId = chargers,
                Description = "33W GaN fast charging via USB-C. Compatible with Samsung, Xiaomi, TECNO, Infinix and all USB-C smartphones. Compact foldable design.",
                ShortDescription = "33W GaN · USB-C · Universal Fast Charge",
                Price = 8500, CompareAtPrice = 12000,
                StockQty = 80, IsActive = true, IsFeatured = false,
                AverageRating = 4.3m, ReviewCount = 76, SoldCount = 234,
                WeightKg = 0.04m,
                AttributesJson = "{\"wattage\":\"33W\",\"technology\":\"GaN\",\"connector\":\"USB-C\",\"compatibility\":\"Samsung, Xiaomi, TECNO, Infinix, Universal\"}"
            },

            new Product
            {
                Id = new Guid("55555555-5004-0000-0000-000000000001"),
                SKU = "XIAOMI-67W-GAN",
                Name = "Xiaomi 67W GaN Type-C Fast Charger",
                Slug = "xiaomi-67w-gan-type-c-fast-charger",
                BrandId = xiaomi, CategoryId = chargers,
                Description = "67W GaN fast charger with dual port — USB-C + USB-A. Supports Xiaomi HyperCharge, Qualcomm QC 3.0, PD 3.0. Charges MacBook Air in 90 minutes.",
                ShortDescription = "67W GaN · USB-C + USB-A · HyperCharge",
                Price = 9000, CompareAtPrice = 13000,
                StockQty = 60, IsActive = true, IsFeatured = false,
                AverageRating = 4.5m, ReviewCount = 62, SoldCount = 198,
                WeightKg = 0.06m,
                AttributesJson = "{\"wattage\":\"67W\",\"technology\":\"GaN\",\"ports\":\"1x USB-C + 1x USB-A\",\"protocols\":\"HyperCharge, QC 3.0, PD 3.0\"}"
            },

            // ════════════════════════════════════════════════════════════════
            // POWER BANKS
            // ════════════════════════════════════════════════════════════════

            new Product
            {
                Id = new Guid("66666666-6001-0000-0000-000000000001"),
                SKU = "ORAIMO-PB-20K",
                Name = "Oraimo OPB-P20000P 20000mAh Power Bank",
                Slug = "oraimo-opb-p20000p-20000mah-power-bank",
                BrandId = oraimo, CategoryId = powerBanks,
                Description = "20000mAh capacity with 22.5W PD fast charging. Dual USB-A + USB-C ports. 15W wireless charging. LCD display. Charges iPhone 16 ~4 times.",
                ShortDescription = "20000mAh · 22.5W PD · Wireless Charging",
                Price = 22000, CompareAtPrice = 28000,
                StockQty = 45, IsActive = true, IsFeatured = false,
                AverageRating = 4.4m, ReviewCount = 89, SoldCount = 198,
                WeightKg = 0.38m,
                AttributesJson = "{\"capacity\":\"20000mAh\",\"output\":\"22.5W PD fast charge\",\"wireless\":\"15W\",\"ports\":\"2x USB-A + 1x USB-C\",\"display\":\"LCD battery level\"}"
            },

            new Product
            {
                Id = new Guid("66666666-6002-0000-0000-000000000001"),
                SKU = "ANKER-PC-26800",
                Name = "Anker PowerCore 26800mAh Power Bank",
                Slug = "anker-powercore-26800mah-power-bank",
                BrandId = anker, CategoryId = powerBanks,
                Description = "Massive 26800mAh capacity with 30W USB-C PD fast charging. Three output ports — charge 3 devices simultaneously. Ultra-high cell quality for 500+ charge cycles.",
                ShortDescription = "26800mAh · 30W PD · 3 Ports",
                Price = 35000, CompareAtPrice = 45000,
                StockQty = 25, IsActive = true, IsFeatured = false,
                AverageRating = 4.7m, ReviewCount = 118, SoldCount = 156,
                WeightKg = 0.49m,
                AttributesJson = "{\"capacity\":\"26800mAh\",\"output\":\"30W USB-C PD\",\"ports\":\"2x USB-A + 1x USB-C\",\"simultaneous\":\"3 devices at once\",\"cycles\":\"500+\"}"
            },

            new Product
            {
                Id = new Guid("66666666-6003-0000-0000-000000000001"),
                SKU = "XIAOMI-PB-33W-20K",
                Name = "Xiaomi 33W Power Bank 20000mAh",
                Slug = "xiaomi-33w-power-bank-20000mah",
                BrandId = xiaomi, CategoryId = powerBanks,
                Description = "20000mAh with 33W fast charging via USB-C PD. Supports Xiaomi HyperCharge. Dual USB-A output. LCD power level display. Slim lightweight design.",
                ShortDescription = "20000mAh · 33W · HyperCharge · LCD",
                Price = 28000, CompareAtPrice = 35000,
                StockQty = 35, IsActive = true, IsFeatured = false,
                AverageRating = 4.5m, ReviewCount = 74, SoldCount = 134,
                WeightKg = 0.41m,
                AttributesJson = "{\"capacity\":\"20000mAh\",\"output\":\"33W USB-C PD\",\"ports\":\"2x USB-A + 1x USB-C\",\"display\":\"LED battery indicator\"}"
            },

            // ════════════════════════════════════════════════════════════════
            // AUDIO
            // ════════════════════════════════════════════════════════════════

            new Product
            {
                Id = new Guid("77777777-7001-0000-0000-000000000001"),
                SKU = "APP-AIRPODS-PRO2",
                Name = "Apple AirPods Pro 2nd Gen USB-C",
                Slug = "apple-airpods-pro-2nd-gen-usbc",
                BrandId = apple, CategoryId = audio,
                Description = "Active Noise Cancellation with Adaptive Audio. Personalised Spatial Audio. Transparency mode. H2 chip. USB-C charging case. 6 hours listening with ANC + 30 hours total.",
                ShortDescription = "ANC · Adaptive Audio · USB-C · H2 Chip",
                Price = 320000, CompareAtPrice = 369000,
                StockQty = 18, IsActive = true, IsFeatured = true,
                AverageRating = 4.9m, ReviewCount = 178, SoldCount = 262,
                WeightKg = 0.061m,
                AttributesJson = "{\"anc\":\"Active Noise Cancellation + Adaptive Audio\",\"chip\":\"H2\",\"battery\":\"6hr + 30hr case\",\"charging\":\"USB-C + MagSafe\",\"water_resistance\":\"IPX4 + IPX4 case\",\"connectivity\":\"Bluetooth 5.3\"}"
            },

            new Product
            {
                Id = new Guid("77777777-7002-0000-0000-000000000001"),
                SKU = "ORAIMO-FREEPODS5",
                Name = "Oraimo FreePods 5 TWS Earbuds",
                Slug = "oraimo-freepods-5-tws-earbuds",
                BrandId = oraimo, CategoryId = audio,
                Description = "40dB Active Noise Cancellation. 13mm drivers. 30 hours total battery with case. IPX5 water resistance. Rapid charge — 10 minutes = 2 hours playback.",
                ShortDescription = "40dB ANC · 30hr battery · IPX5 · TWS",
                Price = 12500, CompareAtPrice = 18000,
                StockQty = 60, IsActive = true, IsFeatured = false,
                AverageRating = 4.2m, ReviewCount = 94, SoldCount = 287,
                WeightKg = 0.042m,
                AttributesJson = "{\"anc\":\"40dB Active Noise Cancellation\",\"drivers\":\"13mm\",\"battery\":\"7hr + 23hr case = 30hr total\",\"water\":\"IPX5\",\"fast_charge\":\"10min = 2hr\",\"connectivity\":\"Bluetooth 5.3\"}"
            },

            new Product
            {
                Id = new Guid("77777777-7003-0000-0000-000000000001"),
                SKU = "JBL-CHARGE5-BLK",
                Name = "JBL Charge 5 Portable Bluetooth Speaker",
                Slug = "jbl-charge-5-portable-bluetooth-speaker",
                BrandId = jbl, CategoryId = audio,
                Description = "Powerful JBL Pro Sound. IP67 waterproof and dustproof. 20 hours battery life. Built-in 7500mAh powerbank charges your devices. PartyBoost to link speakers.",
                ShortDescription = "IP67 · 20hr battery · Built-in Powerbank",
                Price = 89000, CompareAtPrice = 110000,
                StockQty = 20, IsActive = true, IsFeatured = false,
                AverageRating = 4.6m, ReviewCount = 112, SoldCount = 198,
                WeightKg = 0.96m,
                AttributesJson = "{\"waterproof\":\"IP67\",\"battery\":\"20 hours\",\"powerbank\":\"7500mAh built-in\",\"connectivity\":\"Bluetooth 5.1\",\"feature\":\"PartyBoost multi-speaker sync\",\"color\":\"Black\"}"
            },

            // ════════════════════════════════════════════════════════════════
            // PHONE ACCESSORIES
            // ════════════════════════════════════════════════════════════════

            new Product
            {
                Id = new Guid("88888888-8001-0000-0000-000000000001"),
                SKU = "SPIGEN-IP16PM-UH",
                Name = "Spigen iPhone 16 Pro Max Case Ultra Hybrid",
                Slug = "spigen-iphone-16-pro-max-case-ultra-hybrid",
                BrandId = spigen, CategoryId = accessories,
                Description = "Military-grade protection with crystal-clear back. Air Cushion Technology corners. Scratch-resistant raised bezels. Compatible with MagSafe. Yellowing resistant.",
                ShortDescription = "Military-grade · MagSafe · Crystal Clear",
                Price = 8500, CompareAtPrice = 12000,
                StockQty = 80, IsActive = true, IsFeatured = false,
                AverageRating = 4.7m, ReviewCount = 134, SoldCount = 312,
                WeightKg = 0.045m,
                AttributesJson = "{\"model\":\"iPhone 16 Pro Max\",\"material\":\"TPU + PC hybrid\",\"protection\":\"Military-grade MIL-STD-810G\",\"magsafe\":\"Compatible\",\"color\":\"Crystal Clear\"}"
            },

            new Product
            {
                Id = new Guid("88888888-8002-0000-0000-000000000001"),
                SKU = "SAMSNG-S25U-CASE",
                Name = "Samsung Galaxy S25 Ultra Silicone Case Black",
                Slug = "samsung-galaxy-s25-ultra-silicone-case-black",
                BrandId = samsung, CategoryId = accessories,
                Description = "Official Samsung silicone case. S Pen slot compatible. Precise cutouts for all buttons and ports. Soft microfiber lining. Wireless charging compatible.",
                ShortDescription = "Official · S Pen Slot · Wireless Charging",
                Price = 6000, CompareAtPrice = 9500,
                StockQty = 70, IsActive = true, IsFeatured = false,
                AverageRating = 4.4m, ReviewCount = 67, SoldCount = 178,
                WeightKg = 0.038m,
                AttributesJson = "{\"model\":\"Samsung Galaxy S25 Ultra\",\"material\":\"Soft Silicone\",\"spen_slot\":\"Yes\",\"wireless_charge\":\"Compatible\",\"color\":\"Black\"}"
            },

            new Product
            {
                Id = new Guid("88888888-8003-0000-0000-000000000001"),
                SKU = "IP16PM-TEMPERED",
                Name = "iPhone 16 Pro Max 9H Tempered Glass Screen Protector",
                Slug = "iphone-16-pro-max-9h-tempered-glass-screen-protector",
                BrandId = spigen, CategoryId = accessories,
                Description = "9H hardness tempered glass. Ultra-thin 0.3mm profile. Oleophobic coating. Case-friendly design. Bubble-free adhesive. Pack of 2.",
                ShortDescription = "9H Glass · 0.3mm · Oleophobic · 2-Pack",
                Price = 5000, CompareAtPrice = 8000,
                StockQty = 100, IsActive = true, IsFeatured = false,
                AverageRating = 4.5m, ReviewCount = 98, SoldCount = 456,
                WeightKg = 0.02m,
                AttributesJson = "{\"model\":\"iPhone 16 Pro Max\",\"hardness\":\"9H tempered glass\",\"thickness\":\"0.3mm\",\"quantity\":\"2 pack\",\"coating\":\"Oleophobic\"}"
            },

            new Product
            {
                Id = new Guid("88888888-8004-0000-0000-000000000001"),
                SKU = "SGS25-TEMPERED",
                Name = "Samsung Galaxy S25 Ultra Screen Protector 2-Pack",
                Slug = "samsung-galaxy-s25-ultra-screen-protector-2-pack",
                BrandId = spigen, CategoryId = accessories,
                Description = "9H hardness tempered glass designed for Galaxy S25 Ultra. Curved edge compatibility. Case-friendly. Fingerprint sensor compatible. Pack of 2.",
                ShortDescription = "9H Glass · Curved Edge · Fingerprint OK",
                Price = 3500, CompareAtPrice = 6000,
                StockQty = 100, IsActive = true, IsFeatured = false,
                AverageRating = 4.3m, ReviewCount = 76, SoldCount = 312,
                WeightKg = 0.018m,
                AttributesJson = "{\"model\":\"Samsung Galaxy S25 Ultra\",\"hardness\":\"9H tempered glass\",\"edge\":\"Curved compatible\",\"quantity\":\"2 pack\"}"
            },

            new Product
            {
                Id = new Guid("88888888-8005-0000-0000-000000000001"),
                SKU = "ANKER-USBC-240W",
                Name = "Anker 240W USB-C Cable 2 Metres Braided",
                Slug = "anker-240w-usbc-cable-2-metres-braided",
                BrandId = anker, CategoryId = cables,
                Description = "240W maximum power delivery. Supports 4K 60Hz video output. Braided nylon for durability. Compatible with MacBook Pro, iPad Pro, Samsung, and all USB-C devices.",
                ShortDescription = "240W PD · 4K 60Hz · 2M Braided",
                Price = 12500, CompareAtPrice = 16500,
                StockQty = 80, IsActive = true, IsFeatured = false,
                AverageRating = 4.7m, ReviewCount = 156, SoldCount = 423,
                WeightKg = 0.08m,
                AttributesJson = "{\"wattage\":\"240W\",\"video\":\"4K 60Hz\",\"length\":\"2 metres\",\"material\":\"Braided nylon\",\"connector\":\"USB-C to USB-C\",\"compatibility\":\"Universal USB-C\"}"
            },

            new Product
            {
                Id = new Guid("88888888-8006-0000-0000-000000000001"),
                SKU = "ORAIMO-USBC-100W",
                Name = "Oraimo 100W USB-C Braided Cable 1 Metre",
                Slug = "oraimo-100w-usbc-braided-cable-1-metre",
                BrandId = oraimo, CategoryId = cables,
                Description = "100W fast charging USB-C cable. Nylon braided for extra durability. Compatible with all USB-C smartphones, tablets, and laptops.",
                ShortDescription = "100W · Nylon Braided · 1 Metre",
                Price = 5000, CompareAtPrice = 7500,
                StockQty = 100, IsActive = true, IsFeatured = false,
                AverageRating = 4.3m, ReviewCount = 88, SoldCount = 356,
                WeightKg = 0.04m,
                AttributesJson = "{\"wattage\":\"100W\",\"length\":\"1 metre\",\"material\":\"Nylon braided\",\"connector\":\"USB-C to USB-C\"}"
            },

            // ════════════════════════════════════════════════════════════════
            // SMARTWATCHES
            // ════════════════════════════════════════════════════════════════

            new Product
            {
                Id = new Guid("99999999-9001-0000-0000-000000000001"),
                SKU = "AW-S10-45-BLK",
                Name = "Apple Watch Series 10 45mm Black",
                Slug = "apple-watch-series-10-45mm-black",
                BrandId = apple, CategoryId = smartWatches,
                Description = "Thinnest Apple Watch ever. Advanced health features: sleep apnea notifications, ECG, blood oxygen. Crash detection. Always-On Retina display. 18-hour battery.",
                ShortDescription = "Thinnest ever · Sleep Apnea · ECG · AOD",
                Price = 580000, CompareAtPrice = 650000,
                StockQty = 12, IsActive = true, IsFeatured = true,
                AverageRating = 4.8m, ReviewCount = 87, SoldCount = 103,
                WeightKg = 0.042m,
                AttributesJson = "{\"display\":\"45mm Always-On Retina LTPO\",\"health\":\"ECG, Blood Oxygen, Sleep Apnea, Temperature\",\"battery\":\"18 hours\",\"water\":\"50m swim-proof\",\"connectivity\":\"GPS + Cellular\",\"os\":\"watchOS 11\"}"
            },

            new Product
            {
                Id = new Guid("99999999-9002-0000-0000-000000000001"),
                SKU = "SGW7-44-BLK",
                Name = "Samsung Galaxy Watch 7 44mm Black",
                Slug = "samsung-galaxy-watch-7-44mm-black",
                BrandId = samsung, CategoryId = smartWatches,
                Description = "Exynos W1000 5-core chip. Advanced health monitoring: body composition, sleep score, temperature sensor. Galaxy AI. 40-hour battery. 5ATM + IP68.",
                ShortDescription = "Exynos W1000 · Galaxy AI · 40hr battery",
                Price = 320000, CompareAtPrice = 375000,
                StockQty = 15, IsActive = true, IsFeatured = false,
                AverageRating = 4.5m, ReviewCount = 56, SoldCount = 74,
                WeightKg = 0.033m,
                AttributesJson = "{\"chip\":\"Exynos W1000\",\"display\":\"44mm Super AMOLED\",\"health\":\"Body composition, sleep, temperature\",\"battery\":\"40 hours\",\"water\":\"5ATM + IP68\",\"ai\":\"Galaxy AI\"}"
            },

            // ════════════════════════════════════════════════════════════════
            // RING LIGHTS & STANDS
            // ════════════════════════════════════════════════════════════════

            new Product
            {
                Id = new Guid("AAAAAAAA-A001-0000-0000-000000000001"),
                SKU = "NEEWER-18RL-KIT",
                Name = "Neewer 18-Inch LED Ring Light Kit with Stand",
                Slug = "neewer-18-inch-led-ring-light-kit-with-stand",
                BrandId = neewer, CategoryId = accessories,
                Description = "18-inch LED ring light, 3200-5500K bi-colour temperature, 200cm adjustable light stand, phone holder mount, carrying bag. Perfect for TikTok, YouTube, portraits, video calls.",
                ShortDescription = "18\\\" LED · Bi-colour · Stand + Phone Mount",
                Price = 32000, CompareAtPrice = 42000,
                StockQty = 20, IsActive = true, IsFeatured = false,
                AverageRating = 4.4m, ReviewCount = 67, SoldCount = 89,
                WeightKg = 2.8m,
                AttributesJson = "{\"diameter\":\"18 inches\",\"colour_temp\":\"3200K-5500K bi-colour\",\"stand\":\"200cm adjustable\",\"phone_holder\":\"Included\",\"bag\":\"Carrying bag included\",\"uses\":\"Selfie, YouTube, TikTok, Portrait\"}"
            },

            new Product
            {
                Id = new Guid("AAAAAAAA-A002-0000-0000-000000000001"),
                SKU = "FLEXSTAND-PHONE",
                Name = "Flexible Gooseneck Phone Stand & Holder",
                Slug = "flexible-gooseneck-phone-stand-holder",
                BrandId = neewer, CategoryId = accessories,
                Description = "360° flexible gooseneck arm with strong clamp. Compatible with all smartphones. Adjustable viewing angle. Hands-free for video calls, watching, recipes. Desk clip included.",
                ShortDescription = "360° Flexible · Clamp Mount · Universal",
                Price = 4500, CompareAtPrice = 7000,
                StockQty = 100, IsActive = true, IsFeatured = false,
                AverageRating = 4.2m, ReviewCount = 88, SoldCount = 234,
                WeightKg = 0.22m,
                AttributesJson = "{\"arm\":\"Flexible gooseneck\",\"rotation\":\"360 degrees\",\"mount\":\"Desk clamp\",\"compatibility\":\"All smartphones 4-7 inch\",\"uses\":\"Video calls, YouTube, recipes\"}"
            },
        };

        HashSet<string> existingSkus = (await context.Products
            .Select(p => p.SKU)
            .ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        HashSet<Guid> existingProductIds = (await context.Products
            .Select(p => p.Id)
            .ToListAsync()).ToHashSet();

        List<Product> missingProducts = products
            .Where(p => !existingSkus.Contains(p.SKU) && !existingProductIds.Contains(p.Id))
            .ToList();

        if (missingProducts.Count == 0) return;

        await context.Products.AddRangeAsync(missingProducts);
        await context.SaveChangesAsync();
    }

    // ── Catalog 2026 (38-product catalog swap: 34 new inactive + 4 renamed live) ─

    private static async Task SeedCatalog2026Async(ShopFresherzDbContext context)
    {
        Dictionary<string, Guid> brandIdsByName = (await context.Brands.ToListAsync())
            .GroupBy(b => b.Name.Trim().ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First().Id);
        Dictionary<string, int> categoryIds = await context.Categories
            .ToDictionaryAsync(c => c.Slug, c => c.Id);

        Guid zealot   = brandIdsByName["zealot"];
        Guid jbl      = brandIdsByName["jbl"];
        Guid sony     = brandIdsByName["sony"];
        Guid generic  = brandIdsByName["generic"];
        Guid logitech = brandIdsByName["logitech"];
        Guid dji      = brandIdsByName["dji"];
        Guid maono    = brandIdsByName["maono"];
        Guid boya     = brandIdsByName["boya"];
        Guid godox    = brandIdsByName["godox"];
        Guid newAge   = brandIdsByName["new age"];

        int earbuds           = categoryIds["earbuds"];
        int peripherals        = categoryIds["peripherals"];
        int gamingAccessories  = categoryIds["gaming-accessories"];
        int audioCategory      = categoryIds["audio"];
        int contentCreation    = categoryIds["content-creation"];
        int powerBanks         = categoryIds["power-banks"];

        List<Product> newProducts = new()
        {
            new Product
            {
                Id = new Guid("b6000000-1001-0000-0000-000000000001"),
                SKU = "SF-ZLT-B20", Name = "Zealot B20 Wireless Bluetooth Headphones",
                Slug = "zealot-b20-wireless-bluetooth-headphones",
                BrandId = zealot, CategoryId = earbuds,
                Description = "Over-ear wireless Bluetooth headphones with touch controls and a high-performance driver, designed for comfort.",
                ShortDescription = "Over-ear wireless Bluetooth headphones with touch controls and a high-performance driver, designed for comfort.",
                Price = 1m, StockQty = 0, IsActive = false, IsFeatured = false, AverageRating = 0, ReviewCount = 0, ImageUrlsJson = "[]",
            },
            new Product
            {
                Id = new Guid("b6000000-1002-0000-0000-000000000001"),
                SKU = "SF-JBL-JR310", Name = "JBL JR310 Kids On-Ear Wired Headphones",
                Slug = "jbl-jr310-kids-wired-headphones",
                BrandId = jbl, CategoryId = earbuds,
                Description = "Kids-size on-ear headphones with safe sound limited below 85dB, handy remote with mic, and fun stickers. Ages 3+.",
                ShortDescription = "Kids-size on-ear headphones with safe sound limited below 85dB, handy remote with mic, and fun stickers.",
                Price = 1m, StockQty = 0, IsActive = false, IsFeatured = false, AverageRating = 0, ReviewCount = 0, ImageUrlsJson = "[]",
            },
            new Product
            {
                Id = new Guid("b6000000-1003-0000-0000-000000000001"),
                SKU = "SF-SNY-CH520", Name = "Sony WH-CH520 Wireless On-Ear Headphones",
                Slug = "sony-wh-ch520-wireless-headphones",
                BrandId = sony, CategoryId = earbuds,
                Description = "Lightweight wireless on-ear headphones with up to 50-hour battery, 360 Reality Audio and multipoint connection.",
                ShortDescription = "Lightweight wireless on-ear headphones with up to 50-hour battery, 360 Reality Audio and multipoint connection.",
                Price = 1m, StockQty = 0, IsActive = false, IsFeatured = false, AverageRating = 0, ReviewCount = 0, ImageUrlsJson = "[]",
            },
            new Product
            {
                Id = new Guid("b6000000-1004-0000-0000-000000000001"),
                SKU = "SF-PS2-DS2", Name = "PlayStation 2 DualShock 2 Wireless Controller (2.4G)",
                Slug = "ps2-dualshock-2-wireless-controller",
                BrandId = sony, CategoryId = gamingAccessories,
                Description = "Wireless 2.4G analog controller for PlayStation 2 in DualShock 2 form factor.",
                ShortDescription = "Wireless 2.4G analog controller for PlayStation 2 in DualShock 2 form factor.",
                Price = 1m, StockQty = 0, IsActive = false, IsFeatured = false, AverageRating = 0, ReviewCount = 0, ImageUrlsJson = "[]",
            },
            new Product
            {
                Id = new Guid("b6000000-1005-0000-0000-000000000001"),
                SKU = "SF-GEN-G6", Name = "G6 Mobile Phone Radiator Cooling Fan",
                Slug = "g6-mobile-phone-radiator-cooler",
                BrandId = generic, CategoryId = gamingAccessories,
                Description = "Clip-on mobile phone live-streaming radiator/cooler with phone hanger, keeps the phone cool during gaming and streaming.",
                ShortDescription = "Clip-on mobile phone live-streaming radiator/cooler with phone hanger, keeps the phone cool during gaming and streaming.",
                Price = 1m, StockQty = 0, IsActive = false, IsFeatured = false, AverageRating = 0, ReviewCount = 0, ImageUrlsJson = "[]",
            },
            new Product
            {
                Id = new Guid("b6000000-1006-0000-0000-000000000001"),
                SKU = "SF-LGT-MK245", Name = "Logitech MK245 Nano Wireless Keyboard and Mouse Combo",
                Slug = "logitech-mk245-nano-wireless-keyboard-mouse-combo",
                BrandId = logitech, CategoryId = peripherals,
                Description = "Colorful compact wireless keyboard and mouse combo with nano USB receiver.",
                ShortDescription = "Colorful compact wireless keyboard and mouse combo with nano USB receiver.",
                Price = 1m, StockQty = 0, IsActive = false, IsFeatured = false, AverageRating = 0, ReviewCount = 0, ImageUrlsJson = "[]",
            },
            new Product
            {
                Id = new Guid("b6000000-1007-0000-0000-000000000001"),
                SKU = "SF-DJI-OM7", Name = "DJI Osmo Mobile 7 Smartphone Gimbal",
                Slug = "dji-osmo-mobile-7-smartphone-gimbal",
                BrandId = dji, CategoryId = contentCreation,
                Description = "Foldable 3-axis smartphone gimbal stabilizer for smooth video, with subject tracking.",
                ShortDescription = "Foldable 3-axis smartphone gimbal stabilizer for smooth video, with subject tracking.",
                Price = 1m, StockQty = 0, IsActive = false, IsFeatured = false, AverageRating = 0, ReviewCount = 0, ImageUrlsJson = "[]",
            },
            new Product
            {
                Id = new Guid("b6000000-1008-0000-0000-000000000001"),
                SKU = "SF-JBL-T510", Name = "JBL Tune 510BT Wireless On-Ear Headphones",
                Slug = "jbl-tune-510bt-wireless-headphones",
                BrandId = jbl, CategoryId = earbuds,
                Description = "Pure Bass wireless on-ear headphones with up to 40-hour battery. Available in white, pink and blue.",
                ShortDescription = "Pure Bass wireless on-ear headphones with up to 40-hour battery.",
                Price = 1m, StockQty = 0, IsActive = false, IsFeatured = false, AverageRating = 0, ReviewCount = 0, ImageUrlsJson = "[]",
            },
            new Product
            {
                Id = new Guid("b6000000-1009-0000-0000-000000000001"),
                SKU = "SF-LGT-MK270", Name = "Logitech MK270 Wireless Keyboard and Mouse Combo",
                Slug = "logitech-mk270-wireless-keyboard-mouse-combo",
                BrandId = logitech, CategoryId = peripherals,
                Description = "Reliable full-size 2.4GHz wireless keyboard and mouse combo with long battery life.",
                ShortDescription = "Reliable full-size 2.4GHz wireless keyboard and mouse combo with long battery life.",
                Price = 1m, StockQty = 0, IsActive = false, IsFeatured = false, AverageRating = 0, ReviewCount = 0, ImageUrlsJson = "[]",
            },
            new Product
            {
                Id = new Guid("b6000000-1010-0000-0000-000000000001"),
                SKU = "SF-ZLT-B19", Name = "Zealot B19 Bluetooth Headphones with LCD Display",
                Slug = "zealot-b19-bluetooth-headphones-lcd",
                BrandId = zealot, CategoryId = earbuds,
                Description = "Lightweight wireless headphones with cool LCD colorful screen, 40mm speaker driver, FM radio and TF card support.",
                ShortDescription = "Lightweight wireless headphones with cool LCD colorful screen, 40mm speaker driver, FM radio and TF card support.",
                Price = 1m, StockQty = 0, IsActive = false, IsFeatured = false, AverageRating = 0, ReviewCount = 0, ImageUrlsJson = "[]",
            },
            new Product
            {
                Id = new Guid("b6000000-1011-0000-0000-000000000001"),
                SKU = "SF-LGT-MK220", Name = "Logitech MK220 Wireless Keyboard and Mouse Combo",
                Slug = "logitech-mk220-wireless-keyboard-mouse-combo",
                BrandId = logitech, CategoryId = peripherals,
                Description = "Compact space-saving wireless keyboard and mouse combo with reliable 2.4GHz connection.",
                ShortDescription = "Compact space-saving wireless keyboard and mouse combo with reliable 2.4GHz connection.",
                Price = 1m, StockQty = 0, IsActive = false, IsFeatured = false, AverageRating = 0, ReviewCount = 0, ImageUrlsJson = "[]",
            },
            new Product
            {
                Id = new Guid("b6000000-1012-0000-0000-000000000001"),
                SKU = "SF-LGT-MX3S", Name = "Logitech MX Master 3S Performance Wireless Mouse",
                Slug = "logitech-mx-master-3s-wireless-mouse",
                BrandId = logitech, CategoryId = peripherals,
                Description = "Flagship performance wireless mouse from the Master Series with MagSpeed scrolling, quiet clicks and ultra-fast precision tracking.",
                ShortDescription = "Flagship performance wireless mouse from the Master Series with MagSpeed scrolling, quiet clicks and ultra-fast precision tracking.",
                Price = 1m, StockQty = 0, IsActive = false, IsFeatured = false, AverageRating = 0, ReviewCount = 0, ImageUrlsJson = "[]",
            },
            new Product
            {
                Id = new Guid("b6000000-1013-0000-0000-000000000001"),
                SKU = "SF-LGT-M720", Name = "Logitech M720 Triathlon Multi-Device Wireless Mouse",
                Slug = "logitech-m720-triathlon-multi-device-mouse",
                BrandId = logitech, CategoryId = peripherals,
                Description = "Multi-device wireless mouse (Bluetooth + USB receiver) with up to 24-month battery life, fast scrolling and large comfortable size.",
                ShortDescription = "Multi-device wireless mouse (Bluetooth + USB receiver) with up to 24-month battery life, fast scrolling and large comfortable size.",
                Price = 1m, StockQty = 0, IsActive = false, IsFeatured = false, AverageRating = 0, ReviewCount = 0, ImageUrlsJson = "[]",
            },
            new Product
            {
                Id = new Guid("b6000000-1014-0000-0000-000000000001"),
                SKU = "SF-ZLT-B570", Name = "Zealot B570 Bluetooth Headphones",
                Slug = "zealot-b570-bluetooth-headphones",
                BrandId = zealot, CategoryId = earbuds,
                Description = "Wireless over-ear headphones with Bluetooth, MP3 playback, TF card slot, FM radio and 3.5mm audio input.",
                ShortDescription = "Wireless over-ear headphones with Bluetooth, MP3 playback, TF card slot, FM radio and 3.5mm audio input.",
                Price = 1m, StockQty = 0, IsActive = false, IsFeatured = false, AverageRating = 0, ReviewCount = 0, ImageUrlsJson = "[]",
            },
            new Product
            {
                Id = new Guid("b6000000-1015-0000-0000-000000000001"),
                SKU = "SF-ZLT-B39", Name = "Zealot B39 Foldable Wireless Headphones",
                Slug = "zealot-b39-foldable-wireless-headphones",
                BrandId = zealot, CategoryId = earbuds,
                Description = "Foldable wireless stereo headphones with deep bass and long battery life.",
                ShortDescription = "Foldable wireless stereo headphones with deep bass and long battery life.",
                Price = 1m, StockQty = 0, IsActive = false, IsFeatured = false, AverageRating = 0, ReviewCount = 0, ImageUrlsJson = "[]",
            },
            new Product
            {
                Id = new Guid("b6000000-1016-0000-0000-000000000001"),
                SKU = "SF-MNO-PD200XS", Name = "Maono PD200XS USB/XLR Dynamic Podcast Microphone with Boom Arm Kit",
                Slug = "maono-pd200xs-usb-xlr-podcast-mic-boom-arm",
                BrandId = maono, CategoryId = contentCreation,
                Description = "USB/XLR dynamic broadcast microphone with RGB lighting and suspension boom arm stand — full podcast/streaming kit.",
                ShortDescription = "USB/XLR dynamic broadcast microphone with RGB lighting and suspension boom arm stand — full podcast/streaming kit.",
                Price = 1m, StockQty = 0, IsActive = false, IsFeatured = false, AverageRating = 0, ReviewCount = 0, ImageUrlsJson = "[]",
            },
            new Product
            {
                Id = new Guid("b6000000-1017-0000-0000-000000000001"),
                SKU = "SF-BYA-OMIC", Name = "Boya Omic Ultracompact 2.4GHz Dual-Channel Wireless Microphone System",
                Slug = "boya-omic-wireless-microphone-system",
                BrandId = boya, CategoryId = contentCreation,
                Description = "Ultracompact dual-channel 2.4GHz wireless microphone system with charging case. iF Design Award 2023 winner.",
                ShortDescription = "Ultracompact dual-channel 2.4GHz wireless microphone system with charging case.",
                Price = 1m, StockQty = 0, IsActive = false, IsFeatured = false, AverageRating = 0, ReviewCount = 0, ImageUrlsJson = "[]",
            },
            new Product
            {
                Id = new Guid("b6000000-1018-0000-0000-000000000001"),
                SKU = "SF-GEN-GT31", Name = "GT31 Mobile Phone Semiconductor Cooler",
                Slug = "gt31-phone-semiconductor-cooler",
                BrandId = generic, CategoryId = gamingAccessories,
                Description = "Frozen-type cell phone semiconductor cooler with RGB fan and phone hanger for gaming and live streaming.",
                ShortDescription = "Frozen-type cell phone semiconductor cooler with RGB fan and phone hanger for gaming and live streaming.",
                Price = 1m, StockQty = 0, IsActive = false, IsFeatured = false, AverageRating = 0, ReviewCount = 0, ImageUrlsJson = "[]",
            },
            new Product
            {
                Id = new Guid("b6000000-1019-0000-0000-000000000001"),
                SKU = "SF-GEN-AY49X", Name = "AY-49X Live Broadcast LED Ring Selfie Light Kit",
                Slug = "ay-49x-live-broadcast-selfie-light-kit",
                BrandId = generic, CategoryId = contentCreation,
                Description = "Phone video rig kit with dual LED fill lights, microphone, phone holder and tripod grip for live streaming and vlogging.",
                ShortDescription = "Phone video rig kit with dual LED fill lights, microphone, phone holder and tripod grip for live streaming and vlogging.",
                Price = 1m, StockQty = 0, IsActive = false, IsFeatured = false, AverageRating = 0, ReviewCount = 0, ImageUrlsJson = "[]",
            },
            new Product
            {
                Id = new Guid("b6000000-1020-0000-0000-000000000001"),
                SKU = "SF-LGT-M187P", Name = "Logitech M187P Wireless Mini Mouse",
                Slug = "logitech-m187p-wireless-mini-mouse",
                BrandId = logitech, CategoryId = peripherals,
                Description = "Plug-and-play compact wireless mouse with nano receiver and 12-month battery life.",
                ShortDescription = "Plug-and-play compact wireless mouse with nano receiver and 12-month battery life.",
                Price = 1m, StockQty = 0, IsActive = false, IsFeatured = false, AverageRating = 0, ReviewCount = 0, ImageUrlsJson = "[]",
            },
            new Product
            {
                Id = new Guid("b6000000-1021-0000-0000-000000000001"),
                SKU = "SF-LGT-K860", Name = "Logitech ERGO K860 for Business Ergonomic Wireless Keyboard",
                Slug = "logitech-ergo-k860-ergonomic-keyboard",
                BrandId = logitech, CategoryId = peripherals,
                Description = "Split curved ergonomic wireless keyboard with pillowed wrist rest and Logi Bolt secure connection, designed for natural typing.",
                ShortDescription = "Split curved ergonomic wireless keyboard with pillowed wrist rest and Logi Bolt secure connection, designed for natural typing.",
                Price = 1m, StockQty = 0, IsActive = false, IsFeatured = false, AverageRating = 0, ReviewCount = 0, ImageUrlsJson = "[]",
            },
            new Product
            {
                Id = new Guid("b6000000-1022-0000-0000-000000000001"),
                SKU = "SF-LGT-M190", Name = "Logitech M190 Full-Size Wireless Mouse",
                Slug = "logitech-m190-wireless-mouse",
                BrandId = logitech, CategoryId = peripherals,
                Description = "Full-size plug-and-play wireless mouse with contoured comfortable design.",
                ShortDescription = "Full-size plug-and-play wireless mouse with contoured comfortable design.",
                Price = 1m, StockQty = 0, IsActive = false, IsFeatured = false, AverageRating = 0, ReviewCount = 0, ImageUrlsJson = "[]",
            },
            new Product
            {
                Id = new Guid("b6000000-1023-0000-0000-000000000001"),
                SKU = "SF-SNY-XM5", Name = "Sony WH-1000XM5 Wireless Noise Cancelling Headphones",
                Slug = "sony-wh-1000xm5-noise-cancelling-headphones",
                BrandId = sony, CategoryId = earbuds,
                Description = "Sony's flagship wireless noise cancelling headphones with best-in-class ANC, up to 30-hour battery and crystal-clear call quality.",
                ShortDescription = "Sony's flagship wireless noise cancelling headphones with best-in-class ANC, up to 30-hour battery and crystal-clear call quality.",
                Price = 1m, StockQty = 0, IsActive = false, IsFeatured = false, AverageRating = 0, ReviewCount = 0, ImageUrlsJson = "[]",
            },
            new Product
            {
                Id = new Guid("b6000000-1024-0000-0000-000000000001"),
                SKU = "SF-GDX-SK400II", Name = "Godox SK400II 2-Head Studio Flash Kit with Bowens Mount Softboxes",
                Slug = "godox-sk400ii-2-head-studio-flash-kit",
                BrandId = godox, CategoryId = contentCreation,
                Description = "Two-head 400Ws studio strobe lighting kit with Bowens mount softboxes, stands and padded carry bag.",
                ShortDescription = "Two-head 400Ws studio strobe lighting kit with Bowens mount softboxes, stands and padded carry bag.",
                Price = 1m, StockQty = 0, IsActive = false, IsFeatured = false, AverageRating = 0, ReviewCount = 0, ImageUrlsJson = "[]",
            },
            new Product
            {
                Id = new Guid("b6000000-1025-0000-0000-000000000001"),
                SKU = "SF-LGT-M90", Name = "Logitech M90 Wired USB Mouse",
                Slug = "logitech-m90-wired-mouse",
                BrandId = logitech, CategoryId = peripherals,
                Description = "Full-size corded USB mouse with comfortable ambidextrous design.",
                ShortDescription = "Full-size corded USB mouse with comfortable ambidextrous design.",
                Price = 1m, StockQty = 0, IsActive = false, IsFeatured = false, AverageRating = 0, ReviewCount = 0, ImageUrlsJson = "[]",
            },
            new Product
            {
                Id = new Guid("b6000000-1026-0000-0000-000000000001"),
                SKU = "SF-PS5-DSNS", Name = "Sony PS5 DualSense Wireless Controller",
                Slug = "ps5-dualsense-controller",
                BrandId = sony, CategoryId = gamingAccessories,
                Description = "Official DualSense wireless controller for PS5 with haptic feedback and adaptive triggers. Available in Starlight Blue and Cosmic Red.",
                ShortDescription = "Official DualSense wireless controller for PS5 with haptic feedback and adaptive triggers.",
                Price = 1m, StockQty = 0, IsActive = false, IsFeatured = false, AverageRating = 0, ReviewCount = 0, ImageUrlsJson = "[]",
            },
            new Product
            {
                Id = new Guid("b6000000-1027-0000-0000-000000000001"),
                SKU = "SF-GEN-NX10", Name = "NX10 Type-C Wireless Lavalier Microphone",
                Slug = "nx10-type-c-wireless-microphone",
                BrandId = generic, CategoryId = contentCreation,
                Description = "2.4G wireless lavalier microphone set with USB-C connector and charging case with LED display — for phone video and streaming.",
                ShortDescription = "2.4G wireless lavalier microphone set with USB-C connector and charging case with LED display — for phone video and streaming.",
                Price = 1m, StockQty = 0, IsActive = false, IsFeatured = false, AverageRating = 0, ReviewCount = 0, ImageUrlsJson = "[]",
            },
            new Product
            {
                Id = new Guid("b6000000-1028-0000-0000-000000000001"),
                SKU = "SF-ZLT-S78", Name = "Zealot S78 Portable Bluetooth Speaker (100W)",
                Slug = "zealot-s78-bluetooth-speaker-100w",
                BrandId = zealot, CategoryId = audioCategory,
                Description = "High-power 100W portable Bluetooth speaker with deep bass, RGB lighting and carry handle.",
                ShortDescription = "High-power 100W portable Bluetooth speaker with deep bass, RGB lighting and carry handle.",
                Price = 1m, StockQty = 0, IsActive = false, IsFeatured = false, AverageRating = 0, ReviewCount = 0, ImageUrlsJson = "[]",
            },
            new Product
            {
                Id = new Guid("b6000000-1029-0000-0000-000000000001"),
                SKU = "SF-SNY-CH720N", Name = "Sony WH-CH720N Wireless Noise Cancelling Headphones",
                Slug = "sony-wh-ch720n-noise-cancelling-headphones",
                BrandId = sony, CategoryId = earbuds,
                Description = "Lightweight wireless noise cancelling headphones with up to 35-hour battery and 360 Reality Audio. Available in black and blue.",
                ShortDescription = "Lightweight wireless noise cancelling headphones with up to 35-hour battery and 360 Reality Audio.",
                Price = 1m, StockQty = 0, IsActive = false, IsFeatured = false, AverageRating = 0, ReviewCount = 0, ImageUrlsJson = "[]",
            },
            new Product
            {
                Id = new Guid("b6000000-1030-0000-0000-000000000001"),
                SKU = "SF-MNO-DGM20", Name = "Maono DGM20 Gaming USB Microphone with Noise Cancelling & RGB",
                Slug = "maono-dgm20-gaming-usb-microphone-rgb",
                BrandId = maono, CategoryId = contentCreation,
                Description = "Gaming USB condenser microphone with noise cancelling, RGB lighting, pop filter and desk stand.",
                ShortDescription = "Gaming USB condenser microphone with noise cancelling, RGB lighting, pop filter and desk stand.",
                Price = 1m, StockQty = 0, IsActive = false, IsFeatured = false, AverageRating = 0, ReviewCount = 0, ImageUrlsJson = "[]",
            },
            new Product
            {
                Id = new Guid("b6000000-1031-0000-0000-000000000001"),
                SKU = "SF-GEN-AY49", Name = "AY-49 Video Making Kit with Vlogging Tripod",
                Slug = "ay-49-video-making-vlogging-kit",
                BrandId = generic, CategoryId = contentCreation,
                Description = "Video-making vlogging kit with tripod, LED light, shotgun microphone and phone grip for live broadcast and content creation.",
                ShortDescription = "Video-making vlogging kit with tripod, LED light, shotgun microphone and phone grip for live broadcast and content creation.",
                Price = 1m, StockQty = 0, IsActive = false, IsFeatured = false, AverageRating = 0, ReviewCount = 0, ImageUrlsJson = "[]",
            },
            new Product
            {
                Id = new Guid("b6000000-1032-0000-0000-000000000001"),
                SKU = "SF-PS3-DS3", Name = "Sony PS3 DualShock 3 Wireless Controller",
                Slug = "ps3-dualshock-3-wireless-controller",
                BrandId = sony, CategoryId = gamingAccessories,
                Description = "DualShock 3 wireless controller for PlayStation 3. Available in blue and camouflage.",
                ShortDescription = "DualShock 3 wireless controller for PlayStation 3.",
                Price = 1m, StockQty = 0, IsActive = false, IsFeatured = false, AverageRating = 0, ReviewCount = 0, ImageUrlsJson = "[]",
            },
            new Product
            {
                Id = new Guid("b6000000-1033-0000-0000-000000000001"),
                SKU = "SF-GEN-LED600", Name = "Pro LED 600 Rechargeable Photo & Video LED Light Kit",
                Slug = "pro-led-600-photo-video-light-kit",
                BrandId = generic, CategoryId = contentCreation,
                Description = "Professional Varicolor LED panel light kit with barn doors and adjustable brightness and color temperature (3200–5600K).",
                ShortDescription = "Professional Varicolor LED panel light kit with barn doors and adjustable brightness and color temperature (3200–5600K).",
                Price = 1m, StockQty = 0, IsActive = false, IsFeatured = false, AverageRating = 0, ReviewCount = 0, ImageUrlsJson = "[]",
            },
            new Product
            {
                Id = new Guid("b6000000-1034-0000-0000-000000000001"),
                SKU = "SF-PS4-DS4", Name = "Sony PS4 DualShock 4 Wireless Controller (Glacier White)",
                Slug = "ps4-dualshock-4-wireless-controller",
                BrandId = sony, CategoryId = gamingAccessories,
                Description = "Official DualShock 4 wireless controller for PS4 in Glacier White.",
                ShortDescription = "Official DualShock 4 wireless controller for PS4 in Glacier White.",
                Price = 1m, StockQty = 0, IsActive = false, IsFeatured = false, AverageRating = 0, ReviewCount = 0, ImageUrlsJson = "[]",
            },
        };

        HashSet<string> existingSkus = (await context.Products
            .Select(p => p.SKU)
            .ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        HashSet<Guid> existingProductIds = (await context.Products
            .Select(p => p.Id)
            .ToListAsync()).ToHashSet();

        List<Product> missingProducts = newProducts
            .Where(p => !existingSkus.Contains(p.SKU) && !existingProductIds.Contains(p.Id))
            .ToList();

        if (missingProducts.Count > 0)
        {
            await context.Products.AddRangeAsync(missingProducts);
            await context.SaveChangesAsync();
        }

        // ── Rename 4 live, promo-referenced products in place. Id/Price/StockQty/
        // IsActive/images are untouched — only Name/Slug/Description/ShortDescription/SKU. ──

        Product? sonyXm4 = await context.Products
            .FirstOrDefaultAsync(p => p.Id == new Guid("567e6545-e007-4fad-9f59-9b21071575a4"));
        if (sonyXm4 is not null)
        {
            sonyXm4.SKU = "SF-SNY-XM4";
            sonyXm4.Name = "Sony WH-1000XM4 Wireless Noise Cancelling Headphones";
            sonyXm4.Slug = GenerateCatalog2026Slug(sonyXm4.Name);
            sonyXm4.Description = "Industry-leading noise cancelling over-ear headphones with up to 30-hour battery life, 360 Reality Audio, Alexa/Google built-in and Bluetooth multipoint.";
            sonyXm4.ShortDescription = "Industry-leading noise cancelling over-ear headphones with up to 30-hour battery life, 360 Reality Audio, Alexa/Google built-in and Bluetooth multipoint.";
        }

        Product? jblTune710 = await context.Products
            .FirstOrDefaultAsync(p => p.Id == new Guid("684bd797-c6c8-4fc7-8bad-98bf596a0101"));
        if (jblTune710 is not null)
        {
            jblTune710.SKU = "SF-JBL-T710";
            jblTune710.Name = "JBL Tune 710BT Wireless Over-Ear Headphones";
            jblTune710.Slug = GenerateCatalog2026Slug(jblTune710.Name);
            jblTune710.Description = "Pure Bass wireless over-ear headphones with up to 50-hour battery life and multipoint Bluetooth.";
            jblTune710.ShortDescription = "Pure Bass wireless over-ear headphones with up to 50-hour battery life and multipoint Bluetooth.";
        }

        Product? jblTune520 = await context.Products
            .FirstOrDefaultAsync(p => p.Name == "Jbl Tune 520 Bluetooth Headset");
        if (jblTune520 is not null)
        {
            jblTune520.SKU = "SF-JBL-T520";
            jblTune520.Name = "JBL Tune 520BT Wireless On-Ear Headphones";
            jblTune520.Slug = GenerateCatalog2026Slug(jblTune520.Name);
            jblTune520.Description = "Pure Bass wireless on-ear headphones with up to 57-hour battery. Available in white and blue.";
            jblTune520.ShortDescription = "Pure Bass wireless on-ear headphones with up to 57-hour battery.";
        }

        Product? newAgePowerBank = await context.Products
            .FirstOrDefaultAsync(p => p.Name == "New Age J153 22500mah with cables");
        if (newAgePowerBank is not null)
        {
            newAgePowerBank.SKU = "SF-NWA-J153";
            newAgePowerBank.Name = "New Age J153 22500mAh Power Bank with Built-in Cables";
            newAgePowerBank.Slug = GenerateCatalog2026Slug(newAgePowerBank.Name);
            newAgePowerBank.Description = "22500mAh power bank with 3A quick charge, built-in charging cables and LED display. 12-month warranty.";
            newAgePowerBank.ShortDescription = "22500mAh power bank with 3A quick charge, built-in charging cables and LED display.";
            newAgePowerBank.BrandId = newAge;
            newAgePowerBank.CategoryId = powerBanks;
        }

        await context.SaveChangesAsync();

        // ── Deactivate every other product not in the new 38-SKU catalog. Never sets
        // DeletedAt / hard-deletes — OrderItem and CartItem FKs are Restrict. ──

        HashSet<string> keepActiveSkus = newProducts.Select(p => p.SKU)
            .Concat(new[] { "SF-SNY-XM4", "SF-JBL-T710", "SF-JBL-T520", "SF-NWA-J153" })
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<Product> currentlyActive = await context.Products
            .Where(p => p.IsActive)
            .ToListAsync();

        foreach (Product product in currentlyActive)
        {
            if (!keepActiveSkus.Contains(product.SKU))
            {
                product.IsActive = false;
            }
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Slug generator for the Catalog 2026 in-place renames. Unlike
    /// <c>CreateProductCommand</c>'s generator (which drops hyphens entirely), this
    /// converts them to dashes too, so "Over-Ear" becomes "over-ear" rather than "overear".
    /// </summary>
    private static string GenerateCatalog2026Slug(string name)
    {
        string lower = name.Trim().ToLowerInvariant();
        System.Text.StringBuilder sb = new();
        foreach (char c in lower)
        {
            if (char.IsLetterOrDigit(c)) sb.Append(c);
            else if (c == ' ' || c == '-') sb.Append('-');
        }
        string slug = sb.ToString();
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        return slug.Trim('-');
    }

    // ── Flash deal ────────────────────────────────────────────────────────────

    private static async Task SeedFlashDealAsync(ShopFresherzDbContext context)
    {
        if (await context.FlashDeals.AnyAsync()) return;

        Guid ps5Id = new("33333333-3001-0000-0000-000000000001");
        bool ps5Exists = await context.Products.AnyAsync(p => p.Id == ps5Id);
        if (!ps5Exists) return;

        FlashDeal deal = new()
        {
            ProductId    = ps5Id,
            SalePrice    = 590000,
            OriginalPrice = 680000,
            StartsAt     = DateTime.UtcNow,
            EndsAt       = DateTime.UtcNow.AddDays(3),
            MaxQuantity  = 3,
            IsActive     = true,
        };

        await context.FlashDeals.AddAsync(deal);
        await context.SaveChangesAsync();
    }

    // ── Banners ───────────────────────────────────────────────────────────────

    private static async Task SeedBannersAsync(ShopFresherzDbContext context)
    {
        List<HomepageBanner> banners = new()
        {
            new()
            {
                Tag      = "New Arrival",
                Title    = "iPhone 16 Pro Max - Just Arrived",
                SubTitle = "A18 Pro - Titanium - 48MP Camera",
                CtaText  = "Shop iPhones",
                LinkUrl  = "/category/phones",
                ImageUrl = "https://res.cloudinary.com/dj0hxpss4/image/upload/shopfresherz/banners/iphone16-hero.jpg",
                SortOrder = 1, IsActive = true,
            },
            new()
            {
                Tag      = "Limited Stock",
                Title    = "PS5 Slim - Limited Stock",
                SubTitle = "4K 120fps - 1TB SSD - DualSense Included",
                CtaText  = "Get Yours Now",
                LinkUrl  = "/category/gaming",
                ImageUrl = "https://res.cloudinary.com/dj0hxpss4/image/upload/shopfresherz/banners/ps5-slim-hero.jpg",
                SortOrder = 2, IsActive = true,
            },
            new()
            {
                Tag      = "Laptop Deal",
                Title    = "MacBook Air M4",
                SubTitle = "Supercharged performance - 18-hour battery",
                CtaText  = "Shop MacBooks",
                LinkUrl  = "/category/laptops",
                ImageUrl = "https://res.cloudinary.com/dj0hxpss4/image/upload/shopfresherz/banners/macbook-m4-hero.jpg",
                SortOrder = 3, IsActive = true,
            },
        };

        List<HomepageBanner> existingBanners = await context.Set<HomepageBanner>().ToListAsync();
        foreach (HomepageBanner existing in existingBanners)
        {
            HomepageBanner? source = banners.FirstOrDefault(b =>
                b.Title == existing.Title || b.SortOrder == existing.SortOrder);
            if (source is not null && string.IsNullOrWhiteSpace(existing.Tag))
            {
                existing.Tag = source.Tag;
            }
        }

        HashSet<string> existingTitles = existingBanners
            .Select(b => b.Title)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<int> existingSortOrders = existingBanners
            .Select(b => b.SortOrder)
            .ToHashSet();

        List<HomepageBanner> missingBanners = banners
            .Where(b => !existingTitles.Contains(b.Title) && !existingSortOrders.Contains(b.SortOrder))
            .ToList();

        if (missingBanners.Count > 0)
        {
            await context.Set<HomepageBanner>().AddRangeAsync(missingBanners);
        }

        await context.SaveChangesAsync();
    }
    // ── Coupons ───────────────────────────────────────────────────────────────

    private static async Task SeedCouponsAsync(ShopFresherzDbContext context)
    {
        if (await context.Coupons.AnyAsync()) return;

        List<Coupon> coupons = new()
        {
            new()
            {
                Code = "WELCOME10",
                Type = CouponType.Percentage, Value = 10,
                MinimumOrderAmount = 50000, MaxUses = 1000, MaxUsesPerUser = 1,
                ExpiresAt = DateTime.UtcNow.AddMonths(6), IsActive = true,
            },
            new()
            {
                Code = "FRESHERZ5000",
                Type = CouponType.Fixed, Value = 5000,
                MinimumOrderAmount = 100000, MaxUses = 500, MaxUsesPerUser = 1,
                ExpiresAt = DateTime.UtcNow.AddMonths(3), IsActive = true,
            },
            new()
            {
                Code = "GADGET15",
                Type = CouponType.Percentage, Value = 15,
                MinimumOrderAmount = 200000, MaxUses = 200, MaxUsesPerUser = 1,
                ExpiresAt = DateTime.UtcNow.AddMonths(2), IsActive = true,
            },
        };

        await context.Coupons.AddRangeAsync(coupons);
        await context.SaveChangesAsync();
    }

    // ── Reviews ───────────────────────────────────────────────────────────────

    private static async Task SeedReviewsAsync(ShopFresherzDbContext context)
    {
        Guid? adminId = await context.Users
            .Where(u => u.Role == UserRole.SuperAdmin)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync();

        Guid iphoneId = new("11111111-1001-0000-0000-000000000001");
        Guid iphoneProId = new("11111111-1002-0000-0000-000000000001");
        Guid samsungUltraId = new("11111111-1006-0000-0000-000000000001");
        HashSet<Guid> seededReviewProductIds = new() { iphoneId, iphoneProId, samsungUltraId };
        HashSet<Guid> existingProductIds = (await context.Products
            .Where(p => seededReviewProductIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync()).ToHashSet();

        if (!adminId.HasValue || existingProductIds.Count == 0) return;

        List<Review> reviews = new()
        {
            new()
            {
                UserId = adminId.Value, ProductId = iphoneId,
                Rating = 5, Title = "Absolutely incredible phone",
                Body = "The A18 Pro chip is insanely fast. Camera is unmatched — Night mode portraits are stunning. Battery easily lasts a full day of heavy use. Best iPhone I have ever owned.",
                IsApproved = true, IsVerifiedPurchase = true,
            },
            new()
            {
                UserId = adminId.Value, ProductId = iphoneProId,
                Rating = 5, Title = "Worth every kobo",
                Body = "Titanium build feels incredibly premium. The action button is so useful. Camera Control is a game changer. Display is stunning even outdoors. ShopFresherz delivery was super fast.",
                IsApproved = true, IsVerifiedPurchase = true,
            },
            new()
            {
                UserId = adminId.Value, ProductId = samsungUltraId,
                Rating = 4, Title = "Top tier but pricey",
                Body = "Performance is flawless and the camera system is top notch. Only the price is a stretch but you genuinely get what you pay for at this level. Recommend to anyone who can afford it.",
                IsApproved = true, IsVerifiedPurchase = false,
            },
        };

        HashSet<Guid> existingReviewProductIds = (await context.Reviews
            .Where(r => r.UserId == adminId.Value && seededReviewProductIds.Contains(r.ProductId))
            .Select(r => r.ProductId)
            .ToListAsync()).ToHashSet();

        List<Review> missingReviews = reviews
            .Where(r => existingProductIds.Contains(r.ProductId) && !existingReviewProductIds.Contains(r.ProductId))
            .ToList();

        if (missingReviews.Count == 0) return;

        await context.Reviews.AddRangeAsync(missingReviews);
        await context.SaveChangesAsync();
    }

    // ── Promotional Sections ──────────────────────────────────────────────────

    private static async Task SeedPromotionalSectionsAsync(ShopFresherzDbContext context)
    {
        // Clear any stale tracked entities left by earlier seed methods that may have failed.
        context.ChangeTracker.Clear();

        HashSet<string> existingSlugIds = (await context.PromotionalSections
            .Select(s => s.SlugId)
            .ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var sections = new List<PromotionalSection>
        {
            // ── Hero Banners ─────────────────────────────────────────────────
            new()
            {
                SectionKey  = "hero",
                ContentType = "hero-banner",
                SlugId      = "hero-iphone-16-pro",
                Tag         = "New Arrival",
                Badge       = "Hot",
                Title       = "iPhone 16 Pro Max — Power Redefined",
                PriceText   = "From $1,099",
                CtaText     = "Shop Now",
                ImageUrl    = "https://images.unsplash.com/photo-1695048133142-1a20484429be?w=1200&auto=format&fit=crop",
                SortOrder   = 1,
                IsActive    = true,
            },
            new()
            {
                SectionKey  = "hero",
                ContentType = "hero-banner",
                SlugId      = "hero-samsung-s25",
                Tag         = "Best Seller",
                Badge       = "Sale",
                Title       = "Samsung Galaxy S25 Ultra — See Everything",
                PriceText   = "From $899",
                CtaText     = "Explore",
                ImageUrl    = "https://images.unsplash.com/photo-1610945265064-0e34e5519bbf?w=1200&auto=format&fit=crop",
                SortOrder   = 2,
                IsActive    = true,
            },
            new()
            {
                SectionKey  = "hero",
                ContentType = "hero-banner",
                SlugId      = "hero-macbook-pro-m4",
                Tag         = "Limited Offer",
                Badge       = "Deal",
                Title       = "MacBook Pro M4 — Built for Speed",
                PriceText   = "From $1,599",
                CtaText     = "Get Yours",
                ImageUrl    = "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=1200&auto=format&fit=crop",
                SortOrder   = 3,
                IsActive    = true,
            },

            // ── Best Deal Cards ───────────────────────────────────────────────
            new()
            {
                SectionKey        = "best-deal",
                ContentType       = "best-deal-card",
                SlugId            = "deal-airpods-pro",
                Title             = "AirPods Pro (2nd Gen)",
                Badge             = "Best Value",
                Rating            = 4.8m,
                OriginalPriceText = "$249",
                SalePriceText     = "$189",
                Description       = "Active noise cancellation, Adaptive Audio, and up to 30 hours total listening time with MagSafe charging case.",
                ImageUrl          = "https://images.unsplash.com/photo-1588423771073-b8903fead85c?w=600&auto=format&fit=crop",
                SortOrder         = 1,
                IsActive          = true,
            },
            new()
            {
                SectionKey        = "best-deal",
                ContentType       = "best-deal-card",
                SlugId            = "deal-samsung-watch7",
                Title             = "Samsung Galaxy Watch 7",
                Badge             = "Top Rated",
                Rating            = 4.6m,
                OriginalPriceText = "$299",
                SalePriceText     = "$229",
                Description       = "Advanced health tracking with BioActive sensor, sleep coaching, and 40 hours battery life in power-saving mode.",
                ImageUrl          = "https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=600&auto=format&fit=crop",
                SortOrder         = 2,
                IsActive          = true,
            },
            new()
            {
                SectionKey        = "best-deal",
                ContentType       = "best-deal-card",
                SlugId            = "deal-sony-wh1000xm5",
                Title             = "Sony WH-1000XM5 Headphones",
                Badge             = "Editor's Pick",
                Rating            = 4.9m,
                OriginalPriceText = "$399",
                SalePriceText     = "$299",
                Description       = "Industry-leading noise cancellation with 30 hours battery, multipoint connection, and crystal-clear hands-free calling.",
                ImageUrl          = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=600&auto=format&fit=crop",
                SortOrder         = 3,
                IsActive          = true,
            },

            // ── Promo Banner ──────────────────────────────────────────────────
            new()
            {
                SectionKey  = "promo-banner",
                ContentType = "promo-banner",
                SlugId      = "promo-flash-sale-2024",
                Title       = "Up to 40% Off Top Brands",
                Subtitle    = "Limited time deals on iPhones, Samsung, Sony and more. Don't miss out!",
                CtaText     = "View All Deals",
                ImageUrl    = "https://images.unsplash.com/photo-1607082348824-0a96f2a4b9da?w=1400&auto=format&fit=crop",
                ImageAlt    = "Flash sale promotional banner featuring smartphones and electronics",
                Badge       = "Flash Sale",
                SortOrder   = 1,
                IsActive    = true,
            },

            // ── Accessories Promo Cards ───────────────────────────────────────
            new()
            {
                SectionKey  = "accessories-promo",
                ContentType = "feature-card",
                SlugId      = "acc-promo-feature-card",
                Title       = "Computer Accessories",
                Subtitle    = "Upgrade your setup with premium keyboards, mice, hubs, and more.",
                ButtonText  = "Shop Accessories",
                ImageUrl    = "https://images.unsplash.com/photo-1587829741301-dc798b83add3?w=800&auto=format&fit=crop",
                PriceLabel  = "Starting at",
                PriceValue  = "$19.99",
                SortOrder   = 1,
                IsActive    = true,
            },
            new()
            {
                SectionKey  = "accessories-promo",
                ContentType = "discount-card",
                SlugId      = "acc-promo-discount-card",
                Tag         = "Special Offer",
                Headline    = "25% Off All Cables & Chargers",
                Description = "Stock up on fast-charging cables, USB-C hubs, and wireless chargers at unbeatable prices.",
                ButtonText  = "Grab the Deal",
                SortOrder   = 2,
                IsActive    = true,
            },

            // ── Laptop Promo ──────────────────────────────────────────────────
            new()
            {
                SectionKey  = "laptop-promo",
                ContentType = "laptop-promo",
                SlugId      = "laptop-promo-dell-xps",
                Title       = "Dell XPS 15 — Creator's Choice",
                Subtitle    = "OLED display, Intel Core i9, 32 GB RAM. Built for those who demand more.",
                CtaText     = "Shop Laptops",
                ImageUrl    = "https://images.unsplash.com/photo-1593642632559-0c6d3fc62b89?w=900&auto=format&fit=crop",
                ImageAlt    = "Dell XPS 15 laptop open on desk with OLED display",
                PriceBadge  = "From",
                PriceValue  = "$1,399",
                SortOrder   = 1,
                IsActive    = true,
            },
        };

        List<PromotionalSection> missingSections = sections
            .Where(s => !existingSlugIds.Contains(s.SlugId))
            .ToList();

        if (missingSections.Count == 0) return;

        await context.PromotionalSections.AddRangeAsync(missingSections);
        await context.SaveChangesAsync();
    }
}
