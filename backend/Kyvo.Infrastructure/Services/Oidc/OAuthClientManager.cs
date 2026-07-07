using Kyvo.Application.Common;
using Kyvo.Application.Configurations;
using Kyvo.Application.Ports.Oidc;
using Kyvo.Domain.Constants;
using Kyvo.Domain.Enums;
using Kyvo.Infrastructure.Persistence;
using Kyvo.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;

namespace Kyvo.Infrastructure.Services.Oidc;

public sealed class OAuthClientManager : IOAuthClientManager
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly ApplicationDbContext _context;
    private readonly JwtOptions _jwtOptions;

    public OAuthClientManager(
        IOpenIddictApplicationManager applicationManager,
        ApplicationDbContext context,
        IOptions<JwtOptions> jwtOptions)
    {
        _applicationManager = applicationManager;
        _context = context;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<Guid> CreateAsync(CreateOAuthClientRequest request, CancellationToken ct = default)
    {
        var descriptor = BuildDescriptor(request);
        var application = await _applicationManager.CreateAsync(descriptor, ct);
        var id = await GetApplicationGuidIdAsync(application, ct);

        await _context.Set<KyvoOpenIddictApplication>()
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.ApplicationId, request.ApplicationId)
                .SetProperty(x => x.IsSystem, request.IsSystem)
                .SetProperty(x => x.AccessTokenTtlSeconds, request.AccessTokenTtlSeconds > 0 ? request.AccessTokenTtlSeconds : 900),
                ct);

        return id;
    }

    public async Task<OAuthClientInfo?> GetByClientIdAsync(string clientId, CancellationToken ct = default)
    {
        var application = await _applicationManager.FindByClientIdAsync(clientId, ct);
        return application is null ? null : await MapAsync(application, ct);
    }

    public async Task<OAuthClientInfo?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var application = await _applicationManager.FindByIdAsync(id.ToString("D"), ct);
        return application is null ? null : await MapAsync(application, ct);
    }

    public async Task<IReadOnlyList<OAuthClientInfo>> ListByApplicationIdAsync(Guid applicationId, CancellationToken ct = default)
    {
        var entities = await _context.Set<KyvoOpenIddictApplication>()
            .AsNoTracking()
            .Where(x => x.ApplicationId == applicationId)
            .OrderBy(x => x.ClientId)
            .ToListAsync(ct);

        var results = new List<OAuthClientInfo>(entities.Count);
        foreach (var entity in entities)
        {
            var application = await _applicationManager.FindByIdAsync(entity.Id.ToString("D"), ct);
            if (application is not null)
            {
                results.Add(await MapAsync(application, entity, ct));
            }
        }

        return results;
    }

    public async Task<bool> ClientIdExistsAsync(string clientId, CancellationToken ct = default)
    {
        return await _applicationManager.FindByClientIdAsync(clientId, ct) is not null;
    }

    public async Task RepairAdminConsoleClientAsync(Guid applicationId, CancellationToken ct = default)
    {
        var application = await _applicationManager.FindByClientIdAsync(PlatformDefaults.AdminConsole.CLIENT_ID, ct);
        if (application is null)
        {
            return;
        }

        var openIddictApplicationId = await GetApplicationGuidIdAsync(application, ct);
        var entity = await _context.Set<KyvoOpenIddictApplication>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == openIddictApplicationId, ct);

        if (entity is null || !entity.IsSystem || entity.ApplicationId != applicationId)
        {
            return;
        }

        var redirectUris = AdminConsoleClientDefaults.BuildRedirectUris(_jwtOptions.Issuer);
        var postLogoutUris = AdminConsoleClientDefaults.BuildPostLogoutRedirectUris(_jwtOptions.Issuer);
        var scopes = PlatformDefaults.AdminConsole.AllowedScopes.ToList();
        if (!scopes.Contains(OpenIddictConstants.Scopes.OfflineAccess, StringComparer.Ordinal))
        {
            scopes.Add(OpenIddictConstants.Scopes.OfflineAccess);
        }

        var currentRedirectUris = (await _applicationManager.GetRedirectUrisAsync(application, ct))
            .Select(uri => uri.ToString())
            .ToHashSet(StringComparer.Ordinal);

        var currentPostLogoutUris = (await _applicationManager.GetPostLogoutRedirectUrisAsync(application, ct))
            .Select(uri => uri.ToString())
            .ToHashSet(StringComparer.Ordinal);

        var currentPermissions = (await _applicationManager.GetPermissionsAsync(application, ct)).ToHashSet(StringComparer.Ordinal);
        var needsUpdate = !redirectUris.All(currentRedirectUris.Contains)
            || !postLogoutUris.All(currentPostLogoutUris.Contains)
            || !currentPermissions.Contains(OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OfflineAccess);

        if (!needsUpdate)
        {
            return;
        }

        var descriptor = new OpenIddictApplicationDescriptor();
        await _applicationManager.PopulateAsync(descriptor, application, ct);

        foreach (var uri in redirectUris)
        {
            descriptor.RedirectUris.Add(new Uri(uri, UriKind.Absolute));
        }

        foreach (var uri in postLogoutUris)
        {
            descriptor.PostLogoutRedirectUris.Add(new Uri(uri, UriKind.Absolute));
        }

        foreach (var scope in scopes)
        {
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + scope);
        }

        await _applicationManager.UpdateAsync(application, descriptor, ct);
    }

    private static OpenIddictApplicationDescriptor BuildDescriptor(CreateOAuthClientRequest request)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = request.ClientId.Trim(),
            ClientType = request.ClientType == ClientType.Confidential
                ? OpenIddictConstants.ClientTypes.Confidential
                : OpenIddictConstants.ClientTypes.Public,
            ClientSecret = request.ClientSecret,
            ConsentType = request.RequireExplicitConsent
                ? OpenIddictConstants.ConsentTypes.Explicit
                : OpenIddictConstants.ConsentTypes.Implicit,
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.Endpoints.EndSession,
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                OpenIddictConstants.Permissions.ResponseTypes.Code
            },
            Requirements =
            {
                OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
            }
        };

        foreach (var scope in request.AllowedScopes)
        {
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + scope);
        }

        foreach (var uri in request.RedirectUris)
        {
            descriptor.RedirectUris.Add(new Uri(uri, UriKind.Absolute));
        }

        foreach (var uri in request.PostLogoutRedirectUris)
        {
            descriptor.PostLogoutRedirectUris.Add(new Uri(uri, UriKind.Absolute));
        }

        return descriptor;
    }

    private async Task<OAuthClientInfo> MapAsync(object application, CancellationToken ct)
    {
        var id = await GetApplicationGuidIdAsync(application, ct);
        var entity = await _context.Set<KyvoOpenIddictApplication>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return await MapAsync(application, entity, ct);
    }

    private async Task<OAuthClientInfo> MapAsync(object application, KyvoOpenIddictApplication? entity, CancellationToken ct)
    {
        var id = await GetApplicationGuidIdAsync(application, ct);
        var clientTypeRaw = await _applicationManager.GetClientTypeAsync(application, ct);
        var permissions = await _applicationManager.GetPermissionsAsync(application, ct);
        var scopePrefix = OpenIddictConstants.Permissions.Prefixes.Scope;

        return new OAuthClientInfo
        {
            Id = id,
            ApplicationId = entity?.ApplicationId ?? Guid.Empty,
            ClientId = await _applicationManager.GetClientIdAsync(application, ct) ?? string.Empty,
            ClientType = string.Equals(clientTypeRaw, OpenIddictConstants.ClientTypes.Confidential, StringComparison.OrdinalIgnoreCase)
                ? ClientType.Confidential
                : ClientType.Public,
            RedirectUris = (await _applicationManager.GetRedirectUrisAsync(application, ct))
                .Select(uri => uri.ToString())
                .ToList(),
            PostLogoutRedirectUris = (await _applicationManager.GetPostLogoutRedirectUrisAsync(application, ct))
                .Select(uri => uri.ToString())
                .ToList(),
            AllowedScopes = permissions
                .Where(permission => permission.StartsWith(scopePrefix, StringComparison.Ordinal))
                .Select(permission => permission[scopePrefix.Length..])
                .ToList(),
            AccessTokenTtlSeconds = entity?.AccessTokenTtlSeconds ?? 900,
            IsSystem = entity?.IsSystem ?? false,
            DisplayName = await _applicationManager.GetDisplayNameAsync(application, ct)
        };
    }

    private async Task<Guid> GetApplicationGuidIdAsync(object application, CancellationToken ct)
    {
        var id = await _applicationManager.GetIdAsync(application, ct)
            ?? throw new InvalidOperationException("OpenIddict application id is missing.");

        return Guid.Parse(id.ToString()!);
    }
}
