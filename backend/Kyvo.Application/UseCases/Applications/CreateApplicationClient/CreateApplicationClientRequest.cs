using Kyvo.Domain.Enums;

namespace Kyvo.Application.UseCases.Applications.CreateApplicationClient;

public sealed record CreateApplicationClientRequest
{
    public required Guid ApplicationId { get; init; }
    public required string ClientId { get; init; }

    /// <summary>
    /// Plaintext secret for confidential clients (null for public/PKCE clients). Hashed and stored by
    /// OpenIddict itself (<c>OpenIddictApplications</c>) via <c>IOpenIddictApplicationSyncService</c>;
    /// Kyvo's own <c>ApplicationClient</c> row no longer stores any secret material.
    /// </summary>
    public string? ClientSecret { get; init; }

    public required ClientType ClientType { get; init; }
    public required string RedirectUris { get; init; }
    public string? PostLogoutRedirectUris { get; init; }
    public string? AllowedScopes { get; init; }
    public IReadOnlyList<string>? AllowedScopesList { get; init; }
    public required int AccessTokenTtlSeconds { get; init; }
    public required IReadOnlyList<string> ActorPlatformRoles { get; init; }
}
