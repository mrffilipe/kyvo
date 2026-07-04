namespace Kyvo.Domain.Enums;

/// <summary>
/// Federation is implemented entirely through OpenIddict.Client (+ OpenIddict.Client.WebIntegration presets
/// for the well-known providers). <see cref="GenericOidc"/> covers any other OIDC-compliant issuer configured
/// by the platform admin (e.g. Cognito, Auth0, Keycloak, a partner's own IdP), resolved dynamically via its
/// discovery document.
/// </summary>
public enum IdentityProviderType
{
    Local = 0,
    Google = 1,
    Microsoft = 2,
    GitHub = 3,
    GenericOidc = 99
}
