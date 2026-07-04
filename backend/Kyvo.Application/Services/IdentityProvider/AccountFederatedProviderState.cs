using Kyvo.Domain.Enums;

namespace Kyvo.Application.Services.IdentityProvider;

/// <summary>
/// A federated provider button on the login page. The redirect-based OpenIddict.Client flow needs no
/// public client configuration in the browser: the button just links to <c>/login/federated/{Alias}</c>.
/// </summary>
public sealed record AccountFederatedProviderState
{
    public required string Alias { get; init; }
    public required string DisplayName { get; init; }
    public required IdentityProviderType ProviderType { get; init; }
}
