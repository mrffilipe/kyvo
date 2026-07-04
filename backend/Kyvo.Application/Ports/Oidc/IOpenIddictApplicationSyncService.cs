using Kyvo.Domain.Entities;

namespace Kyvo.Application.Ports.Oidc;

/// <summary>
/// Mirrors a Kyvo <see cref="ApplicationClient"/> (redirect URIs, scopes, client type) into OpenIddict's own
/// <c>OpenIddictApplications</c> table, so the authorization server's PKCE/redirect_uri/scope validation
/// operates on the same data the admin console manages. Kyvo's <see cref="ApplicationClient"/> stays the
/// source of truth for the admin UI and branding; OpenIddict is the source of truth for the client secret.
/// </summary>
public interface IOpenIddictApplicationSyncService
{
    /// <param name="plainTextClientSecret">
    /// Only pass a non-null value when creating a client or explicitly rotating its secret; passing null on
    /// an update leaves the previously stored secret untouched.
    /// </param>
    Task SyncAsync(ApplicationClient client, string? plainTextClientSecret, CancellationToken ct = default);
}
