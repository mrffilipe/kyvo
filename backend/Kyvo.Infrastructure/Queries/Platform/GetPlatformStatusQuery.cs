using Kyvo.Application.Configurations;
using Kyvo.Application.Ports.Oidc;
using Kyvo.Application.Queries.Platform.GetPlatformStatus;
using Kyvo.Domain.Constants;
using Kyvo.Domain.Repositories;
using Kyvo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;

namespace Kyvo.Infrastructure.Queries.Platform;

public sealed class GetPlatformStatusQuery : IGetPlatformStatusQuery
{
    private readonly ApplicationDbContext _context;
    private readonly IApplicationClientRepository _clients;
    private readonly IOpenIddictApplicationSyncService _openIddictSync;
    private readonly JwtOptions _jwtOptions;

    public GetPlatformStatusQuery(
        ApplicationDbContext context,
        IApplicationClientRepository clients,
        IOpenIddictApplicationSyncService openIddictSync,
        IOptions<JwtOptions> jwtOptions)
    {
        _context = context;
        _clients = clients;
        _openIddictSync = openIddictSync;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<PlatformStatusResult> ExecuteAsync(CancellationToken ct = default)
    {
        var configuration = await _context.PlatformConfigurations
            .AsNoTracking()
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        var isConfigured = configuration?.IsBootstrapped == true && configuration.RootUserId.HasValue;

        if (isConfigured)
        {
            var changed = false;
            changed |= await EnsureAdminConsoleOfflineAccessScopeAsync(ct);
            changed |= await EnsureAdminConsoleRedirectUriAsync(ct);
            changed |= await EnsureAdminConsolePostLogoutRedirectUriAsync(ct);

            if (changed)
            {
                var client = await _clients.GetByClientIdAsync(PlatformDefaults.AdminConsole.CLIENT_ID, ct);
                if (client is not null)
                {
                    await _openIddictSync.SyncAsync(client, plainTextClientSecret: null, ct);
                }
            }
        }

        return new PlatformStatusResult
        {
            IsConfigured = isConfigured,
            RequiresBootstrap = !isConfigured,
            OauthClientId = isConfigured ? configuration?.OauthClientId : null
        };
    }

    private IReadOnlyList<string> BuildAdminConsoleRedirectUris()
    {
        var uris = new HashSet<string>(StringComparer.Ordinal);
        foreach (var uri in PlatformDefaults.AdminConsole.DefaultRedirectUris)
        {
            uris.Add(uri);
        }

        var issuer = _jwtOptions.Issuer.Trim().TrimEnd('/');
        if (!string.IsNullOrEmpty(issuer))
        {
            uris.Add($"{issuer}/auth/callback");
        }

        return uris.ToList();
    }

    private IReadOnlyList<string> BuildAdminConsolePostLogoutRedirectUris()
    {
        var uris = new HashSet<string>(StringComparer.Ordinal);
        foreach (var uri in PlatformDefaults.AdminConsole.DefaultPostLogoutRedirectUris)
        {
            uris.Add(uri);
        }

        var issuer = _jwtOptions.Issuer.Trim().TrimEnd('/');
        if (!string.IsNullOrEmpty(issuer))
        {
            uris.Add($"{issuer}/login");
        }

        return uris.ToList();
    }

    private async Task<bool> EnsureAdminConsoleRedirectUriAsync(CancellationToken ct)
    {
        var client = await _clients.GetByClientIdAsync(PlatformDefaults.AdminConsole.CLIENT_ID, ct);
        if (client is null || !client.IsSystem)
        {
            return false;
        }

        var expected = BuildAdminConsoleRedirectUris();
        var current = client.RedirectUris;
        if (expected.All(uri => current.Contains(uri, StringComparer.Ordinal)))
        {
            return false;
        }

        var merged = new HashSet<string>(current, StringComparer.Ordinal);
        foreach (var uri in expected)
        {
            merged.Add(uri);
        }

        await _context.ApplicationClients
            .Where(c => c.Id == client.Id)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(c => c.RedirectUris, merged.ToList()),
                ct);

        return true;
    }

    private async Task<bool> EnsureAdminConsolePostLogoutRedirectUriAsync(CancellationToken ct)
    {
        var client = await _clients.GetByClientIdAsync(PlatformDefaults.AdminConsole.CLIENT_ID, ct);
        if (client is null || !client.IsSystem)
        {
            return false;
        }

        var expected = BuildAdminConsolePostLogoutRedirectUris();
        var current = client.PostLogoutRedirectUris;
        if (expected.All(uri => current.Contains(uri, StringComparer.Ordinal)))
        {
            return false;
        }

        var merged = new HashSet<string>(current, StringComparer.Ordinal);
        foreach (var uri in expected)
        {
            merged.Add(uri);
        }

        await _context.ApplicationClients
            .Where(c => c.Id == client.Id)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(c => c.PostLogoutRedirectUris, merged.ToList()),
                ct);

        return true;
    }

    private async Task<bool> EnsureAdminConsoleOfflineAccessScopeAsync(CancellationToken ct)
    {
        var client = await _clients.GetByClientIdAsync(PlatformDefaults.AdminConsole.CLIENT_ID, ct);
        if (client is null || !client.IsSystem)
        {
            return false;
        }

        var scopes = client.AllowedScopes;
        if (scopes.Contains(OpenIddictConstants.Scopes.OfflineAccess, StringComparer.Ordinal))
        {
            return false;
        }

        var updatedScopes = scopes.ToList();
        updatedScopes.Add(OpenIddictConstants.Scopes.OfflineAccess);

        await _context.ApplicationClients
            .Where(c => c.Id == client.Id)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(c => c.AllowedScopes, updatedScopes),
                ct);

        return true;
    }
}
