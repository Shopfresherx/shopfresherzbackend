using System.Text.Json.Serialization;
using AspNetCoreRateLimit;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using ShopFresherz.API;
using ShopFresherz.Application.Common.Extensions;
using ShopFresherz.Domain.Interfaces.Services;
using ShopFresherz.Infrastructure.Configuration;
using ShopFresherz.Infrastructure.Extensions;
using ShopFresherz.Infrastructure.Jobs;
using ShopFresherz.Infrastructure.Persistence;
using ShopFresherz.Infrastructure.Services;
using System.Security.Cryptography;

// ── Serilog bootstrap logger (captures startup errors before DI is ready) ──────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    // ── Serilog ───────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, services, config) =>
        config.ReadFrom.Configuration(ctx.Configuration)
              .ReadFrom.Services(services));

    // ── Application (MediatR, FluentValidation, AutoMapper) ──────────────────
    builder.Services.AddApplication();

    // ── Infrastructure (EF Core, repositories, services) ─────────────────────
    builder.Services.AddInfrastructure(builder.Configuration);
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
    }
    else
    {
        string? cloudinaryCloudName = builder.Configuration["Cloudinary:CloudName"]
            ?? Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME");
        string? azureBlobConnectionString = builder.Configuration["Azure:BlobStorage:ConnectionString"]
            ?? Environment.GetEnvironmentVariable("AZURE_BLOB_STORAGE_CONNECTION_STRING")
            ?? Environment.GetEnvironmentVariable("Azure__BlobStorage__ConnectionString");

        if (!string.IsNullOrWhiteSpace(cloudinaryCloudName))
        {
            builder.Services.AddScoped<IFileStorageService, CloudinaryFileStorageService>();
        }
        else if (!string.IsNullOrWhiteSpace(azureBlobConnectionString))
        {
            builder.Services.AddScoped<IFileStorageService, AzureBlobFileStorageService>();
        }
        else
        {
            Log.Warning(
                "No Cloudinary or Azure Blob storage configuration found. Falling back to local wwwroot/uploads storage.");
            builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
        }
    }

    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            new SqlServerStorageOptions
            {
                PrepareSchemaIfNecessary = true,
            }));

    builder.Services.AddHangfireServer(options =>
    {
        options.WorkerCount = 2;
        options.Queues = ["default", "critical"];
    });

    builder.Services.AddScoped<StockReservationExpiryJob>();
    builder.Services.AddScoped<FlashDealExpiryJob>();
    builder.Services.AddScoped<BackInStockNotificationJob>();

    builder.Services.AddMemoryCache();
    builder.Services.Configure<IpRateLimitOptions>(options =>
    {
        options.EnableEndpointRateLimiting = true;
        options.StackBlockedRequests = false;
        options.HttpStatusCode = StatusCodes.Status429TooManyRequests;
        options.RealIpHeader = "X-Real-IP";
        options.GeneralRules =
        [
            new RateLimitRule { Endpoint = "*", Period = "1m", Limit = 60 },
            new RateLimitRule { Endpoint = "post:/api/v1/auth/login", Period = "5m", Limit = 5 },
            new RateLimitRule { Endpoint = "post:/api/v1/auth/register", Period = "1h", Limit = 10 },
            new RateLimitRule { Endpoint = "post:/api/v1/auth/forgot-password", Period = "15m", Limit = 3 },
            new RateLimitRule { Endpoint = "post:/api/v1/auth/resend-otp", Period = "1h", Limit = 3 },
            new RateLimitRule { Endpoint = "get:/api/v1/search/instant", Period = "1m", Limit = 120 },
        ];
    });
    builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
    builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
    builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
    builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
    builder.Services.AddInMemoryRateLimiting();

    // ── JWT RS256 authentication ───────────────────────────────────────────────
    string? privateKeyPem = builder.Configuration["Jwt:PrivateKeyPem"];
    string? publicKeyPem = builder.Configuration["Jwt:PublicKeyPem"];

    if (string.IsNullOrWhiteSpace(privateKeyPem) && string.IsNullOrWhiteSpace(publicKeyPem))
    {
        throw new InvalidOperationException("Jwt:PrivateKeyPem or Jwt:PublicKeyPem is not configured.");
    }

    RSA publicRsa = RSA.Create();
    if (!string.IsNullOrWhiteSpace(privateKeyPem))
    {
        privateKeyPem = privateKeyPem.Replace("\\n", "\n").Trim();
        publicRsa.ImportFromPem(privateKeyPem);
    }
    else
    {
        publicKeyPem = publicKeyPem!.Replace("\\n", "\n").Trim();
        publicRsa.ImportFromPem(publicKeyPem);
    }

    RsaSecurityKey rsaPublicKey = new(publicRsa);

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidIssuer              = builder.Configuration["Jwt:Issuer"],
                ValidateAudience         = true,
                ValidAudience            = builder.Configuration["Jwt:Audience"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey         = rsaPublicKey,
                ValidateLifetime         = true,
                ClockSkew                = TimeSpan.FromSeconds(30),
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        // NOTE: TokenService issues role claim using ClaimTypes.Role (standard MS role claim type).
        // TokenService uses ClaimTypes.Role, so authorization policies must check the same claim type.
        const string roleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

        options.AddPolicy("RequireCustomer", policy =>
            policy.RequireAuthenticatedUser()
                  .RequireClaim(roleClaimType, "Customer", "Admin", "SuperAdmin"));

        options.AddPolicy("RequireAdmin", policy =>
            policy.RequireAuthenticatedUser()
                  .RequireClaim(roleClaimType, "Admin", "SuperAdmin"));

        options.AddPolicy("RequireSuperAdmin", policy =>
            policy.RequireAuthenticatedUser()
                  .RequireClaim(roleClaimType, "SuperAdmin"));
    });

    // ── Controllers ────────────────────────────────────────────────────────────
    builder.Services
        .AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

    // ── CORS ───────────────────────────────────────────────────────────────────
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("ShopFresherzCors", policy =>
        {
            string[] origins = builder.Configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? Array.Empty<string>();

            string[] hardcoded =
            [
                "http://localhost:3000",
                "https://localhost:3000",
                "http://localhost:3001",
                "https://localhost:3001",
                "http://localhost:8080",
                "https://localhost:8080",
                "http://fresherz-001-site1.ftempurl.com",
                "https://fresherz-001-site1.ftempurl.com",
                "http://shopfresherz.com",
                "https://shopfresherz.com",
                "http://www.shopfresherz.com",
                "https://www.shopfresherz.com"
            ];

            string[] allOrigins = origins
                .Union(hardcoded, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            policy.WithOrigins(allOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });


    // ── Swagger / OpenAPI ──────────────────────────────────────────────────────
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title       = "ShopFresherz API",
            Version     = "v1",
            Description = "Nigerian tech/gadgets e-commerce platform API",
        });

        // JWT Bearer security definition
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name         = "Authorization",
            Type         = SecuritySchemeType.Http,
            Scheme       = "bearer",
            BearerFormat = "JWT",
            In           = ParameterLocation.Header,
            Description  = "Enter your JWT access token.",
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id   = "Bearer",
                    },
                },
                []
            },
        });

        // Include XML comments for Swagger documentation
        string xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    });

    // ── Build and configure middleware pipeline ────────────────────────────────
    WebApplication app = builder.Build();

    // Apply pending EF Core migrations automatically on startup (must run before seeding).
    using (IServiceScope scope = app.Services.CreateScope())
    {
        ShopFresherzDbContext db = scope.ServiceProvider.GetRequiredService<ShopFresherzDbContext>();
        await db.Database.MigrateAsync();
    }

    // Seed baseline data (categories + admin + products etc.) for new/empty databases.
    // Each seed method is wrapped in try/catch so one failure won't crash the app.
    using (IServiceScope scope = app.Services.CreateScope())
    {
        ShopFresherzDbContext db = scope.ServiceProvider
            .GetRequiredService<ShopFresherzDbContext>();
        await DataSeeder.SeedAsync(db);
    }

    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            string origin = context.Request.Headers["Origin"].ToString();
            if (!string.IsNullOrWhiteSpace(origin))
            {
                context.Response.Headers["Access-Control-Allow-Origin"] = origin;
                context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
            }

            await context.Response.WriteAsync(
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    code = "INTERNAL_ERROR",
                    message = "An unexpected error occurred."
                }));
        });
    });

    app.UseSerilogRequestLogging();

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ShopFresherz API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "ShopFresherz API";
    });

    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    // CORS must come before HTTPS redirect to handle preflight OPTIONS requests
    app.UseCors("ShopFresherzCors");

    // Short-circuit OPTIONS preflight requests to avoid CORS redirect issues
    app.Use(async (context, next) =>
    {
        if (context.Request.Method == "OPTIONS")
        {
            context.Response.Headers["Access-Control-Allow-Origin"] =
                context.Request.Headers["Origin"].ToString();
            context.Response.Headers["Access-Control-Allow-Methods"] =
                "GET, POST, PUT, DELETE, PATCH, OPTIONS";
            context.Response.Headers["Access-Control-Allow-Headers"] =
                "Content-Type, Authorization, X-Requested-With, Accept, Origin";
            context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
            context.Response.Headers["Access-Control-Max-Age"] = "86400";
            context.Response.StatusCode = 204;
            await context.Response.CompleteAsync();
            return;
        }
        await next();
    });

    app.UseHttpsRedirection();

    // Security headers middleware (production hardening)
    app.Use(async (context, next) =>

    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        if (!app.Environment.IsDevelopment())
        {
            context.Response.Headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' https://checkout.flutterwave.com; " +
                "style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data: https:; " +
                "connect-src 'self' https://api.flutterwave.com;";
        }

        await next();
    });

    string webRootPath = app.Environment.WebRootPath
        ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
    Directory.CreateDirectory(Path.Combine(webRootPath, "uploads"));
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(webRootPath)
    });

    app.UseIpRateLimiting();

    app.UseAuthentication();
    app.UseAuthorization();

    IDashboardAuthorizationFilter hangfireAuth = app.Environment.IsDevelopment()
        ? new HangfireDevAuthFilter()
        : new HangfireProductionAuthFilter();

    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [hangfireAuth]
    });

    try
    {
        RecurringJob.AddOrUpdate<StockReservationExpiryJob>(
            "stock-reservation-expiry",
            job => job.ExecuteAsync(),
            "*/5 * * * *");

        RecurringJob.AddOrUpdate<FlashDealExpiryJob>(
            "flash-deal-expiry",
            job => job.ExecuteAsync(),
            "* * * * *");

        RecurringJob.AddOrUpdate<BackInStockNotificationJob>(
            "back-in-stock",
            job => job.ExecuteAsync(),
            "*/10 * * * *");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Hangfire recurring jobs could not be scheduled during startup.");
    }

    app.MapControllers();

    // Health endpoint for production smoke tests
    app.MapGet("/health", () => Results.Ok(new
    {
        status      = "healthy",
        timestamp   = DateTime.UtcNow,
        environment = app.Environment.EnvironmentName,
        version     = "1.0.0",
    })).AllowAnonymous();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "ShopFresherz API failed to start.");
}
finally
{
    Log.CloseAndFlush();
}
