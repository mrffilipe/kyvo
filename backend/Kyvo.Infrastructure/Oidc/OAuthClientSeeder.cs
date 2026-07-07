using Kyvo.Domain.Constants;
using Kyvo.Domain.Entities;
using Kyvo.Domain.Enums;
using Kyvo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;

namespace Kyvo.Infrastructure.Oidc;

public sealed class OAuthClientSeeder
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OAuthClientSeeder> _logger;

    public OAuthClientSeeder(IServiceProvider serviceProvider, ILogger<OAuthClientSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        await SeedSpaClientAsync(manager, ct);
        await SeedConfidentialClientAsync(manager, ct);

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await SeedIdentityProvidersAsync(dbContext, ct);
    }

    private async Task SeedIdentityProvidersAsync(ApplicationDbContext dbContext, CancellationToken ct)
    {
        if (await dbContext.Set<IdentityProvider>().AnyAsync(ct))
            return;

        var localIdp = new IdentityProvider(
            alias: "local",
            displayName: "Email and Password",
            providerType: IdentityProviderType.Local,
            capabilities: [IdpCapability.LocalPassword]
        );

        var googleIdp = new IdentityProvider(
            alias: "google",
            displayName: "Google",
            providerType: IdentityProviderType.Google,
            capabilities: [IdpCapability.GoogleSocial],
            enabled: false // Disabled by default, configure configJson first
        );

        var microsoftIdp = new IdentityProvider(
            alias: "microsoft",
            displayName: "Microsoft",
            providerType: IdentityProviderType.Microsoft,
            capabilities: [IdpCapability.MicrosoftSocial],
            enabled: false
        );

        var githubIdp = new IdentityProvider(
            alias: "github",
            displayName: "GitHub",
            providerType: IdentityProviderType.GitHub,
            capabilities: [IdpCapability.GenericOidc], // Or GitHubSocial if added
            enabled: false
        );

        dbContext.Set<IdentityProvider>().AddRange(localIdp, googleIdp, microsoftIdp, githubIdp);
        await dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Seeded initial identity providers");
    }

    private async Task SeedSpaClientAsync(IOpenIddictApplicationManager manager, CancellationToken ct)
    {
        const string clientId = "kyvo-spa";

        if (await manager.FindByClientIdAsync(clientId, ct) is not null)
        {
            return;
        }

        await manager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            DisplayName = "Kyvo SPA / Mobile",
            ClientType = OpenIddictConstants.ClientTypes.Public,
            ConsentType = OpenIddictConstants.ConsentTypes.Explicit,
            RedirectUris =
            {
                new Uri("http://localhost:3000/callback"),
                new Uri("https://localhost:3000/callback"),
                new Uri("http://127.0.0.1:3000/callback")
            },
            PostLogoutRedirectUris =
            {
                new Uri("http://localhost:3000/logout-callback"),
                new Uri("https://localhost:3000/logout-callback")
            },
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.Endpoints.Revocation,
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                OpenIddictConstants.Permissions.ResponseTypes.Code,
                OpenIddictConstants.Permissions.Scopes.Email,
                OpenIddictConstants.Permissions.Scopes.Profile,
                OpenIddictConstants.Permissions.Scopes.Roles,
                OpenIddictConstants.Permissions.Prefixes.Scope + KyvoScopes.Api,
                OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OfflineAccess
            },
            Requirements =
            {
                OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
            }
        }, ct);

        _logger.LogInformation("Seeded OAuth public client {ClientId}", clientId);
    }

    private async Task SeedConfidentialClientAsync(IOpenIddictApplicationManager manager, CancellationToken ct)
    {
        const string clientId = "kyvo-backend";

        if (await manager.FindByClientIdAsync(clientId, ct) is not null)
        {
            return;
        }

        await manager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientSecret = "dev-secret-change-in-production",
            DisplayName = "Kyvo Confidential Backend",
            ClientType = OpenIddictConstants.ClientTypes.Confidential,
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
            RedirectUris =
            {
                new Uri("http://localhost:5001/callback"),
                new Uri("https://localhost:5001/callback")
            },
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.Endpoints.Introspection,
                OpenIddictConstants.Permissions.Endpoints.Revocation,
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                OpenIddictConstants.Permissions.ResponseTypes.Code,
                OpenIddictConstants.Permissions.Scopes.Email,
                OpenIddictConstants.Permissions.Scopes.Profile,
                OpenIddictConstants.Permissions.Scopes.Roles,
                OpenIddictConstants.Permissions.Prefixes.Scope + KyvoScopes.Api,
                OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OfflineAccess
            }
        }, ct);

        _logger.LogInformation("Seeded OAuth confidential client {ClientId}", clientId);
    }
}
