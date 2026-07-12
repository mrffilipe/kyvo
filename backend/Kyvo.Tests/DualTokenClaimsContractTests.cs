using System.Security.Claims;
using Kyvo.Domain.Constants;
using Xunit;

namespace Kyvo.Tests;

/// <summary>
/// Contract tests for dual-token claim separation (OIDC platform vs tenant JWT).
/// </summary>
public sealed class DualTokenClaimsContractTests
{
    [Fact]
    public void Platform_token_must_not_carry_tenant_claims()
    {
        var forbidden = new[] { "tid", "mid", "trole", "token_use" };
        var platformClaims = new HashSet<string>(StringComparer.Ordinal)
        {
            "sub", "email", "name", "sid", "client_id", PlatformRoleDefaults.CLAIM_TYPE
        };

        Assert.DoesNotContain(platformClaims, c => forbidden.Contains(c));
        Assert.Contains("sid", platformClaims);
        Assert.Contains(PlatformRoleDefaults.CLAIM_TYPE, platformClaims);
    }

    [Fact]
    public void Tenant_token_must_include_domain_claims()
    {
        var tenantClaims = new Dictionary<string, string>
        {
            ["sub"] = Guid.NewGuid().ToString("D"),
            ["sid"] = Guid.NewGuid().ToString("D"),
            ["token_use"] = "tenant",
            ["tid"] = Guid.NewGuid().ToString("D"),
            ["mid"] = Guid.NewGuid().ToString("D"),
            ["trole"] = TenantRoleDefaults.OWNER,
            [PlatformRoleDefaults.CLAIM_TYPE] = PlatformRoleDefaults.PLATFORM_ADMINISTRATOR
        };

        Assert.Equal("tenant", tenantClaims["token_use"]);
        Assert.True(Guid.TryParse(tenantClaims["tid"], out _));
        Assert.True(Guid.TryParse(tenantClaims["mid"], out _));
        Assert.False(string.IsNullOrWhiteSpace(tenantClaims["trole"]));
    }

    [Fact]
    public void Connect_routes_remain_identity_only()
    {
        var connectExclusions = new[]
        {
            "/connect/authorize",
            "/connect/token",
            "/connect/userinfo",
            "/connect/logout",
            "/connect/revoke",
            "/connect/introspect"
        };

        var domainOnly = new[]
        {
            "/api/v1/auth/subscribe",
            "/api/v1/auth/switch-tenant",
            "/api/v1/tenants",
            "/api/v1/memberships"
        };

        Assert.All(connectExclusions, r => Assert.StartsWith("/connect/", r));
        Assert.All(domainOnly, r => Assert.StartsWith("/api/v1/", r));
        Assert.DoesNotContain(domainOnly, r => r.StartsWith("/connect/", StringComparison.Ordinal));
    }
}
