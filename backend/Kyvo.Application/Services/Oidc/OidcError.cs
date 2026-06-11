namespace Kyvo.Application.Services.Oidc;

public sealed record OidcError
{
    public required string Error { get; init; }
    public string? ErrorDescription { get; init; }
}
