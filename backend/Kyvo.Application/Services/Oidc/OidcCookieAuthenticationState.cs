namespace Kyvo.Application.Services.Oidc;

public sealed class OidcCookieAuthenticationState
{
    public required bool Succeeded { get; init; }
    public DateTimeOffset? IssuedUtc { get; init; }
    public Guid? SessionId { get; init; }
}
