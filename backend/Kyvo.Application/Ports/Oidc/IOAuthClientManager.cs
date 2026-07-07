namespace Kyvo.Application.Ports.Oidc;

/// <summary>
/// Manages OAuth clients stored in OpenIddict (<c>OpenIddictApplications</c>) as the single source of truth.
/// </summary>
public interface IOAuthClientManager
{
    Task<Guid> CreateAsync(CreateOAuthClientRequest request, CancellationToken ct = default);
    Task<OAuthClientInfo?> GetByClientIdAsync(string clientId, CancellationToken ct = default);
    Task<OAuthClientInfo?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<OAuthClientInfo>> ListByApplicationIdAsync(Guid applicationId, CancellationToken ct = default);
    Task<bool> ClientIdExistsAsync(string clientId, CancellationToken ct = default);
    Task RepairAdminConsoleClientAsync(Guid applicationId, CancellationToken ct = default);
}
