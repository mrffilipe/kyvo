using Microsoft.AspNetCore.Mvc;

namespace Kyvo.API.Models.Oidc;

/// <summary>
/// Form body for POST /connect/authorize (application/x-www-form-urlencoded).
/// </summary>
public sealed record OidcAuthorizeFormRequest
{
    [FromForm(Name = "client_id")]
    public string? ClientId { get; init; }

    [FromForm(Name = "redirect_uri")]
    public string? RedirectUri { get; init; }

    [FromForm(Name = "response_type")]
    public string? ResponseType { get; init; }

    [FromForm(Name = "scope")]
    public string? Scope { get; init; }

    [FromForm(Name = "state")]
    public string? State { get; init; }

    [FromForm(Name = "prompt")]
    public string? Prompt { get; init; }

    [FromForm(Name = "max_age")]
    public int? MaxAge { get; init; }

    [FromForm(Name = "code_challenge")]
    public string? CodeChallenge { get; init; }

    [FromForm(Name = "code_challenge_method")]
    public string? CodeChallengeMethod { get; init; }

    [FromForm(Name = "nonce")]
    public string? Nonce { get; init; }
}
