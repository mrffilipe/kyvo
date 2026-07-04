namespace Kyvo.API.Models.Oidc;

/// <summary>OpenID Connect UserInfo response shape.</summary>
public sealed class OidcUserInfoResponse
{
    public string? Sub { get; init; }
    public string? Email { get; init; }
    public string? Name { get; init; }
    public string? Tid { get; init; }
    public string? Mid { get; init; }
    public IReadOnlyList<string> Trole { get; init; } = [];
    public IReadOnlyList<string> Prole { get; init; } = [];
}
