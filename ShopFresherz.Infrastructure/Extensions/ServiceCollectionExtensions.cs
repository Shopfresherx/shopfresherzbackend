using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using ShopFresherz.Domain.Interfaces;
using ShopFresherz.Domain.Interfaces.Repositories;
using ShopFresherz.Domain.Interfaces.Services;
using ShopFresherz.Infrastructure.Configuration;
using ShopFresherz.Infrastructure.Persistence;
using ShopFresherz.Infrastructure.Persistence.Repositories;
using ShopFresherz.Infrastructure.Search;
using ShopFresherz.Infrastructure.Services;

namespace ShopFresherz.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering Infrastructure layer services with the DI container.
/// Call <see cref="AddInfrastructure"/> from the API layer's Program.cs.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the ShopFresherzDbContext, repositories, Unit of Work, and all
    /// Infrastructure services (token, cache, email, SMS, file storage, image processing, payments).
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── EF Core ────────────────────────────────────────────────────────────
        services.AddDbContext<ShopFresherzDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(ShopFresherzDbContext).Assembly.FullName)),
            optionsLifetime: ServiceLifetime.Singleton);
        services.AddDbContextFactory<ShopFresherzDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(ShopFresherzDbContext).Assembly.FullName)));

        // ── Unit of Work ───────────────────────────────────────────────────────
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ── Redis / Cache ──────────────────────────────────────────────────────
        string? redisConnection = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redisConnection))
        {
            redisConnection = configuration["Redis:ConnectionString"];
        }

        if (string.IsNullOrWhiteSpace(redisConnection))
        {
            // No Redis configured — fall back to in-memory cache (already registered in Program.cs).
            services.AddSingleton<ICacheService, MemoryCacheService>();
        }
        else
        {
            try
            {
                services.AddSingleton<IConnectionMultiplexer>(
                    _ => ConnectionMultiplexer.Connect(redisConnection));
                services.AddSingleton<ICacheService, RedisCacheService>();
            }
            catch
            {
                // If Redis connection fails, fall back to in-memory cache.
                services.AddSingleton<ICacheService, MemoryCacheService>();
            }
        }

        // ── HTTP clients for third-party services ─────────────────────────────
        services.Configure<FlutterwaveOptions>(configuration.GetSection("Flutterwave"));
        services.AddHttpClient<IFlutterwavePaymentService, FlutterwavePaymentService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        services.Configure<BankTransferSettings>(configuration.GetSection("BankTransfer"));
        services.AddSingleton<IBankTransferDetailsProvider, BankTransferDetailsProvider>();

        // ── Token / Auth ───────────────────────────────────────────────────────
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.Configure<GoogleAuthSettings>(configuration.GetSection("GoogleAuth"));
        services.AddSingleton<IGoogleTokenValidator, GoogleTokenValidator>();

        // ── Email ──────────────────────────────────────────────────────────────
        services.Configure<BrevoSettings>(configuration.GetSection("Brevo"));
        services.AddHttpClient<IEmailService, BrevoEmailService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddScoped<IAuditLogService, EfAuditLogService>();

        // ── SMS ────────────────────────────────────────────────────────────────
        services.Configure<TermiiOptions>(configuration.GetSection("Termii"));
        services.AddHttpClient<ISmsService, TermiiSmsService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        // ── Image processing ───────────────────────────────────────────────────
        services.AddScoped<IImageProcessingService, ImageProcessingService>();

        // ── Search ─────────────────────────────────────────────────────────────
        services.Configure<ElasticsearchOptions>(configuration.GetSection("Elasticsearch"));
        services.AddScoped<ISearchService, ElasticsearchService>();

        // ── Notifications ───────────────────────────────────────────────────────
        services.AddScoped<INotificationRepository, EfNotificationRepository>();

        // ── Payment gateway ────────────────────────────────────────────────────

        // ── Chatbot ────────────────────────────────────────────────────────────
        services.AddScoped<IChatbotService, ChatbotService>();

        return services;
    }
}
