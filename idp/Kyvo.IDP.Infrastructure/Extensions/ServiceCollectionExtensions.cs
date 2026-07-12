using Kyvo.IDP.Application.Configurations;
using Kyvo.IDP.Application.Services.AccountLinking;
using Kyvo.IDP.Application.Services.Claims;
using Kyvo.IDP.Application.UseCases.ExternalLogin;
using Kyvo.IDP.Infrastructure.Identity;
using Kyvo.IDP.Infrastructure.Persistence;
using Kyvo.IDP.Infrastructure.Services.AccountLinking;
using Kyvo.IDP.Infrastructure.Services.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;

namespace Kyvo.IDP.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is missing.");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
            options.UseOpenIddict();
        });

        services.Configure<OidcOptions>(configuration.GetSection(OidcOptions.SECTION));
        services.Configure<GoogleOidcOptions>(configuration.GetSection(GoogleOidcOptions.SECTION));
        services.Configure<DevSeedOptions>(configuration.GetSection(DevSeedOptions.SECTION));

        services.AddScoped<IClaimsMappingService, ClaimsMappingService>();
        services.AddScoped<IAccountLinkingService, AccountLinkingService>();
        services.AddScoped<IExternalLoginUseCase, ExternalLoginUseCase>();

        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync(ct);

        await SeedOAuthClientAsync(scope.ServiceProvider, ct);
        await SeedDevUserAsync(scope.ServiceProvider, ct);
    }

    private static async Task SeedOAuthClientAsync(IServiceProvider sp, CancellationToken ct)
    {
        var manager = sp.GetRequiredService<IOpenIddictApplicationManager>();
        const string clientId = "kyvo-idp-spa";

        if (await manager.FindByClientIdAsync(clientId, ct) is not null)
        {
            return;
        }

        await manager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            DisplayName = "Kyvo IDP Dev SPA",
            ClientType = OpenIddictConstants.ClientTypes.Public,
            ConsentType = OpenIddictConstants.ConsentTypes.Explicit,
            RedirectUris =
            {
                new Uri("https://localhost:3000/callback"),
                new Uri("http://localhost:3000/callback"),
                new Uri("https://oauth.pstmn.io/v1/callback")
            },
            PostLogoutRedirectUris =
            {
                new Uri("https://localhost:3000/"),
                new Uri("http://localhost:3000/")
            },
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.Endpoints.EndSession,
                OpenIddictConstants.Permissions.Endpoints.Revocation,
                OpenIddictConstants.Permissions.Endpoints.Introspection,
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                OpenIddictConstants.Permissions.ResponseTypes.Code,
                OpenIddictConstants.Permissions.Scopes.Email,
                OpenIddictConstants.Permissions.Scopes.Profile,
                OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OfflineAccess
            },
            Requirements =
            {
                OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
            }
        }, ct);

        sp.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Kyvo.IDP.Seed")
            .LogInformation("Seeded OAuth public client {ClientId}", clientId);
    }

    private static async Task SeedDevUserAsync(IServiceProvider sp, CancellationToken ct)
    {
        var seed = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<DevSeedOptions>>().Value;
        if (!seed.Enabled)
        {
            return;
        }

        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        if (await userManager.FindByEmailAsync(seed.AdminEmail) is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = seed.AdminEmail,
            Email = seed.AdminEmail,
            EmailConfirmed = true,
            DisplayName = seed.AdminDisplayName,
            IsActive = true
        };
        user.SetCreatedAt();

        var result = await userManager.CreateAsync(user, seed.AdminPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Dev user seed failed: {string.Join("; ", result.Errors.Select(e => e.Description))}");
        }

        sp.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Kyvo.IDP.Seed")
            .LogInformation("Seeded development user {Email}", seed.AdminEmail);
    }
}
