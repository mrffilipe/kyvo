using System.Net;
using System.Net.Http.Headers;
using Kyvo.API.Tests.Fixtures;
using Kyvo.Domain.Constants;
using Xunit;

namespace Kyvo.API.Tests;

public sealed class TenantApiAuthorizationIntegrationTests
{
    [RequiresDatabaseFact]
    public async Task AuditLogs_WithoutToken_ReturnsUnauthorized()
    {

        await using var factory = new KyvoWebApplicationFactory();
        await IntegrationTestSeed.SeedTenantMembershipAsync(factory.Services);

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/AuditLogs");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [RequiresDatabaseFact]
    public async Task AuditLogs_WithPlatformToken_ReturnsForbidden()
    {

        await using var factory = new KyvoWebApplicationFactory();
        var scenario = await IntegrationTestSeed.SeedTenantMembershipAsync(factory.Services);

        var client = factory.CreateClient();
        var platformToken = TestAccessTokenFactory.CreatePlatformToken(scenario.UserId, scenario.SessionId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);

        var response = await client.GetAsync("/api/v1/AuditLogs");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [RequiresDatabaseFact]
    public async Task AuditLogs_WithTenantOwnerToken_ReturnsOk()
    {

        await using var factory = new KyvoWebApplicationFactory();
        var scenario = await IntegrationTestSeed.SeedTenantMembershipAsync(factory.Services);

        var client = factory.CreateClient();
        var tenantToken = TestAccessTokenFactory.CreateTenantToken(
            scenario.UserId,
            scenario.SessionId,
            scenario.TenantId,
            scenario.MembershipId,
            TenantRoleDefaults.OWNER);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tenantToken);

        var response = await client.GetAsync("/api/v1/AuditLogs");
        response.EnsureSuccessStatusCode();
    }
}
