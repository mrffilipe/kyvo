using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Kyvo.API.Tests.Fixtures;
using Kyvo.Application.UseCases.Auth;
using Xunit;

namespace Kyvo.API.Tests;

public sealed class SwitchTenantFlowTests
{
    [RequiresDatabaseFact]
    public async Task SwitchTenant_ReturnsTenantAccessToken_WithTenantClaims()
    {

        await using var factory = new KyvoWebApplicationFactory();
        var scenario = await IntegrationTestSeed.SeedTenantMembershipAsync(factory.Services);

        var client = factory.CreateClient();
        var platformToken = TestAccessTokenFactory.CreatePlatformToken(scenario.UserId, scenario.SessionId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/switch-tenant",
            new { tenantId = scenario.TenantId });

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<TenantContextResult>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));
        Assert.Equal(scenario.TenantId, body.TenantId);
        Assert.Equal("Bearer", body.TokenType);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(body.AccessToken);
        Assert.Equal("tenant", jwt.Claims.First(c => c.Type == "token_use").Value);
        Assert.Equal(scenario.TenantId.ToString("D"), jwt.Claims.First(c => c.Type == "tid").Value);
    }
}
