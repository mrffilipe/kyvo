namespace Kyvo.Application.IdentityProviderConfigs;

/// <summary>
/// Single configuration schema shared by every non-local <see cref="Kyvo.Domain.Enums.IdentityProviderType"/>.
/// <see cref="Issuer"/> is only required for <see cref="Kyvo.Domain.Enums.IdentityProviderType.GenericOidc"/>;
/// well-known providers (Google/Microsoft/GitHub) resolve their issuer/discovery endpoint from the
/// OpenIddict.Client.WebIntegration preset.
/// </summary>
public sealed record FederatedProviderConfig
{
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public string? Issuer { get; init; }
}
