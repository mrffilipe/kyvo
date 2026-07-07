namespace Kyvo.API.Models.Oidc;

/// <summary>OpenID Connect UserInfo response shape (platform context only).</summary>
public sealed class OidcUserInfoResponse
{
    public string? Sub { get; init; }
    public string? Email { get; init; }
    public string? Name { get; init; }
    public string? Sid { get; init; }
    public IReadOnlyList<string> Prole { get; init; } = [];
}
