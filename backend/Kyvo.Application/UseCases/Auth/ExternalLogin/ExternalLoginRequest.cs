namespace Kyvo.Application.UseCases.Auth.ExternalLogin;

/// <summary>
/// Claims already authenticated and validated upstream by OpenIddict.Client (Google/Microsoft/GitHub preset
/// or a generic OIDC provider's own discovery document). This use case never talks to the upstream provider
/// or validates tokens itself; it only resolves/creates the local <c>User</c> and links the external login.
/// </summary>
public sealed record ExternalLoginRequest
{
    public required string ProviderAlias { get; init; }
    public required string ProviderUserId { get; init; }
    public required string Email { get; init; }
    public string? DisplayName { get; init; }
}
