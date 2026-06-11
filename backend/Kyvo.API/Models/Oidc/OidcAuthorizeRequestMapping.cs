using Kyvo.Application.Services.Oidc;

namespace Kyvo.API.Models.Oidc;

internal static class OidcAuthorizeRequestMapping
{
    public static OidcAuthorizeRequest ToOidcAuthorizeRequest(this OidcAuthorizeQueryRequest query) =>
        Map(query.ClientId, query.RedirectUri, query.ResponseType, query.Scope, query.State, query.Prompt,
            query.MaxAge, query.CodeChallenge, query.CodeChallengeMethod, query.Nonce);

    public static OidcAuthorizeRequest ToOidcAuthorizeRequest(this OidcAuthorizeFormRequest form) =>
        Map(form.ClientId, form.RedirectUri, form.ResponseType, form.Scope, form.State, form.Prompt,
            form.MaxAge, form.CodeChallenge, form.CodeChallengeMethod, form.Nonce);

    private static OidcAuthorizeRequest Map(
        string? clientId,
        string? redirectUri,
        string? responseType,
        string? scope,
        string? state,
        string? prompt,
        int? maxAge,
        string? codeChallenge,
        string? codeChallengeMethod,
        string? nonce) =>
        new()
        {
            ClientId = clientId ?? string.Empty,
            RedirectUri = redirectUri ?? string.Empty,
            ResponseType = responseType ?? string.Empty,
            Scope = scope ?? string.Empty,
            State = NullIfEmpty(state),
            Prompt = NullIfEmpty(prompt),
            MaxAge = maxAge,
            CodeChallenge = NullIfEmpty(codeChallenge),
            CodeChallengeMethod = NullIfEmpty(codeChallengeMethod),
            Nonce = NullIfEmpty(nonce)
        };

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
