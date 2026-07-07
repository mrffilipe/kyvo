namespace Kyvo.Client.Models;

/// <summary>
/// Platform OIDC access token plus optional tenant-scoped JWT from switch/subscribe.
/// </summary>
public sealed record KyvoSessionTokens(
    string PlatformAccessToken,
    string? TenantAccessToken = null)
{
    public string RequirePlatformAccessToken() =>
        string.IsNullOrWhiteSpace(PlatformAccessToken)
            ? throw new InvalidOperationException("Platform access token is required.")
            : PlatformAccessToken;

    public string RequireTenantAccessToken() =>
        string.IsNullOrWhiteSpace(TenantAccessToken)
            ? throw new InvalidOperationException("Tenant access token is required. Call Auth.SwitchTenantAsync or SubscribeAsync first.")
            : TenantAccessToken;
}
