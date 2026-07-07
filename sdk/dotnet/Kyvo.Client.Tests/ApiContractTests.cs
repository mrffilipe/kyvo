using System.Text.Json;
using System.Text.Json.Serialization;
using Kyvo.Client.Models;
using Xunit;

namespace Kyvo.Client.Tests;

/// <summary>
/// Contract paths aligned with Kyvo API v1 (<c>/api/v1/*</c>).
/// </summary>
public sealed class ApiContractTests
{
    private const string V = "/api/v1";

    [Theory]
    [InlineData($"{V}/auth/subscribe")]
    [InlineData($"{V}/auth/switch-tenant")]
    [InlineData($"{V}/auth/sessions")]
    [InlineData($"{V}/Users/me")]
    [InlineData($"{V}/Users/me/memberships")]
    [InlineData($"{V}/Tenants")]
    [InlineData($"{V}/Tenants/keys/{{key}}/availability")]
    [InlineData($"{V}/auth/account")]
    [InlineData($"{V}/AuditLogs/filter-options")]
    [InlineData($"{V}/invites/accept")]
    [InlineData($"{V}/Tenants/{{tenantId}}/invites")]
    [InlineData($"{V}/Invites/{{id}}")]
    [InlineData($"{V}/tenants/{{tenantId}}/memberships")]
    [InlineData($"{V}/tenants/{{tenantId}}/roles")]
    [InlineData($"{V}/Memberships/{{id}}")]
    [InlineData($"{V}/TenantRoles/{{id}}")]
    [InlineData($"{V}/AuditLogs")]
    public void ProductEndpointPaths_UseExpectedPrefix(string pathTemplate)
    {
        Assert.StartsWith(V, pathTemplate, StringComparison.Ordinal);
        Assert.DoesNotContain("Applications", pathTemplate, StringComparison.Ordinal);
        Assert.DoesNotContain("IdentityProviders", pathTemplate, StringComparison.Ordinal);
        Assert.DoesNotContain("platform/bootstrap", pathTemplate, StringComparison.Ordinal);
    }

    [Fact]
    public void Subscribe_IsOnlyOnAuthRoute()
    {
        const string subscribe = $"{V}/auth/subscribe";
        Assert.Equal("/api/v1/auth/subscribe", subscribe);
    }

    [Fact]
    public void TenantContextResult_DeserializesAccessTokenFromSubscribeResponse()
    {
        const string json = """
            {
              "userId": "11111111-1111-1111-1111-111111111111",
              "email": "user@example.com",
              "tenantId": "22222222-2222-2222-2222-222222222222",
              "membershipId": "33333333-3333-3333-3333-333333333333",
              "tenantRoles": ["owner"],
              "platformRoles": [],
              "tenants": [],
              "accessToken": "eyJhbGciOiJIUzI1NiJ9.test",
              "expiresIn": 900,
              "tokenType": "Bearer"
            }
            """;

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() },
        };

        var result = JsonSerializer.Deserialize<TenantContextResult>(json, options);
        Assert.NotNull(result);
        Assert.Equal("eyJhbGciOiJIUzI1NiJ9.test", result.AccessToken);
        Assert.Equal(900, result.ExpiresIn);
        Assert.Equal("Bearer", result.TokenType);
    }
}
