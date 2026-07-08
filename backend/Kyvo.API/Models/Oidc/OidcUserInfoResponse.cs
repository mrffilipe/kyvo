namespace Kyvo.API.Models.Oidc;

/// <summary>Standard OpenID Connect UserInfo response shape.</summary>
public sealed class OidcUserInfoResponse
{
    public string? Sub { get; init; }
    public string? Email { get; init; }
    public string? Name { get; init; }
}
