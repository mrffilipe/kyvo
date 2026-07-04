namespace Kyvo.Application.Ports.Federation;

/// <summary>
/// Invalidates the cached OpenIddict.Client registration for a federated <c>IdentityProvider</c> after an
/// admin edit, so the next login attempt picks up the new ClientId/ClientSecret/Issuer immediately.
/// </summary>
public interface IFederatedProviderRegistrationCache
{
    void Invalidate(string alias);
}
