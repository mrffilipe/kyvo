namespace Kyvo.Application.Services.Oidc;

public interface IOidcAuthorizeHandler
{
    Task<OidcAuthorizeOutcome> HandleAsync(OidcAuthorizeRequest request, OidcCookieAuthenticationState authentication, CancellationToken ct = default);
}
