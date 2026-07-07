using Kyvo.API;

using Xunit;

namespace Kyvo.API.Tests;

public sealed class TenantApiAuthorizationIntegrationTests
{
    private static bool HasTestDatabase =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("KYVO_TEST_DB"));

    [Fact]
    public async Task AuditLogs_RequiresTenantToken_WhenDatabaseConfigured()
    {
        if (!HasTestDatabase)
        {
            return;
        }

        await using var factory = new KyvoWebApplicationFactory();
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/AuditLogs");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
