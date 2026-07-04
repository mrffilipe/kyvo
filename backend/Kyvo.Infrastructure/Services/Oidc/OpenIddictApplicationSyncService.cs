using Kyvo.Application.Ports.Oidc;
using Kyvo.Domain.Entities;
using Kyvo.Domain.Enums;
using OpenIddict.Abstractions;

namespace Kyvo.Infrastructure.Services.Oidc;

public sealed class OpenIddictApplicationSyncService : IOpenIddictApplicationSyncService
{
    private readonly IOpenIddictApplicationManager _applicationManager;

    public OpenIddictApplicationSyncService(IOpenIddictApplicationManager applicationManager)
    {
        _applicationManager = applicationManager;
    }

    public async Task SyncAsync(ApplicationClient client, string? plainTextClientSecret, CancellationToken ct = default)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = client.ClientId,
            ClientType = client.ClientType == ClientType.Confidential
                ? OpenIddictConstants.ClientTypes.Confidential
                : OpenIddictConstants.ClientTypes.Public,
            ClientSecret = plainTextClientSecret,
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                OpenIddictConstants.Permissions.ResponseTypes.Code
            },
            Requirements =
            {
                OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
            }
        };

        foreach (var scope in client.AllowedScopes)
        {
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + scope);
        }

        foreach (var uri in client.RedirectUris)
        {
            descriptor.RedirectUris.Add(new Uri(uri, UriKind.Absolute));
        }

        foreach (var uri in client.PostLogoutRedirectUris)
        {
            descriptor.PostLogoutRedirectUris.Add(new Uri(uri, UriKind.Absolute));
        }

        var existing = await _applicationManager.FindByClientIdAsync(client.ClientId, ct);
        if (existing is null)
        {
            await _applicationManager.CreateAsync(descriptor, ct);
        }
        else
        {
            await _applicationManager.UpdateAsync(existing, descriptor, ct);
        }
    }
}
