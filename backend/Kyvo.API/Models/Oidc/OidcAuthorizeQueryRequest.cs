using Microsoft.AspNetCore.Mvc;

namespace Kyvo.API.Models.Oidc;

/// <summary>
/// Query string for GET /connect/authorize.
/// </summary>
public sealed record OidcAuthorizeQueryRequest
{
    [FromQuery(Name = "client_id")]
    public string? ClientId { get; init; }

    [FromQuery(Name = "redirect_uri")]
    public string? RedirectUri { get; init; }

    [FromQuery(Name = "response_type")]
    public string? ResponseType { get; init; }

    [FromQuery(Name = "scope")]
    public string? Scope { get; init; }

    [FromQuery(Name = "state")]
    public string? State { get; init; }

    [FromQuery(Name = "prompt")]
    public string? Prompt { get; init; }

    [FromQuery(Name = "max_age")]
    public int? MaxAge { get; init; }

    [FromQuery(Name = "code_challenge")]
    public string? CodeChallenge { get; init; }

    [FromQuery(Name = "code_challenge_method")]
    public string? CodeChallengeMethod { get; init; }

    [FromQuery(Name = "nonce")]
    public string? Nonce { get; init; }
}
