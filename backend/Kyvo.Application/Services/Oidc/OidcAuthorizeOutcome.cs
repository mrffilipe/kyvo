namespace Kyvo.Application.Services.Oidc;

public abstract record OidcAuthorizeOutcome;

public sealed record OidcAuthorizeSuccess(string RedirectUri, string Code, string? State) : OidcAuthorizeOutcome;

public sealed record OidcAuthorizeRedirectError(string? RedirectUri, string? State, OidcError Error) : OidcAuthorizeOutcome;

public sealed record OidcAuthorizeLoginChallenge : OidcAuthorizeOutcome;
