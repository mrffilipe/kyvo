using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using TenancyKit.Abstractions;
using TenancyKit.AspNetCore;
using TenancyKit.Core;

namespace Kyvo.Infrastructure.Persistence;

/// <summary>
/// Supplies a design-time <see cref="ApplicationDbContext"/> for EF Core tooling (migrations, bundles)
/// without bootstrapping the full API host (OpenIddict, JWT signing keys, etc.).
/// </summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var tenancyOptions = new TenancyKitOptions<TenantInfoAdapter>();
        tenancyOptions
            .UseMissingTenantBehavior(MissingTenantBehavior.Ignore)
            .UseClaimsTenantResolver()
            .UseStore(_ => new InMemoryTenantStore<TenantInfoAdapter>([]));

        var tenantContext = new TenantContext<TenantInfoAdapter>();
        var tenantContextAccessor = new TenantContextAccessor<TenantInfoAdapter>(tenantContext);

        var connectionString = ResolveConnectionString();

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options, tenancyOptions, tenantContextAccessor);
    }

    private static string ResolveConnectionString()
    {
        var apiProjectPath = ResolveApiProjectPath();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiProjectPath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetSection("Database")["ConnectionString"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Database:ConnectionString is not configured. Set it in Kyvo.API/appsettings.Development.json " +
                "or export Database__ConnectionString before running EF tooling.");
        }

        return connectionString;
    }

    private static string ResolveApiProjectPath()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (directory is not null)
        {
            var apiPath = Path.Combine(directory.FullName, "Kyvo.API");
            if (Directory.Exists(apiPath))
            {
                return apiPath;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate the Kyvo.API project directory. Run EF commands from the backend folder " +
            "or pass --startup-project Kyvo.API.");
    }
}
