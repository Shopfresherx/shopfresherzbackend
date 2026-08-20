using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ShopFresherz.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by EF Core CLI tools (dotnet ef migrations add, database update).
/// Bypasses the application startup so migrations can run without live third-party credentials.
/// The connection string is read from the API project's appsettings.json.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ShopFresherzDbContext>
{
    /// <inheritdoc />
    public ShopFresherzDbContext CreateDbContext(string[] args)
    {
        // Walk up from the Infrastructure project to find the API project's appsettings.json
        string basePath = Path.GetFullPath(
            Path.Combine(Directory.GetCurrentDirectory(), "..", "ShopFresherz.API"));

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        string connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection is missing from appsettings.json. " +
                "Set it before running migrations.");

        DbContextOptionsBuilder<ShopFresherzDbContext> optionsBuilder = new();
        optionsBuilder.UseSqlServer(connectionString,
            sql => sql.MigrationsAssembly(typeof(ShopFresherzDbContext).Assembly.FullName));

        return new ShopFresherzDbContext(optionsBuilder.Options);
    }
}
