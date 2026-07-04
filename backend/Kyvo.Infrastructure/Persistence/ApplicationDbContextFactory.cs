using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
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

        var connectionString = Environment.GetEnvironmentVariable("Database__ConnectionString")
            ?? "Host=localhost;Database=kyvo_ef_design;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options, tenancyOptions, tenantContextAccessor);
    }
}
