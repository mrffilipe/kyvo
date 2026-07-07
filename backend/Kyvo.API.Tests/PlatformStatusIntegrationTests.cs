using Kyvo.API.Tests.Fixtures;
using Xunit;

namespace Kyvo.API.Tests;

public sealed class PlatformStatusIntegrationTests
{
    [RequiresDatabaseFact]
    public async Task GetPlatformStatus_ReturnsOk_WhenDatabaseConfigured()
    {

        await using var factory = new KyvoWebApplicationFactory();
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/platform/status");
        response.EnsureSuccessStatusCode();
    }
}
