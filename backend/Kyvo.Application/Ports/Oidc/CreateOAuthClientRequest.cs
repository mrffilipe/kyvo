using Kyvo.Domain.Enums;

namespace Kyvo.Application.Ports.Oidc;

public sealed record CreateOAuthClientRequest
{
    public required Guid ApplicationId { get; init; }
    public required string ClientId { get; init; }
    public required ClientType ClientType { get; init; }
    public required IReadOnlyList<string> RedirectUris { get; init; }
    public required IReadOnlyList<string> PostLogoutRedirectUris { get; init; }
    public required IReadOnlyList<string> AllowedScopes { get; init; }
    public required int AccessTokenTtlSeconds { get; init; }
    public string? ClientSecret { get; init; }
    public bool IsSystem { get; init; }
    public bool RequireExplicitConsent { get; init; } = true;
}
